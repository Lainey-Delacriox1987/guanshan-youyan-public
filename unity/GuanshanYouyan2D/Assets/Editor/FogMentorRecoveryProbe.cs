using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using GuanshanYouyan.Story;

namespace GuanshanYouyan.Editor
{
    // Editor-only failure-path verification; never bundled as a model or production endpoint.
    public sealed class FogMentorRecoveryProbe
    {
        public string StatusJson {get;private set;}="";
        FogGame game;MentorChatClient client;string endpoint;int timeout,initialFrames;bool finished;
        readonly Dictionary<string,byte[]> originals=new Dictionary<string,byte[]>();
        readonly List<string> checks=new List<string>();
        static FogMentorRecoveryProbe running;Stack<IEnumerator> stack;double nextTick;GameObject label;
        string Output=>Path.GetFullPath("ImportReports/mentor-unity-recovery.json");
        public static string StartProbe()
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play first");
            if(running!=null&&!running.finished)throw new Exception("Recovery probe already exists");
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            foreach(var window in Resources.FindObjectsOfTypeAll<EditorWindow>())if(window.titleContent.text=="Game")window.Focus();
            running=new FogMentorRecoveryProbe();running.Begin();return "Started failure-only probe; no model inference";
        }
        void Begin()
        {
            game=UnityEngine.Object.FindFirstObjectByType<FogGame>();
            if(game!=null)game.EnableExperimentalMentorChatForEditor();
            client=UnityEngine.Object.FindFirstObjectByType<MentorChatClient>();
            if(game==null||client==null)throw new Exception("Full game/client missing");
            foreach(var suffix in new[]{"",".bak",".previous",".tmp"})originals[suffix]=File.Exists(game.Story.SavePath+suffix)?File.ReadAllBytes(game.Story.SavePath+suffix):null;
            endpoint=client.endpoint;timeout=client.timeoutSeconds;initialFrames=game.TickCount;
            client.endpoint="http://127.0.0.1:39573";client.timeoutSeconds=1;
            label=new GameObject("Failure-only QA label",typeof(RectTransform),typeof(TextMeshProUGUI));
            var rt=(RectTransform)label.transform;rt.SetParent(GameObject.Find("StoryCanvas").transform,false);rt.anchorMin=new Vector2(0,1);rt.anchorMax=Vector2.one;rt.pivot=new Vector2(.5f,1);rt.anchoredPosition=new Vector2(0,-8);rt.sizeDelta=new Vector2(0,42);
            var text=label.GetComponent<TextMeshProUGUI>();text.font=game.chineseFont;text.text="故障恢复验收 · 仅测试超时/取消 · 没有模型或生成回复";text.fontSize=22;text.color=Color.yellow;text.raycastTarget=false;
            Publish("running",false,null);stack=new Stack<IEnumerator>();stack.Push(Run());nextTick=EditorApplication.timeSinceStartup;
            EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=PlayState;AssemblyReloadEvents.beforeAssemblyReload+=BeforeReload;
        }
        void Check(bool condition,string name){if(!condition)throw new Exception(name);checks.Add(name);}
        void Publish(string status,bool success,string error)
        {
            var report=new{status,success,error,fixture=true,realModelResponseVerified=false,scope="Actual Editor game frames, UI raycasts/callbacks and UnityWebRequest timeout/cancel/retry. Failure-only HTTP fixture, not real AI response acceptance.",frames=game!=null?game.TickCount-initialFrames:0,checks,verifiedAt=DateTime.UtcNow.ToString("o")};
            StatusJson=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);File.WriteAllText(Output,StatusJson);
        }
        void Tick()
        {
            if(EditorApplication.timeSinceStartup<nextTick)return;nextTick=EditorApplication.timeSinceStartup+.1;
            if(!EditorApplication.isPlaying){Finish(false,"Play stopped during recovery verification");return;}
            while(stack.Count>0)
            {
                object current=null;bool next=false;Exception error=null;
                try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){Finish(false,error.ToString());return;}
                if(!next){stack.Pop();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}
                foreach(var window in Resources.FindObjectsOfTypeAll<EditorWindow>())window.Repaint();
                return;
            }
            Finish(true,null);
        }
        IEnumerator Wait(Func<bool> condition,string name,float seconds=30)
        {var end=Time.realtimeSinceStartup+seconds;yield return null;while(!condition()){if(Time.realtimeSinceStartup>end)throw new Exception("Timeout: "+name+" @ "+game.CurrentView);yield return null;}}
        IEnumerator Click(string name)
        {
            yield return null;var go=GameObject.Find(name);Check(go!=null,"button present: "+name);
            var button=go.GetComponent<UnityEngine.UI.Button>();Check(button!=null&&button.interactable,"button enabled: "+name);
            Canvas.ForceUpdateCanvases();var sr=go.GetComponentInParent<UnityEngine.UI.ScrollRect>();var rt=(RectTransform)go.transform;
            if(sr!=null){var local=sr.viewport.InverseTransformPoint(rt.TransformPoint(rt.rect.center));if(!sr.viewport.rect.Contains(local)){sr.content.anchoredPosition+=new Vector2(0,sr.viewport.rect.center.y-local.y);Canvas.ForceUpdateCanvases();}}
            yield return null;
            var point=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            Check(hits.Count>0&&(hits[0].gameObject==go||hits[0].gameObject.transform.IsChildOf(go.transform)),"button raycast: "+name);
            button.onClick.Invoke();yield return null;
        }
        IEnumerator Walk(string id,string view){game.WalkTo(id);yield return Wait(()=>game.CurrentView==view,"walk "+id);}
        void EnterQuestion()
        {var input=GameObject.Find("ChatInput").GetComponent<TMP_InputField>();input.text="这是故障恢复测试，不请求任何模型回答。";}
        IEnumerator Run()
        {
            yield return Wait(()=>client.Ready,"failure fixture health",25);Check(true,"failure fixture reached through actual Unity client");
            game.ShowHome();yield return Click("NewGameButton");yield return Wait(()=>game.CurrentView=="Prologue","prologue");
            yield return Click("PauseDialogueButton");yield return Wait(()=>game.CurrentView=="Message","arrival");yield return Click("AcknowledgeButton");
            yield return Walk("mentor","MentorTopics");yield return Click("FreeQuestionTopic");
            var facts=string.Join("|",game.Story.State.facts);var journal=game.Story.State.journal.Count;
            EnterQuestion();yield return Click("SendQuestionButton");Check(client.Sending,"actual request is sending");
            yield return Wait(()=>!client.Sending,"real UnityWebRequest timeout",8);
            yield return Wait(()=>GameObject.Find("RetryQuestionButton")!=null,"retry after timeout");Check(true,"timeout exposes retry and keeps investigation controls");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("ImportReports/mentor-timeout-editor.png"));yield return null;
            client.timeoutSeconds=10;yield return Click("RetryQuestionButton");Check(client.Sending,"retry starts another request");yield return new WaitForSecondsRealtime(.3f);
            yield return Click("CancelQuestionButton");Check(!client.Sending,"cancel button aborts the pending request");
            Check(GameObject.Find("RetryQuestionButton")!=null,"cancel remains retryable");
            EnterQuestion();yield return Click("SendQuestionButton");yield return new WaitForSecondsRealtime(.3f);Check(client.Sending,"another pending question exists before close");
            yield return Click("CloseChatButton");Check(!client.Sending&&!game.IsModal,"closing chat aborts request and resumes scene");
            Check(facts==string.Join("|",game.Story.State.facts)&&journal==game.Story.State.journal.Count,"failed questions do not award facts or rewrite event proofs");
            game.WalkTo("door");yield return Wait(()=>game.Story.State.room=="inspection","enter office after failure");
            yield return Walk("weight","WeightInspection");yield return Click("WeightChoice0");
            while(game.Story.Current!=null)yield return Click("NextButton");
            Check(game.Story.Has("weight"),"normal mainline gains weight evidence after timeout and cancellation");
            Check(!client.Sending&&game.CurrentView!="MentorChat","late failed request does not reopen chat");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("ImportReports/mentor-recovery-mainline-editor.png"));yield return null;
            Check(game.TickCount-initialFrames>100&&game.WalkingFrames>0,"actual frames and walking advanced");
        }
        void Finish(bool success,string error)
        {
            if(finished)return;finished=true;
            EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=PlayState;AssemblyReloadEvents.beforeAssemblyReload-=BeforeReload;
            if(client!=null){client.Cancel();client.endpoint=endpoint;client.timeoutSeconds=timeout;}
            if(game!=null)game.ShowHome();
            foreach(var kv in originals){var file=game.Story.SavePath+kv.Key;if(kv.Value==null){if(File.Exists(file))File.Delete(file);}else File.WriteAllBytes(file,kv.Value);}
            Publish(success?"passed":"failed",success,error);
            if(label!=null)UnityEngine.Object.Destroy(label);
        }
        void PlayState(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Finish(false,"Play exited during verification");}
        void BeforeReload()=>Finish(false,"Assembly reload interrupted verification");
    }
}
