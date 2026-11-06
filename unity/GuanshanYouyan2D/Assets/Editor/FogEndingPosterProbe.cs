using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using GuanshanYouyan.Story;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GuanshanYouyan.Editor
{
    public sealed class FogEndingPosterProbe
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        FogGame game;FogStory original;int checks;float start;
        public static string Start()
        {
            var probe=new FogEndingPosterProbe{game=UnityEngine.Object.FindFirstObjectByType<FogGame>(),start=Time.unscaledTime};
            if(probe.game?.Story==null)throw new Exception("Enter ticking Play mode");
            probe.original=probe.game.Story;var fixture=new FogStory(probe.original.Book);
            fixture.Restore(File.ReadAllText("ImportReports/ending-browser-fixture.json"));
            typeof(FogGame).GetProperty("Story").SetValue(probe.game,fixture);
            typeof(FogGame).GetMethod("ShowSuccessEnding",Private).Invoke(probe.game,null);
            probe.game.StartCoroutine(probe.Run());return "Isolated exact lettering / autoplay / wheel / drag / end / resize test started";
        }
        void Check(bool yes,string name){checks++;if(!yes)throw new Exception(name);}
        IEnumerator Verify()
        {
            yield return null;Canvas.ForceUpdateCanvases();
            var poster=UnityEngine.Object.FindFirstObjectByType<FogEndingPosterText>();
            var roll=UnityEngine.Object.FindFirstObjectByType<FogEndingRoll>();
            var scroll=UnityEngine.Object.FindFirstObjectByType<FogEndingScrollRect>();
            Check(poster!=null&&roll!=null&&scroll!=null,"original art with ordered scroll controller");
            var image=poster.GetComponent<UnityEngine.UI.Image>();Check(image!=null&&image.sprite.name=="SuccessPosterClean","original supplied illustration retained");
            var texts=poster.GetComponentsInChildren<TMPro.TMP_Text>();
            Check(texts.Length==20&&FogEndingPosterText.Copy.Sum(group=>group.Length)==19,"title plus nineteen exact supplied lines");
            Check(texts.Skip(1).Select(t=>t.text).SequenceEqual(File.ReadAllLines("../../output/结尾海报文案-用户原文.txt").Where(t=>t.Length>0)),"copy matches user text in exact order");
            foreach(var text in texts){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"no text overflow "+text.text);Check(text.font.HasCharacters(text.text),"font has every character "+text.text);}
            Check(texts.Skip(1).Max(t=>t.fontSize)-texts.Skip(1).Min(t=>t.fontSize)<.01f,"all body lines share one coordinated font size");
            float before=roll.Progress;yield return new WaitForSecondsRealtime(.8f);
            Check(roll.Progress>before&&roll.AutoPlaying,"opening starts moving automatically without input");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("../../output/结尾海报-新文字-顶部-Editor.png"));
            var wheel=new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-4),position=new Vector2(600,400)};
            scroll.OnScroll(wheel);yield return null;float manual=roll.Progress;
            yield return new WaitForSecondsRealtime(.6f);Check(Mathf.Abs(roll.Progress-manual)<.001f,"wheel pauses briefly for reading");
            yield return new WaitForSecondsRealtime(.8f);Check(roll.Progress>manual&&roll.AutoPlaying,"wheel automatically resumes without click");
            wheel.scrollDelta=new Vector2(0,3);scroll.OnScroll(wheel);yield return null;manual=roll.Progress;
            yield return new WaitForSecondsRealtime(1.45f);Check(roll.Progress>manual,"upward wheel also resumes");
            var drag=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=new Vector2(600,350)};
            scroll.OnBeginDrag(drag);drag.position=new Vector2(600,500);scroll.OnDrag(drag);yield return null;
            Check(!roll.AutoPlaying,"drag suspends automatic movement");scroll.OnEndDrag(drag);manual=roll.Progress;
            yield return new WaitForSecondsRealtime(1.45f);Check(roll.Progress>manual&&roll.AutoPlaying,"drag release automatically resumes");
            scroll.verticalNormalizedPosition=.55f;yield return null;
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("../../output/结尾海报-新文字-中段-Editor.png"));
            float progress=roll.Progress;var viewport=scroll.viewport;var size=viewport.sizeDelta;viewport.sizeDelta=size+new Vector2(-360,0);
            yield return null;Canvas.ForceUpdateCanvases();yield return null;
            Check(Mathf.Abs(roll.Progress-progress)<.003f,"resize keeps reading position");
            Check(Mathf.Abs(scroll.content.rect.height/scroll.content.rect.width-3)<.001f,"poster keeps aspect after resize");
            viewport.sizeDelta=size;yield return null;Canvas.ForceUpdateCanvases();yield return null;
            scroll.verticalNormalizedPosition=0;yield return new WaitForSecondsRealtime(.25f);Check(roll.Finished,"end shows final paragraph");
            float end=scroll.content.anchoredPosition.y;yield return new WaitForSecondsRealtime(.25f);
            Check(Mathf.Abs(scroll.content.anchoredPosition.y-end)<.1f,"end remains held");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("../../output/结尾海报-新文字-底部-Editor.png"));
            wheel.scrollDelta=new Vector2(0,4);scroll.OnScroll(wheel);yield return null;manual=roll.Progress;
            yield return new WaitForSecondsRealtime(1.45f);Check(roll.Progress>manual&&!roll.Finished,"scrolling up from end restarts automatic movement");
            scroll.enabled=false;scroll.verticalNormalizedPosition=1;yield return null;
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("../../output/结尾海报-协调字号-顶部-Editor.png"));yield return null;scroll.enabled=true;
        }
        IEnumerator Run()
        {
            Exception error=null;var routine=Verify();while(true){bool next=false;try{next=routine.MoveNext();}catch(Exception e){error=e;}if(error!=null||!next)break;yield return routine.Current;}
            typeof(FogGame).GetProperty("Story").SetValue(game,original);game.ShowHome();
            File.WriteAllText("ImportReports/ending-lettering-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,checks,seconds=Time.unscaledTime-start,error=error?.ToString()},Newtonsoft.Json.Formatting.Indented));
            if(error!=null)Debug.LogException(error);
        }
    }
}
