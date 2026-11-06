using System;
using System.Collections;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using GuanshanYouyan.Story;

namespace GuanshanYouyan.Editor
{
    public sealed class FogLetterCenterProbe
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        FogGame game;FogStory original;bool oldHome;int checks;
        void Check(bool yes,string name){checks++;if(!yes)throw new Exception(name);}
        void Invoke(string name,params object[] args)=>typeof(FogGame).GetMethod(name,Private).Invoke(game,args);
        public static string Start()
        {
            var p=new FogLetterCenterProbe();p.game=UnityEngine.Object.FindFirstObjectByType<FogGame>();
            if(p.game?.Story==null)throw new Exception("Enter Play mode first");
            p.original=p.game.Story;p.oldHome=(bool)typeof(FogGame).GetField("home",Private).GetValue(p.game);
            typeof(FogGame).GetProperty("Story").SetValue(p.game,FogStoryRegression.Fresh());
            typeof(FogGame).GetField("home",Private).SetValue(p.game,false);
            foreach(var audio in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))audio.Pause();
            p.game.StartCoroutine(p.Run());return "Isolated centered-letter and parked-arrival check started";
        }
        IEnumerator Verify()
        {
            game.ShowLetter();yield return null;Canvas.ForceUpdateCanvases();
            var text=GameObject.Find("LetterHandwriting").GetComponent<TMP_Text>();text.ForceMeshUpdate();
            var root=GameObject.Find("LetterContent").GetComponent<RectTransform>();
            Check(text.alignment==TextAlignmentOptions.Center,"letter text centered");
            Check(Vector2.Distance(text.rectTransform.anchoredPosition,Vector2.zero)<.1f,"text block centered in viewport");
            Check(!text.isTextTruncated,"complete letter remains visible");
            var button=GameObject.Find("FinishLetterButton").GetComponent<UnityEngine.UI.Button>();
            Check(button.GetComponentInChildren<TMP_Text>().text=="继续","exact short label without arrow");
            Check(((RectTransform)button.transform).rect.width<root.rect.width*.33f,"compact button");
            var sheet=GameObject.Find("LetterSheet").GetComponent<RectTransform>();
            Check(sheet.rect.width>=root.rect.width-.1f&&sheet.rect.height>=root.rect.height-.1f,"paper still covers viewport");
            Invoke("BeginHarborArrival",true);yield return null;
            var shuttle=(Transform)typeof(FogGame).GetField("shuttle",Private).GetValue(game);
            var hero=(SpriteRenderer)typeof(FogGame).GetField("hero",Private).GetValue(game);var position=shuttle.position;
            Check(hero.gameObject.activeSelf,"player visible immediately at parked harbor");
            for(int frame=0;frame<12;frame++){yield return new WaitForSecondsRealtime(.1f);Check(Vector3.Distance(shuttle.position,position)<.0001f,"harbor vehicle remains parked "+frame);}
            Check(game.CurrentView==""&&Mathf.Abs(position.x-2.4f)<.001f,"black reveal ends with parked vehicle");
        }
        IEnumerator Run()
        {
            Exception error=null;var r=Verify();while(true){bool next=false;try{next=r.MoveNext();}catch(Exception e){error=e;}if(error!=null||!next)break;yield return r.Current;}
            typeof(FogGame).GetProperty("Story").SetValue(game,original);typeof(FogGame).GetField("home",Private).SetValue(game,oldHome);Invoke("ApplyRoom",true);game.ShowHome();
            File.WriteAllText("ImportReports/letter-center-parked-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,checks,error=error?.ToString()},Newtonsoft.Json.Formatting.Indented));
            if(error!=null)Debug.LogException(error);
        }
    }
}
