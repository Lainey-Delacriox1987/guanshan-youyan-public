using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GuanshanYouyan.Story;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GuanshanYouyan.Editor
{
    public sealed class FogMusicProbe
    {
        const string Key="guanshan-mist-music-gain-v1";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        FogGame game;FogStory original;
        bool hadPreference,mouseEnabled;float preference;
        Mouse mouse;Touchscreen touch;InputSettings.BackgroundBehavior background;
        readonly List<string> checks=new List<string>();
        public static string Start()
        {
            var p=new FogMusicProbe();p.game=UnityEngine.Object.FindFirstObjectByType<FogGame>();
            if(p.game==null||p.game.Story==null)throw new Exception("Enter Play mode first");
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
            foreach(var window in Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())if(window.titleContent.text=="Game")window.Focus();
            p.original=p.game.Story;p.hadPreference=PlayerPrefs.HasKey(Key);p.preference=PlayerPrefs.GetFloat(Key,1);
            typeof(FogGame).GetProperty("Story").SetValue(p.game,FogStoryRegression.Fresh());
            typeof(FogGame).GetField("home",Private).SetValue(p.game,false);
            PlayerPrefs.SetFloat(Key,1);p.Invoke("ApplyMusic");
            p.background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            p.mouse=Mouse.current;p.mouseEnabled=p.mouse!=null&&p.mouse.enabled;if(p.mouseEnabled)InputSystem.DisableDevice(p.mouse);
            p.touch=InputSystem.AddDevice<Touchscreen>();p.game.StartCoroutine(p.Run());return "Started isolated music pointer test";
        }
        void Invoke(string method)=>typeof(FogGame).GetMethod(method,Private).Invoke(game,null);
        AudioSource[] Audio()=>UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);}
        IEnumerator TapButton(string name)
        {
            var go=GameObject.Find(name);Check(go!=null,"button exists: "+name);Canvas.ForceUpdateCanvases();
            var sr=go.GetComponentInParent<UnityEngine.UI.ScrollRect>();if(sr!=null){sr.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();}
            yield return new WaitForSecondsRealtime(.2f);
            var rt=(RectTransform)go.transform;var pos=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));
            var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=pos};
            var hits=new List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&(hits[0].gameObject==go||hits[0].gameObject.transform.IsChildOf(go.transform)),"button raycast: "+name);
            go.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);
        }
        IEnumerator Run()
        {
            Exception failure=null;var stack=new Stack<IEnumerator>();stack.Push(Verify());
            try
            {
                while(stack.Count>0)
                {
                    object current=null;bool next=false;
                    try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}catch(Exception e){failure=e;break;}
                    if(!next){stack.Pop();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
                }
                File.WriteAllText("ImportReports/music-toggle-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{success=failure==null,error=failure?.ToString(),checks,scope="Actual running AudioSources and UI raycasts/callbacks; browser pointer evidence recorded separately; independent case state and original preferences restored"},Newtonsoft.Json.Formatting.Indented));
            }
            finally
            {
                if(touch!=null)InputSystem.RemoveDevice(touch);if(mouseEnabled)InputSystem.EnableDevice(mouse);InputSystem.settings.backgroundBehavior=background;
                if(hadPreference)PlayerPrefs.SetFloat(Key,preference);else PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();
                typeof(FogGame).GetProperty("Story").SetValue(game,original);game.ShowHome();
                foreach(var audio in Audio()){audio.mute=true;audio.Pause();}
            }
        }
        IEnumerator Verify()
        {
            yield return new WaitForSecondsRealtime(.3f);Check(Audio().Length==2&&Audio().All(a=>a.isPlaying&&!a.mute&&a.volume>0),"both tracks initially playing");
            game.ShowNotebook();yield return TapButton("MusicButton");
            Check(game.Story.State.musicGain==0&&Audio().All(a=>a.volume==0&&a.mute&&!a.isPlaying),"music off mutes and pauses both tracks");
            var button=GameObject.Find("MusicButton");Check(button.GetComponent<FogMusicGraphic>().Muted,"speaker icon confirms muted state");
            Check(button.transform.parent.name=="StoryCanvas"&&GameObject.Find("MusicToggle")==null,"music control independent of notebook scroll");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("ImportReports/music-toggle-off.png"));yield return null;
            yield return TapButton("MusicButton");Check(game.Story.State.musicGain==1&&Audio().All(a=>!a.mute&&a.isPlaying&&a.volume>0),"music on resumes both tracks");
            yield return TapButton("MusicButton");game.ShowHome();Check(Audio().All(a=>a.mute&&!a.isPlaying),"return to cover stays silent");
            Invoke("ConfirmNewGame");Check(game.Story.State.musicGain==0&&Audio().All(a=>a.mute&&!a.isPlaying),"new story keeps music disabled");
            var saved=JsonUtility.ToJson(game.Story.State);var other=new FogStory(game.Story.Book);other.Restore(saved);Check(other.State.musicGain==0,"save restore retains disabled setting");
            typeof(FogGame).GetProperty("Story").SetValue(game,new FogStory(game.Story.Book));Invoke("RestoreMusicPreference");Invoke("ApplyMusic");
            Check(game.Story.State.musicGain==0&&Audio().All(a=>a.mute&&!a.isPlaying),"startup preference silences title before Continue");
        }
    }
}
