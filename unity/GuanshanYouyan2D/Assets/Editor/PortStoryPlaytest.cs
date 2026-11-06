using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuanshanYouyan.Story;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GuanshanYouyan.Editor
{
    // Exercises the actual running controller and Button callbacks. No completion flags are injected.
    public static class PortStoryPlaytest
    {
        static IEnumerator routine;static Wait waiting;static PortGame game;
        static List<string> checks;static double deadline;static int beforeFrames,beforeTicks;
        static string savePath;static Dictionary<string,byte[]> saveBackup;
        class Wait{public Func<bool> condition;public Wait(Func<bool> p){condition=p;}}
        public static string Start()
        {
            if(routine!=null)throw new Exception("Probe already running");
            game=UnityEngine.Object.FindFirstObjectByType<PortGame>();if(game==null||!EditorApplication.isPlaying)throw new Exception("Enter Play mode first");
            savePath=game.Story.SavePath;saveBackup=new Dictionary<string,byte[]>();
            foreach(var suffix in new[]{"",".bak",".previous",".tmp"})saveBackup[suffix]=File.Exists(savePath+suffix)?File.ReadAllBytes(savePath+suffix):null;
            checks=new List<string>();beforeFrames=game.WalkingFrames;beforeTicks=game.TickCount;routine=Run();waiting=null;
            File.WriteAllText("ImportReports/port-playtest-status.json","{\"status\":\"running\"}");EditorApplication.update+=Tick;return "Running actual UI walkthrough";
        }
        static void Check(bool value,string name){if(!value)throw new Exception(name);checks.Add(name);}
        static void Click(string name)
        {
            var go=GameObject.Find(name);Check(go!=null,"button exists: "+name);var button=go.GetComponent<UnityEngine.UI.Button>();Check(button!=null&&button.interactable,"button enabled: "+name);
            // Verify the button is within the scroll viewport, scrolling it into view when needed.
            var sr=button.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            if(sr!=null)
            {
                Canvas.ForceUpdateCanvases();var rt=(RectTransform)button.transform;
                var point=sr.viewport.InverseTransformPoint(rt.TransformPoint(rt.rect.center));
                if(!sr.viewport.rect.Contains(point))
                {
                    float shift=sr.viewport.rect.center.y-point.y;sr.content.anchoredPosition+=new Vector2(0,shift);Canvas.ForceUpdateCanvases();
                }
            }
            var rect=(RectTransform)button.transform;var screen=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var pd=new PointerEventData(EventSystem.current){position=screen};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pd,hits);
            Check(hits.Count>0 && (hits[0].gameObject==go||hits[0].gameObject.transform.IsChildOf(go.transform)),"button center receives raycast: "+name);
            button.onClick.Invoke();
        }
        static IEnumerator Dialogue()
        {while(game.Story.Current!=null){Click("NextButton");yield return null;}}
        static Wait View(string view)=>new Wait(()=>game.CurrentView==view);
        static Wait Walk(string id,string expected){game.WalkTo(id);return View(expected);}
        static IEnumerator Run()
        {
            game.ShowHome();yield return null;Click("NewGameButton");yield return null;Check(game.CurrentView=="Prologue","new game opens prologue");
            Click("PauseDialogueButton");yield return View("Message");Check(game.Story.Has("letterReceived"),"skip preserves letter summary");Click("AcknowledgeButton");yield return null;
            yield return Walk("mentor","MentorTopics");Click("IntroTopic");yield return null;Click("NextButton");yield return null;
            int line=game.Story.State.line;float pos=game.Story.X;
            Click("PauseDialogueButton");yield return null;Click("NotebookButton");yield return null;Click("SaveHomeButton");yield return null;
            Click("ContinueButton");yield return View("Dialogue");Check(game.Story.State.line==line&&Mathf.Abs(game.Story.X-pos)<.02f,"save/continue restores paused dialogue and position");
            var dialogue=Dialogue();while(dialogue.MoveNext())yield return dialogue.Current;
            yield return Walk("cargo","Message");Check(!game.Story.Has("cargo"),"actual cargo hotspot blocks early result");Click("AcknowledgeButton");yield return null;
            yield return Walk("door","");yield return new Wait(()=>game.Story.State.room=="inspection");
            yield return Walk("weight","WeightInspection");Click("WeightChoice1");yield return View("Message");Check(!game.Story.Has("weight"),"wrong weight choice gives no record");
            Click("AcknowledgeButton");yield return View("WeightInspection");Click("WeightChoice0");yield return View("Dialogue");
            dialogue=Dialogue();while(dialogue.MoveNext())yield return dialogue.Current;
            yield return Walk("mentor","MentorTopics");Click("ReportWeightTopic");yield return View("Dialogue");dialogue=Dialogue();while(dialogue.MoveNext())yield return dialogue.Current;
            yield return Walk("door","");yield return new Wait(()=>game.Story.State.room=="harbor");
            yield return Walk("cargo","CargoInspection");Click("RecordCargoButton");yield return View("Dialogue");dialogue=Dialogue();while(dialogue.MoveNext())yield return dialogue.Current;
            yield return Walk("handover","Dialogue");dialogue=Dialogue();while(dialogue.MoveNext())yield return dialogue.Current;
            yield return Walk("mentor","MentorTopics");Click("MismatchTopic");yield return View("RelationClaims");Click("Claim2");yield return View("EvidencePresentation");
            Click("Evidence_weight");Click("Evidence_cargo");Click("PresentButton");yield return View("Message");Check(!game.Story.Has("portMismatch"),"bad assertion rejected through real UI");
            Click("AcknowledgeButton");yield return View("RelationClaims");Click("Claim1");yield return View("EvidencePresentation");Click("Evidence_weight");Click("Evidence_cargo");Click("PresentButton");yield return View("Message");
            Check(game.Story.Has("portMismatch"),"anomaly presentation recorded");Click("AcknowledgeButton");yield return View("MentorTopics");
            Click("DestinationTopic");yield return View("RelationClaims");Click("Claim0");yield return View("EvidencePresentation");Click("Evidence_handover");Click("PresentButton");yield return View("Message");
            Check(game.Story.Has("portDestination"),"destination presentation recorded");Click("AcknowledgeButton");yield return View("MentorTopics");Click("KeepInvestigatingButton");yield return null;
            yield return Walk("departure","Dialogue");dialogue=Dialogue();while(dialogue.MoveNext())yield return dialogue.Current;
            yield return View("Act1Complete");Check(game.Story.Has("act1Complete")&&!game.Story.Has("seal"),"full UI route finishes without optional observation");
            Check(game.WalkingFrames>beforeFrames+100&&game.TickCount>beforeTicks+100,"real frames and walking animation advanced");
            foreach(var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))Check(source.isPlaying&&source.loop&&source.time>0,"music playing and looping: "+source.name);
            Click("CompleteSaveButton");yield return View("Home");Click("ContinueButton");yield return View("Act1Complete");Check(game.Story.Has("act1Complete"),"completed chapter survives continue");
            yield return null;
        }
        static void Tick()
        {
            try
            {
                if(waiting!=null){if(!waiting.condition()){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out in view "+game.CurrentView+", room "+game.Story.State.room);return;}waiting=null;}
                if(!routine.MoveNext()){Finish(true,null);return;}
                if(routine.Current is Wait w){waiting=w;deadline=EditorApplication.timeSinceStartup+30;}
            }
            catch(Exception e){Finish(false,e.ToString());}
        }
        static void Finish(bool success,string error)
        {
            EditorApplication.update-=Tick;routine=null;waiting=null;
            File.WriteAllText("ImportReports/port-playtest-status.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {status=success?"passed":"failed",success,error,checks=checks.ToArray(),width=Screen.width,height=Screen.height,scope="Editor mouse/button path and live frames, not a physical touch-device test"},Newtonsoft.Json.Formatting.Indented));
            // Stop runtime autosaving before restoring any user save.
            game.ShowHome();
            foreach(var kv in saveBackup){if(kv.Value==null){if(File.Exists(savePath+kv.Key))File.Delete(savePath+kv.Key);}else File.WriteAllBytes(savePath+kv.Key,kv.Value);}
        }
    }
}
