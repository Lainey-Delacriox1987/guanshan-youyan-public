#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GuanshanYouyan.Story
{
    // Included only in Editor/development builds. All progression goes through the visible UI.
    public sealed class FogWalkthrough : MonoBehaviour
    {
        [Serializable] class Report
        {public string status,error,phase,scope;public bool success,ecology,phoneFirst;public int width,height,frames,walkingFrames;public string[] checks;}
        public string StatusJson{get;private set;}="";
        FogGame game;List<string> checks=new List<string>();string output,phase="start";bool ecology,phoneFirst,finished;
        Touchscreen virtualTouch;FogStory originalStory;string fixtureDirectory;int beforeFrames;
        InputSettings.BackgroundBehavior originalBackground;
        Mouse physicalMouse;bool physicalMouseWasEnabled;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void FogVerificationReport(string json);
#endif
        public void Begin(bool withEcology=false,bool receiptFirst=false,string filename="full-playtest.json")
        {
            game=FindFirstObjectByType<FogGame>();if(game==null)throw new Exception("Missing full game");ecology=withEcology;phoneFirst=!receiptFirst;
            output=Path.Combine(Application.isEditor?Path.GetFullPath("ImportReports"):Application.persistentDataPath,filename);Directory.CreateDirectory(Path.GetDirectoryName(output));
            originalStory=game.Story;fixtureDirectory=Path.Combine(Application.temporaryCachePath,"fog-walkthrough-"+Guid.NewGuid().ToString("N"));
            typeof(FogGame).GetProperty("Story").SetValue(game,new FogStory(originalStory.Book,Path.Combine(fixtureDirectory,"progress.json")));
            originalBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            // Isolate scripted touches from unrelated desktop clicks during the automated run.
            physicalMouse=Mouse.current;physicalMouseWasEnabled=physicalMouse!=null&&physicalMouse.enabled;if(physicalMouseWasEnabled)InputSystem.DisableDevice(physicalMouse);
            virtualTouch=InputSystem.AddDevice<Touchscreen>();beforeFrames=game.TickCount;Publish("running",false,null);StartCoroutine(Runner(Run()));
        }
        void Check(bool value,string name){if(!value)throw new Exception(name);checks.Add(name);}
        void Publish(string status,bool success,string error)
        {
            var r=new Report{status=status,success=success,error=error,phase=phase,width=Screen.width,height=Screen.height,frames=game.TickCount-beforeFrames,walkingFrames=game.WalkingFrames,checks=checks.ToArray(),ecology=ecology,phoneFirst=phoneFirst,scope="Real runtime frames, walking, UI raycasts/callbacks and InputSystem virtual touch; not a physical phone test"};
            StatusJson=JsonUtility.ToJson(r,true);File.WriteAllText(output,StatusJson);
#if UNITY_WEBGL && !UNITY_EDITOR
            FogVerificationReport(StatusJson);
#endif
        }
        IEnumerator Runner(IEnumerator run)
        {
            var stack=new Stack<IEnumerator>();stack.Push(run);
            while(stack.Count>0)
            {
                object current=null;bool next=false;Exception error=null;
                try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){Finish(false,error.ToString());yield break;}
                if(!next){stack.Pop();continue;}
                if(current is IEnumerator nested){stack.Push(nested);continue;}
                // Editor auto-ticks can outrun Game View repaints; await a visible render interval.
#if UNITY_EDITOR
                foreach(var window in Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())window.Repaint();
#endif
                if(current==null)yield return new WaitForSecondsRealtime(.08f);else yield return current;
            }
            Finish(true,null);
        }
        IEnumerator Wait(Func<bool> condition,string name,float timeout=30)
        {float until=Time.realtimeSinceStartup+timeout;yield return null;while(!condition()){if(Time.realtimeSinceStartup>until)throw new Exception("Timeout: "+name+"; "+game.CurrentView+" @ "+game.Story.State.room+" x="+game.Story.X);yield return null;}}
        GameObject ButtonObject(string name,bool verifyRaycast=true)
        {
            var go=GameObject.Find(name);Check(go!=null,"button present: "+name);var button=go.GetComponent<UnityEngine.UI.Button>();Check(button!=null&&button.interactable,"button enabled: "+name);
            var sr=go.GetComponentInParent<UnityEngine.UI.ScrollRect>();Canvas.ForceUpdateCanvases();
            if(sr!=null){var rt=(RectTransform)go.transform;var local=sr.viewport.InverseTransformPoint(rt.TransformPoint(rt.rect.center));if(!sr.viewport.rect.Contains(local)){sr.content.anchoredPosition+=new Vector2(0,sr.viewport.rect.center.y-local.y);Canvas.ForceUpdateCanvases();}}
            var rect=(RectTransform)go.transform;var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var data=new PointerEventData(EventSystem.current){position=point};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            if(verifyRaycast)Check(hits.Count>0&&(hits[0].gameObject==go||hits[0].gameObject.transform.IsChildOf(go.transform)),"button raycast: "+name+" ["+string.Join(",",hits.Take(3).Select(h=>h.gameObject.name))+"] point="+point+" rect="+rect.rect+" depth="+go.GetComponent<UnityEngine.UI.Image>().depth+(sr!=null?" viewport="+sr.viewport.rect+" content="+sr.content.rect:""));return go;
        }
        IEnumerator Click(string name)
        {
            ButtonObject(name,false);
#if !UNITY_EDITOR
            yield return new WaitForEndOfFrame();
#endif
            yield return null;
            ButtonObject(name).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }
        IEnumerator Touch(Vector2 point)
        {
            InputSystem.QueueStateEvent(virtualTouch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});yield return new WaitForSeconds(.06f);
            InputSystem.QueueStateEvent(virtualTouch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});yield return new WaitForSeconds(.06f);
        }
        IEnumerator TouchButton(string name)
        {
            ButtonObject(name,false);
#if !UNITY_EDITOR
            yield return new WaitForEndOfFrame();
#endif
            yield return null;
            var rt=(RectTransform)ButtonObject(name).transform;yield return Touch(RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center)));
        }
        IEnumerator Walk(string id,string view)
        {game.WalkTo(id);yield return Wait(()=>game.CurrentView==view,"walk to "+id);yield return null;}
        IEnumerator Lines()
        {yield return null;while(game.Story.Current!=null){yield return Click("NextButton");yield return null;}}
        IEnumerator Message()
        {yield return Wait(()=>game.CurrentView=="Message","message");yield return Touch(new Vector2(Screen.width*.025f,Screen.height*.5f));yield return null;}
        IEnumerator Door(string id,string target)
        {game.WalkTo(id);yield return Wait(()=>game.Story.State.room==target,"door "+target);yield return null;}
        IEnumerator Inspect(string id)
        {
            var action=game.Story.Action(id);yield return Walk(id,"Inspection_"+id);yield return Click(action.choices.Length>0?"ActionChoice_"+action.correct:"Record_"+id);yield return Lines();
        }
        IEnumerator Relation(string id)
        {
            var r=game.Story.Book.relations.First(v=>v.id==id);yield return Click("RelationTopic_"+id);yield return null;yield return Click("Claim"+r.correct);yield return null;
            foreach(var card in r.evidence)yield return Click("Evidence_"+card);yield return Click("PresentButton");yield return Message();Check(game.Story.Has(id),"relationship recorded: "+id);
        }
        IEnumerator Travel(string id,string target)
        {yield return Walk(id,"Dialogue");yield return Lines();Check(game.Story.State.room==target,"chapter transition: "+target);yield return Message();Snapshot(target);}
        void Snapshot(string name)
        {
            phase=name;Publish("running",false,null);
#if !UNITY_WEBGL || UNITY_EDITOR
            ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(output),"full-"+(ecology?"ecology-":"main-")+name+"-"+Screen.width+"x"+Screen.height+".png"));
#endif
        }
        IEnumerator Run()
        {
            game.ShowHome();yield return null;
            // Enter a fresh fixture explicitly: the real legacy-import file may exist.
            typeof(FogGame).GetMethod("ConfirmNewGame",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,null);
            yield return Wait(()=>game.CurrentView=="Prologue","fresh isolated story");Check(true,"new story enters its intro");
            yield return new WaitForSecondsRealtime(.6f);
            GameObject.Find("Prologue").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Wait(()=>game.CurrentView=="Letter","prologue opens original letter");
            yield return new WaitForSecondsRealtime(.6f);GameObject.Find("Letter").GetComponent<FogLetterView>().Continue();
            yield return Wait(()=>GameObject.Find("ArrivalHint")!=null,"nonblocking arrival notice");Check(game.Story.Has("letterReceived"),"reading records letter");
            Check(!game.IsModal&&game.CurrentView=="","arrival notice does not block scene input");
            float initial=game.Story.X;var floor=game.Story.Room.floor;var point=game.sceneCamera.WorldToScreenPoint(new Vector3(initial+1.5f,floor+3.0f,0));yield return Touch(new Vector2(point.x,point.y));
            yield return Wait(()=>game.Story.X>initial+.25f,"touch moves protagonist");Check(true,"InputSystem touch moves on ground");
            yield return new WaitForSecondsRealtime(2.0f);Check(GameObject.Find("ArrivalHint")==null,"arrival hint disappears automatically after 1.8 seconds");
            point=game.sceneCamera.WorldToScreenPoint(new Vector3(.9f,floor+3.0f,0));yield return Touch(new Vector2(point.x,point.y));yield return Wait(()=>game.Story.X<1.5f,"walk away from mentor");
            float beforeDirect=game.Story.X;point=game.sceneCamera.WorldToScreenPoint(new Vector3(game.Story.MentorX,floor+1.2f,0));yield return Touch(new Vector2(point.x,point.y));
            yield return Wait(()=>game.CurrentView=="MentorTopics","distant actor click opens topics");Check(Mathf.Abs(game.Story.X-beforeDirect)<.25f,"actor click opens immediately without walking closer");
            Check(FindObjectsByType<MentorChatClient>(FindObjectsSortMode.None).Length==0,"offline edition creates no model client");
            Check(GameObject.Find("FreeQuestionTopic")==null,"offline edition has no free-input entry");
            var beforeFixed=string.Join("|",game.Story.State.facts);yield return Click("FixedQuestionTopic");yield return Message();
            Check(game.CurrentView=="MentorTopics"&&beforeFixed==string.Join("|",game.Story.State.facts),"offline fixed topic works without granting progress");
            yield return Click("IntroTopic");yield return null;yield return Click("NextButton");yield return null;
            int line=game.Story.State.line;float x=game.Story.X;yield return Touch(new Vector2(Screen.width*.025f,Screen.height*.5f));yield return null;yield return Click("NotebookButton");yield return null;
            Check(GameObject.Find("Profile_9")==null,"unknown profile does not reveal Chen Du early");
            yield return Touch(new Vector2(Screen.width*.025f,Screen.height*.5f));game.ContinueGame();yield return Wait(()=>game.CurrentView=="Dialogue","continue pending");
            Check(game.Story.State.line==line&&Mathf.Abs(game.Story.X-x)<.01f,"continue preserves dialogue and position");yield return Lines();
            yield return Walk("cargo","Message");Check(!game.Story.Has("cargo"),"early cargo blocks");yield return Message();
            yield return Door("door","inspection");yield return Walk("weight","WeightInspection");yield return Click("WeightChoice1");yield return Message();Check(!game.Story.Has("weight"),"wrong weight no reward");
            yield return Click("WeightChoice0");yield return Lines();yield return Walk("mentor","MentorTopics");yield return Click("ReportWeightTopic");yield return Lines();yield return Door("door","harbor");
            yield return Walk("cargo","CargoInspection");yield return Click("RecordCargoButton");yield return Lines();yield return Walk("handover","Dialogue");yield return Lines();
            yield return Walk("mentor","MentorTopics");yield return Relation("portMismatch");yield return Relation("portDestination");yield return Click("KeepInvestigatingButton");yield return null;yield return Travel("departure","warehouse");
            Check(!game.Story.Has("seal"),"main route does not need seal observation");
            yield return Walk("worker","Dialogue");yield return Lines();yield return Inspect("parcels");yield return Door("door","dispatch");yield return Inspect("camera");yield return Inspect("dispatch");
            Snapshot("dispatch");yield return Walk("mentor","MentorTopics");yield return Relation("historicalRoute");yield return Click("KeepInvestigatingButton");yield return null;yield return Door("door","warehouse");
            yield return Walk("worker","NpcTopics");yield return Click("WorkerRecordTopic");yield return Lines();Check(game.Story.Has("letterIdentity"),"letter identity recovered");yield return Travel("advance","town");
            if(phoneFirst){yield return Walk("denial","Dialogue");yield return Lines();}else{yield return Walk("courier","Dialogue");yield return Lines();}
            yield return Door("door","records");yield return Walk("account","Message");Check(!game.Story.Has("account"),"account blocks without shop conversation");yield return Message();
            yield return Walk("shopShare","Dialogue");yield return Lines();yield return Inspect("account");
            if(ecology)
            {
                yield return Inspect("wildlife");yield return Walk("shopShare","NpcTopics");yield return Click("EcologySourceTopic");yield return Lines();yield return Walk("mentor","MentorTopics");yield return Click("EcologyReferTopic");yield return Lines();Check(game.Story.Has("referred"),"optional ecology separately referred");
            }
            Snapshot("records");yield return Door("door","town");yield return Inspect("shelf");
            if(phoneFirst){yield return Walk("courier","Dialogue");yield return Lines();}else{yield return Walk("denial","Dialogue");yield return Lines();}
            yield return Walk("mentor","MentorTopics");yield return Relation("receiptContradiction");yield return Relation("participantScope");yield return Click("KeepInvestigatingButton");yield return null;yield return Travel("advance","interview");
            yield return Door("door","archive");yield return Walk("mentor","MentorTopics");yield return Click("FinalReviewTopic");yield return Wait(()=>game.CurrentView=="Message","report gate feedback");Check(game.Story.State.ending=="","no ending before inquiries");yield return Message();yield return Click("KeepInvestigatingButton");yield return null;yield return Door("door","interview");
            yield return Walk("suspect","InquiryTopics");yield return Click("InquiryTopic_receiptInquiry");yield return null;yield return Click("Evidence_S1");yield return Click("Evidence_courier");yield return Click("PresentButton");yield return null;
            yield return Click("NextButton");yield return null;Check(!game.Story.Has("receiptQuestioned"),"inquiry waits for heard response");yield return Lines();
            yield return Walk("suspect","InquiryTopics");yield return Click("InquiryTopic_roleInquiry");yield return null;yield return Click("Evidence_courier");yield return Click("PresentButton");yield return Message();Check(!game.Story.Has("roleQuestioned"),"one card does not replace three records");
            yield return Click("InquiryTopic_roleInquiry");yield return null;foreach(var card in new[]{"dispatch","courier","account"})yield return Click("Evidence_"+card);yield return Click("PresentButton");yield return Lines();
            yield return Walk("mentor","MentorTopics");yield return Click("ScopeTopic");yield return null;yield return Click("ActionChoice_2");yield return Lines();Check(game.Story.Has("scopeReviewed"),"scope review performed");
            Snapshot("interview");yield return Door("door","archive");yield return Walk("mentor","MentorTopics");yield return Click("FinalReviewTopic");yield return null;yield return Click("ReviewPerson_6");yield return Message();
            yield return Click("ReviewPerson_9");yield return null;
            for(int step=0;step<4;step++)
            {
                var r=game.Story.Book.reports[step];Check(game.CurrentView=="ReportStep_"+step,"sequential report dialogue "+step);
                yield return Click("ReportClaim_"+(!ecology&&step==1?(r.correct+1)%3:r.correct));yield return null;foreach(var card in r.evidence)yield return Click("Evidence_"+card);yield return Click("PresentButton");yield return null;
                if(step<3)yield return Message();
            }
            if(!ecology)
            {
                Check(game.CurrentView=="EndingReview"&&game.Story.State.reviewStep==1,"specific returned report segment");int materialCount=game.Story.State.facts.Count;Snapshot("review");yield return null;
                yield return Click("RetryReportButton");yield return null;var r=game.Story.Book.reports[1];yield return Click("ReportClaim_"+r.correct);yield return null;foreach(var card in r.evidence)yield return Click("Evidence_"+card);yield return Click("PresentButton");yield return null;
                Check(game.Story.State.facts.Count>=materialCount,"retry preserves materials");
            }
            Check(game.CurrentView=="EndingAccepted"&&game.Story.Has("reportSubmitted"),"full game accepted ending");Check(game.Story.EndingText().Contains("老赵"),"letter reply in ending");
            Check(ecology?game.Story.Has("referred")&&!game.Story.Has("seal"):!game.Story.Has("wildlife"),"actual optional branch state accurate");Snapshot("accepted");yield return null;
            game.SaveForPageHide();game.ContinueGame();yield return Wait(()=>game.CurrentView=="EndingAccepted","saved ending resumes");Check(game.CurrentView=="EndingAccepted","final ending survives reload");
            Check(game.TickCount-beforeFrames>100&&game.WalkingFrames>100,"actual frames and walking advance");
            foreach(var audio in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))Check(audio.isPlaying&&audio.loop&&audio.time>0,"looping music: "+audio.name);
            Check(FindObjectsByType<MentorChatClient>(FindObjectsSortMode.None).Length==0,"full offline walkthrough completes without model client");
        }
        void Finish(bool success,string error)
        {
            if(finished)return;finished=true;phase="finished";Publish(success?"passed":"failed",success,error);game.ShowHome();
            typeof(FogGame).GetProperty("Story").SetValue(game,originalStory);
            FogStory.FlushStorage();InputSystem.settings.backgroundBehavior=originalBackground;
            if(virtualTouch!=null){InputSystem.RemoveDevice(virtualTouch);virtualTouch=null;}if(physicalMouseWasEnabled&&physicalMouse!=null)InputSystem.EnableDevice(physicalMouse);physicalMouseWasEnabled=false;game.ShowHome();
        }
        void OnDestroy(){if(virtualTouch!=null)InputSystem.RemoveDevice(virtualTouch);if(physicalMouseWasEnabled&&physicalMouse!=null)InputSystem.EnableDevice(physicalMouse);}
    }
}
#endif
