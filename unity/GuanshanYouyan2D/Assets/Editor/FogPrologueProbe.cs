using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Linq;
using TMPro;
using GuanshanYouyan.Story;
using UnityEngine;

namespace GuanshanYouyan.Editor
{
    public sealed class FogPrologueProbe
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        FogGame game;FogStory original,fixture;bool originalHome;int checks;
        void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
        void Invoke(string name,params object[] args)=>typeof(FogGame).GetMethod(name,Private).Invoke(game,args);
        public static string Start()
        {
            var p=new FogPrologueProbe();p.game=UnityEngine.Object.FindFirstObjectByType<FogGame>();
            if(p.game?.Story==null)throw new Exception("Enter Play mode first");
            p.original=p.game.Story;p.originalHome=(bool)typeof(FogGame).GetField("home",Private).GetValue(p.game);
            p.fixture=FogStoryRegression.Fresh();p.fixture.NewGame();
            typeof(FogGame).GetProperty("Story").SetValue(p.game,p.fixture);typeof(FogGame).GetField("home",Private).SetValue(p.game,false);
            foreach(var audio in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))audio.Pause();
            p.game.StartCoroutine(p.Run());return "Started isolated prologue visual and progression probe";
        }
        void Inspect()
        {
            Canvas.ForceUpdateCanvases();var root=GameObject.Find("Prologue");
            Check(game.CurrentView=="Prologue"&&root!=null,"dedicated prologue view");
            Check(root.GetComponent<UnityEngine.UI.Image>().color==Color.black,"opaque pure black background");
            var captions=root.GetComponentsInChildren<TMP_Text>();Check(captions.Length==1,"only one text line and no old headings");
            var label=captions[0];Check(label.text==FogPrologueView.Caption,"exact requested caption");
            Check(label.font!=null&&label.font.name=="LongCang-Intro","real handwritten font bound");
            Check(FogPrologueView.Caption.All(c=>label.font.HasCharacter(c)),"every glyph present without fallback");
            Check(label.textWrappingMode==TextWrappingModes.NoWrap&&label.alignment==TextAlignmentOptions.Center,"single centered line");
            Check(root.transform.Find("DialogueBox")==null&&root.transform.Find("SaveClose")==null,"no former dialogue panel or skip controls");
            var rt=label.rectTransform;Check(rt.anchorMin==new Vector2(.06f,.43f)&&rt.anchorMax==new Vector2(.94f,.57f),"caption centered on screen");
            label.ForceMeshUpdate();Check(label.textInfo.lineCount==1&&!label.isTextTruncated,"caption not wrapped or clipped");
        }
        IEnumerator Verify()
        {
            game.ShowDialogue();yield return null;Inspect();
            var first=GameObject.Find("Prologue");game.ShowDialogue();yield return null;
            Check(GameObject.Find("Prologue")==first,"redraw does not restart intro");
            yield return new WaitForSecondsRealtime(10.9f);
            Check(game.CurrentView=="Prologue"&&!fixture.Has("letterReceived"),"intro waits for click beyond former auto timeout");
            GameObject.Find("Prologue").GetComponent<FogPrologueView>().Continue();
            yield return new WaitForSecondsRealtime(.8f);
            Check(game.CurrentView=="Letter"&&fixture.State.pending=="prologue","intro opens paper letter before completing prerequisite");
            InspectLetter();
            Check(!fixture.Has("letterReceived"),"unread letter not marked complete");
            yield return new WaitForSecondsRealtime(16.2f);
            Check(game.CurrentView=="Letter"&&!fixture.Has("letterReceived"),"letter waits for click beyond former auto timeout");
            GameObject.Find("Letter").GetComponent<FogLetterView>().Continue();
            yield return new WaitForSecondsRealtime(.8f);
            Check(game.CurrentView!="Letter","paper letter fades away after click");
            Check(fixture.Has("letterReceived")&&fixture.State.completed.Contains("prologue"),"letter prerequisites retained");
            Check(fixture.Book.actions.First(a=>a.id=="prologue").lines[1].text.Contains("0615"),"original letter still available in notebook");
            GameObject.Find("OpeningDrive").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(.8f);
            yield return new WaitForSecondsRealtime(2.1f);
            Check(fixture.State.room=="harbor"&&Mathf.Abs(fixture.X-4.1f)<.1f,"arrival returns control in harbor");
            foreach(int oldLine in new[]{1,2})
            {
                fixture.NewGame();fixture.State.line=oldLine;game.ShowDialogue();yield return null;InspectLetter();
                yield return new WaitForSecondsRealtime(.5f);
                GameObject.Find("Letter").GetComponent<FogLetterView>().Continue();
                yield return new WaitForSecondsRealtime(.8f);
                Check(fixture.Has("letterReceived")&&fixture.State.pending=="","click continues migrated prologue line "+oldLine);
                yield return new WaitForSecondsRealtime(2f);
            }
            File.WriteAllText("ImportReports/prologue-browser-fixture.json",JsonUtility.ToJson(new FogSave{pending="prologue",line=0},true));
        }
        void InspectLetter()
        {
            Canvas.ForceUpdateCanvases();
            Check(game.CurrentView=="Letter","dedicated paper letter view");
            var image=GameObject.Find("LetterSheet").GetComponent<UnityEngine.UI.Image>();
            Check(image.sprite!=null&&image.sprite.name=="LetterPaper","supplied paper bound");
            var text=GameObject.Find("LetterHandwriting").GetComponent<TMP_Text>();text.ForceMeshUpdate();
            Check(text.font.name=="LongCang-Letter","handwritten letter atlas bound");
            Check(text.text.StartsWith("致林见：")&&text.text.EndsWith("请求帮忙核对。")&&!text.text.Contains("一下"),"exact requested addressee and closing request");
            Check(GameObject.Find("LetterContinueButton")==null&&GameObject.Find("LetterFooter")==null,"no visible continue control");
            Check(text.text.Contains("原始入库联")&&text.text.Contains("乳霜"),"rewritten concrete record discrepancy");
            Check(!text.isTextTruncated&&!text.isTextOverflowing,"letter text complete");
            var area=(RectTransform)text.transform.parent;
            Check(area.anchorMin==new Vector2(.19f,.31f)&&area.anchorMax==new Vector2(.79f,.745f),"text stays within brown paper safe area");
        }
        IEnumerator Run()
        {
            Exception error=null;var routine=Verify();while(true){bool next=false;try{next=routine.MoveNext();}catch(Exception e){error=e;}if(error!=null||!next)break;yield return routine.Current;}
            typeof(FogGame).GetProperty("Story").SetValue(game,original);typeof(FogGame).GetField("home",Private).SetValue(game,originalHome);game.ShowHome();
            File.WriteAllText("ImportReports/prologue-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,checks,error=error?.ToString()},Newtonsoft.Json.Formatting.Indented));
            if(error!=null)Debug.LogException(error);
        }
    }
}
