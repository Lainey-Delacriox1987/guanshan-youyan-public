using System;
using System.Collections;
using System.IO;
using System.Reflection;
using GuanshanYouyan.Story;
using UnityEngine;

namespace GuanshanYouyan.Editor
{
    public sealed class FogOpeningDriveProbe
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        FogGame game;FogStory original,fixture;bool originalHome;int checks;
        void Check(bool yes,string name){checks++;if(!yes)throw new Exception(name);}
        void Invoke(string name,params object[] args)=>typeof(FogGame).GetMethod(name,Private).Invoke(game,args);
        void Use(FogStory story){typeof(FogGame).GetProperty("Story").SetValue(game,story);}
        public static string Start()
        {
            var p=new FogOpeningDriveProbe();p.game=UnityEngine.Object.FindFirstObjectByType<FogGame>();
            if(p.game?.Story==null)throw new Exception("Enter ticking Play mode first");
            p.original=p.game.Story;p.originalHome=(bool)typeof(FogGame).GetField("home",Private).GetValue(p.game);
            p.fixture=FogStoryRegression.Fresh();p.fixture.NewGame();p.Use(p.fixture);
            typeof(FogGame).GetField("home",Private).SetValue(p.game,false);
            foreach(var audio in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))audio.Pause();
            p.game.StartCoroutine(p.Run());return "Started isolated opening drive, save and arrival verification";
        }
        IEnumerator Verify()
        {
            fixture.State.line=1;game.ShowDialogue();yield return null;
            Check(game.CurrentView=="Letter"&&!fixture.Has("letterReceived"),"letter remains before road sequence");
            Invoke("CompleteLetter");yield return null;
            Check(game.CurrentView=="OpeningDrive"&&fixture.Has("letterReceived"),"letter completion begins road sequence");
            Check(fixture.State.openingDrivePending&&fixture.State.pending=="","cutscene checkpoint saved independently of dialogue");
            var view=GameObject.Find("OpeningDrive").GetComponent<FogOpeningDriveView>();
            Check(view.Shot==0&&view.BlackAlpha>.7f,"first shot begins from black");
            Check(GameObject.Find("OpeningDrive").GetComponentsInChildren<TMPro.TMP_Text>().Length==0,"no cutscene text or visible control labels");
            var vehicle=view.Vehicle;var image=vehicle.GetComponent<UnityEngine.UI.Image>();
            Check(image.preserveAspect&&vehicle.localScale==Vector3.one,"vehicle has natural aspect");
            var front=GameObject.Find("OpeningForegroundTrunks").GetComponent<FogDriveForeground>();
            Check(front.transform.GetSiblingIndex()>vehicle.GetSiblingIndex()&&front.transform.GetSiblingIndex()<GameObject.Find("OpeningBlackFade").transform.GetSiblingIndex(),"near scenery renders above car and below fades");
            Check(!front.raycastTarget&&front.canvasRenderer!=null,"foreground accepts no clicks and has CanvasRenderer");
            float previousX=vehicle.anchoredPosition.x;yield return new WaitForSecondsRealtime(.75f);
            var road=GameObject.Find("OpeningRoad").GetComponent<UnityEngine.UI.Image>();
            Check(vehicle.rect.width/road.rectTransform.rect.width>.59f&&vehicle.rect.width/road.rectTransform.rect.width<.63f,"near-shot van matches supplied reference framing");
            Check(vehicle.anchoredPosition.x>previousX&&view.BlackAlpha<.02f,"car enters from left as black fades away");
            previousX=vehicle.anchoredPosition.x;yield return new WaitForSecondsRealtime(.6f);
            Check(vehicle.anchoredPosition.x>previousX,"car travels right");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("ImportReports/drive-tree-editor.png"));
            while(view.Elapsed<FogOpeningDriveView.ShotDuration-.18f)yield return null;
            Check(view.BlackAlpha>.5f,"first shot fades back to black");
            while(view.Shot<1)yield return null;
            Check(fixture.State.openingDriveShot==1&&view.BlackAlpha>.7f,"second supplied road resumes from black and checkpoints");
            Check(GameObject.Find("OpeningRoad").GetComponent<UnityEngine.UI.Image>().sprite.name=="Road2","second road uses supplied ordering");
            yield return new WaitForSecondsRealtime(2.8f);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("ImportReports/drive-rail-editor.png"));
            while(view.Shot<2)yield return null;
            Check(fixture.State.openingDriveShot==2&&view.BlackAlpha>.7f,"third shot begins from black");
            Check(GameObject.Find("OpeningRoad").GetComponent<UnityEngine.UI.Image>().sprite.name=="Road3","third road is harbor gate");
            while(view.Elapsed<FogOpeningDriveView.ParkAt+.08f)yield return null;
            var parked=vehicle.anchoredPosition;yield return new WaitForSecondsRealtime(.65f);
            Check(Vector2.Distance(parked,vehicle.anchoredPosition)<.1f&&view.BlackAlpha<.02f,"third shot holds parked before fade");
            var cab=road.rectTransform.InverseTransformPoint(vehicle.TransformPoint(new Vector3(vehicle.rect.xMax,0,0)));
            float cabX=(cab.x-road.rectTransform.rect.xMin)/road.rectTransform.rect.width;
            Check(cabX>.635f&&cabX<.817f,"cab stops in front of the pictured gate opening");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("ImportReports/drive-parked-editor.png"));
            float timeout=Time.unscaledTime+6;while(game.CurrentView=="OpeningDrive"&&Time.unscaledTime<timeout)yield return null;
            Check(game.CurrentView=="ArrivalReveal"&&!fixture.State.openingDrivePending,"road sequence ends on black harbor reveal");
            yield return new WaitForSecondsRealtime(2.1f);
            Check(game.CurrentView==""&&fixture.State.room=="harbor"&&Mathf.Abs(fixture.X-4.1f)<.1f,"playable harbor arrival restored");
            // Restore a page-hidden checkpoint in each supplied shot.
            for(int shot=0;shot<3;shot++)
            {
                var saved=FogStoryRegression.Fresh();saved.State.openingDrivePending=true;saved.State.openingDriveShot=shot;
                var restored=new FogStory(saved.Book);restored.Restore(JsonUtility.ToJson(saved.State));
                Check(restored.State.openingDrivePending&&restored.State.openingDriveShot==shot,"saved shot survives narrative replay "+shot);
            }
            var browser=FogStoryRegression.Fresh();browser.State.openingDrivePending=true;browser.State.openingDriveShot=0;
            File.WriteAllText("ImportReports/opening-drive-browser-fixture.json",JsonUtility.ToJson(browser.State,true));
            Use(browser);Invoke("ApplyRoom",true);Invoke("ShowOpeningDrive");yield return new WaitForSecondsRealtime(.55f);
            GameObject.Find("OpeningDrive").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(.65f);Check(!browser.State.openingDrivePending&&game.CurrentView=="ArrivalReveal","click skips through black into harbor");
            yield return new WaitForSecondsRealtime(2f);
            var legacy=FogStoryRegression.Fresh();var reload=new FogStory(legacy.Book);reload.Restore(JsonUtility.ToJson(legacy.State));
            Check(!reload.State.openingDrivePending,"existing investigation saves do not replay opening");
        }
        IEnumerator Run()
        {
            Exception error=null;var routine=Verify();while(true){bool next=false;try{next=routine.MoveNext();}catch(Exception e){error=e;}if(error!=null||!next)break;yield return routine.Current;}
            Use(original);typeof(FogGame).GetField("home",Private).SetValue(game,originalHome);game.ShowHome();
            File.WriteAllText("ImportReports/opening-drive-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,checks,shots=3,seconds=2*FogOpeningDriveView.ShotDuration+FogOpeningDriveView.FinalShotDuration,error=error?.ToString()},Newtonsoft.Json.Formatting.Indented));
            if(error!=null)Debug.LogException(error);
        }
    }
}
