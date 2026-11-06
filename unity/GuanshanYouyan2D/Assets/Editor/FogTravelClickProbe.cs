using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GuanshanYouyan.Story;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GuanshanYouyan.Editor
{
    // Exercise the actual pointer handler with in-memory progress; never replace player saves.
    public sealed class FogTravelClickProbe
    {
        FogGame game;
        FogStory original, fixture;
        Touchscreen touch;
        Mouse mouse;
        bool mouseEnabled;
        InputSettings.BackgroundBehavior background;
        string output;
        int frames;
        readonly List<object> checks=new List<object>();
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Start(string report="travel-click-probe.json")
        {
            var game=UnityEngine.Object.FindFirstObjectByType<FogGame>();
            if(game==null)throw new Exception("Enter full-story Play mode first");
            var probe=new FogTravelClickProbe();
            probe.game=game;probe.output=Path.GetFullPath("ImportReports/"+report);
            probe.original=game.Story;
            probe.fixture=FogStoryRegression.Fresh();
            FogStoryRegression.Act1(probe.fixture);FogStoryRegression.Act2(probe.fixture);
            typeof(FogGame).GetProperty("Story").SetValue(game,probe.fixture);
            typeof(FogGame).GetField("home",Private).SetValue(game,false);
            typeof(FogGame).GetField("arrival",Private).SetValue(game,-1f);
            probe.background=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            probe.mouse=Mouse.current;probe.mouseEnabled=probe.mouse!=null&&probe.mouse.enabled;
            if(probe.mouseEnabled)InputSystem.DisableDevice(probe.mouse);
            probe.touch=InputSystem.AddDevice<Touchscreen>();probe.frames=game.TickCount;
            game.StartCoroutine(probe.Run());return "Started isolated pointer-route probe";
        }
        void Place(string room,float x)
        {
            typeof(FogGame).GetMethod("ClearModal",Private).Invoke(game,null);
            FogStoryRegression.Go(fixture,room,x);
            typeof(FogGame).GetMethod("ApplyRoom",Private).Invoke(game,new object[]{true});
        }
        IEnumerator Tap(float x,float y)
        {
            var p=game.sceneCamera.WorldToScreenPoint(new Vector3(x,y,0));
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=17,phase=UnityEngine.InputSystem.TouchPhase.Began,position=new Vector2(p.x,p.y)});
            yield return new WaitForSecondsRealtime(.12f);
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=17,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=new Vector2(p.x,p.y)});
            yield return new WaitForSecondsRealtime(.12f);
        }
        IEnumerator Run()
        {
            Place("warehouse",21.5f);yield return new WaitForSecondsRealtime(.3f);
            yield return Tap(23.1f,.2f);
            checks.Add(new{name="upper right departure goes to town",expected="town",actual=fixture.State.room,success=fixture.State.room=="town"});
            ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(output),Path.GetFileNameWithoutExtension(output)+"-right-exit.png"));
            yield return new WaitForSecondsRealtime(.15f);
            Place("warehouse",21.5f);yield return new WaitForSecondsRealtime(.15f);
            yield return Tap(23.1f,fixture.Room.floor+1.2f);
            checks.Add(new{name="lower right departure goes to town",expected="town",actual=fixture.State.room,success=fixture.State.room=="town"});
            var townReload=new FogStory(fixture.Book);townReload.Restore(JsonUtility.ToJson(fixture.State));
            checks.Add(new{name="continuing after travel stays in town",success=townReload.State.room=="town"});
            Place("warehouse",19.0f);yield return new WaitForSecondsRealtime(.15f);
            yield return Tap(19.0f,.3f);
            checks.Add(new{name="staircase still opens dispatch",expected="dispatch",actual=fixture.State.room,success=fixture.State.room=="dispatch"});
            var save=JsonUtility.ToJson(fixture.State);var restored=new FogStory(fixture.Book);restored.Restore(save);
            checks.Add(new{name="room and evidence survive restore",success=restored.State.room==fixture.State.room&&new HashSet<string>(restored.State.facts).SetEquals(fixture.State.facts)});
            fixture=FogStoryRegression.Fresh();FogStoryRegression.Act1(fixture);
            FogStoryRegression.Take(fixture,"workerIntro");FogStoryRegression.Take(fixture,"parcels");
            FogStoryRegression.Take(fixture,"camera");FogStoryRegression.Take(fixture,"dispatch");
            FogStoryRegression.Relate(fixture,"historicalRoute");FogStoryRegression.Take(fixture,"worker");
            typeof(FogGame).GetProperty("Story").SetValue(game,fixture);
            Place("warehouse",21.5f);yield return new WaitForSecondsRealtime(.15f);yield return Tap(23.1f,.2f);
            checks.Add(new{name="first departure starts correct dialogue",success=fixture.State.pending=="warehouseDeparture"&&game.CurrentView=="Dialogue"});
            while(fixture.Current!=null){game.AdvanceDialogue();yield return null;}
            checks.Add(new{name="first departure arrives in town",success=fixture.State.room=="town"&&fixture.Has("act2Complete")});
            ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(output),Path.GetFileNameWithoutExtension(output)+"-town.png"));
            yield return new WaitForSecondsRealtime(.15f);
            string json=Newtonsoft.Json.JsonConvert.SerializeObject(checks);
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{success=!json.Contains("\"success\":false"),frames=game.TickCount-frames,checks,scope="Actual InputSystem pointer handling in running Unity, independent in-memory investigation"},Newtonsoft.Json.Formatting.Indented));
            Restore();
        }
        void Restore()
        {
            if(touch!=null){InputSystem.RemoveDevice(touch);touch=null;}
            if(mouseEnabled&&mouse!=null)InputSystem.EnableDevice(mouse);mouseEnabled=false;
            InputSystem.settings.backgroundBehavior=background;
            if(original!=null){typeof(FogGame).GetProperty("Story").SetValue(game,original);original=null;game.ShowHome();}
        }
        void OnDestroy(){Restore();}
    }
}
