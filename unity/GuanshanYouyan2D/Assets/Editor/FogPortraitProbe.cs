using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GuanshanYouyan.Story;
using UnityEngine;

namespace GuanshanYouyan.Editor
{
    public sealed class FogPortraitProbe
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        FogGame game;FogStory original,fixture;bool originalHome;int checks,lines;readonly HashSet<int> roles=new HashSet<int>();
        readonly List<object> measurements=new List<object>();
        static readonly string[] Names={"林见","许岚","值班员","老赵","苏禾","周叔","陈渡"};
        void Invoke(string name,params object[] args)=>typeof(FogGame).GetMethod(name,Private).Invoke(game,args);
        void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
        public static string Start()
        {
            var p=new FogPortraitProbe();p.game=UnityEngine.Object.FindFirstObjectByType<FogGame>();
            if(p.game?.Story==null)throw new Exception("Enter ticking Play mode first");
            p.original=p.game.Story;p.originalHome=(bool)typeof(FogGame).GetField("home",Private).GetValue(p.game);
            p.fixture=FogStoryRegression.Fresh();FogStoryRegression.Act1(p.fixture);FogStoryRegression.Act2(p.fixture);FogStoryRegression.Act3(p.fixture);FogStoryRegression.Inquiries(p.fixture);
            typeof(FogGame).GetProperty("Story").SetValue(p.game,p.fixture);typeof(FogGame).GetField("home",Private).SetValue(p.game,false);
            foreach(var audio in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))audio.Pause();
            p.game.StartCoroutine(p.Run());return "Started all-speaker presentation probe on isolated story";
        }
        void Portrait(string name,int actor,float textLeft)
        {
            Canvas.ForceUpdateCanvases();var image=GameObject.Find(name)?.GetComponent<UnityEngine.UI.Image>();Check(image!=null,"portrait exists "+name);
            var root=(RectTransform)typeof(FogGame).GetField("canvasRoot",Private).GetValue(game);var rt=image.rectTransform;float target=FogPortraitLayout.Height(root.rect.size);
            Check(Mathf.Abs(rt.rect.height-target)<.1f,"same medium visible height "+actor);
            Check(Mathf.Abs(rt.rect.width/rt.rect.height-image.sprite.rect.width/image.sprite.rect.height)<.001f,"natural aspect "+actor);
            Check(rt.localScale==Vector3.one&&!image.raycastTarget,"no independent scale or input block "+actor);
            Check(rt.anchoredPosition.x+rt.rect.width*.5f<=root.rect.width*textLeft+.1f,"clear of text "+actor);
            Check(rt.anchoredPosition.x-rt.rect.width*.5f>=0&&rt.anchoredPosition.y==0,"on screen and bottom aligned "+actor);
            var panel=(RectTransform)(GameObject.Find("DialogueBox")??GameObject.Find("CharacterPanel")).transform;
            Check(panel.anchorMin==FogPortraitLayout.PanelMin&&panel.anchorMax==FogPortraitLayout.PanelMax,"all character dialogue panels use identical bounds "+actor);
            Check(Mathf.Abs(rt.anchoredPosition.x-root.rect.width*(root.rect.width/root.rect.height<.85f?.185f:.18f))<.1f,"common portrait column "+actor);
            roles.Add(actor);
        }
        IEnumerator Verify()
        {
            var portraits=(Sprite[])typeof(FogGame).GetField("portraits",Private).GetValue(game);
            Check(FogPortraitLayout.Height(new Vector2(935,871))>871*.49f,"small panel uses enlarged common portrait height");
            Check(Mathf.Abs(FogPortraitLayout.Height(new Vector2(1920,1080))-1080*.60f)<.01f,"wide view uses sixty percent height for every character");
            foreach(var size in new[]{new Vector2(935,871),new Vector2(1098,1022),new Vector2(1280,720),new Vector2(1920,1080),new Vector2(390,844),new Vector2(844,390)})
            {
                float common=FogPortraitLayout.Height(size);for(int actor=3;actor<10;actor++)
                {
                    var test=new GameObject("PortraitGeometry",typeof(RectTransform));var rt=(RectTransform)test.transform;
                    foreach(float right in new[]{FogPortraitLayout.ColumnRight})
                    {
                        FogPortraitLayout.Fit(rt,size,portraits[actor],right);Check(Mathf.Abs(rt.rect.height-common)<.01f,"all cast same height "+size);
                        float edge=rt.anchoredPosition.x+rt.rect.width*.5f;Check(edge<size.x*.36f,"responsive text clearance "+size);
                        Check(rt.anchoredPosition.x-rt.rect.width*.5f>=0&&rt.rect.height<size.y*.61f,"responsive bounds "+size);
                    }
                    measurements.Add(new{size=new[]{size.x,size.y},actor,height=common,width=common*portraits[actor].rect.width/portraits[actor].rect.height});UnityEngine.Object.Destroy(test);
                }
            }
            foreach(var action in fixture.Book.actions)
            {
                fixture.State.room=action.room==""||action.room=="*"?fixture.Book.rooms.First(r=>r.act==action.act).id:action.room;
                Invoke("ApplyRoom",true);fixture.State.pending=action.id;
                for(int line=0;line<action.lines.Length;line++)
                {
                    fixture.State.line=line;game.ShowDialogue();yield return null;lines++;
                    int actor=Array.IndexOf(Names,action.lines[line].speaker)+3;
                    if(action.id!="prologue"&&actor>=3)Portrait("SpeakerPortrait",actor,.36f);
                    else Check(GameObject.Find("SpeakerPortrait")==null,"narration has no wrong portrait");
                }
            }
            fixture.State.pending="";fixture.State.line=0;
            for(int actor=3;actor<10;actor++){Invoke("ShowProfile",actor);yield return null;Portrait("TopicPortrait",actor,.36f);}
            foreach(var room in fixture.Book.rooms)
            {
                fixture.State.room=room.id;Invoke("ApplyRoom",true);Invoke("ShowMentor");yield return null;Portrait("TopicPortrait",4,.36f);
                foreach(var a in room.actors)Check(a.actor>=3&&a.actor<=9,"known scene cast "+room.id);
            }
            fixture.State.room="interview";Invoke("ApplyRoom",true);Invoke("ShowInquiries");yield return null;Portrait("TopicPortrait",9,.36f);
            fixture.State.room="archive";Invoke("ApplyRoom",true);
            for(int step=0;step<4;step++){Invoke("ShowReportStep",step);yield return null;Portrait("TopicPortrait",4,.36f);}
            foreach(int actor in new[]{6,7}){Invoke("ShowNpc",actor);yield return null;Portrait("TopicPortrait",actor,.36f);}
            for(int actor=3;actor<10;actor++){game.Message(Names[actor-3],"请先核对记录。");yield return null;Portrait("TopicPortrait",actor,.36f);}
            Invoke("ShowRelation",fixture.Book.relations[0].id);yield return null;Portrait("TopicPortrait",4,.36f);
            Invoke("ShowFinalIntro");yield return null;
            var thumbs=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None).Where(i=>i.gameObject.name=="Portrait").ToArray();Check(thumbs.Length==4,"four summary portraits");
            float thumbnailHeight=-1;foreach(var thumb in thumbs){float height=Mathf.Min(thumb.rectTransform.rect.height,thumb.rectTransform.rect.width*thumb.sprite.rect.height/thumb.sprite.rect.width);if(thumbnailHeight<0)thumbnailHeight=height;Check(Mathf.Abs(height-thumbnailHeight)<1,"summary cast equal visual height");}
            Check(roles.Count==7,"every story speaker covered");
            // Valid browser fixture for the exact Lin Jian -> Chen Du -> Xu Lan transition.
            var browser=FogStoryRegression.Fresh();FogStoryRegression.Act1(browser);FogStoryRegression.Act2(browser);FogStoryRegression.Act3(browser);FogStoryRegression.Take(browser,"receiptInquiry");FogStoryRegression.Go(browser,"interview",12.8f);
            Check(browser.Begin("roleInquiry",-1,new[]{"dispatch","courier","account"})==null,"valid dialogue fixture");
            browser.Position(9.0f,false);
            File.WriteAllText("ImportReports/portrait-dialogue-fixture.json",JsonUtility.ToJson(browser.State,true));
        }
        IEnumerator Run()
        {
            Exception failure=null;var routine=Verify();while(true){bool next=false;try{next=routine.MoveNext();}catch(Exception e){failure=e;}if(failure!=null||!next)break;yield return routine.Current;}
            typeof(FogGame).GetProperty("Story").SetValue(game,original);typeof(FogGame).GetField("home",Private).SetValue(game,originalHome);game.ShowHome();
            File.WriteAllText("ImportReports/portrait-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{success=failure==null,checks,dialogueLines=lines,roles=roles.OrderBy(x=>x),rooms=8,measurements,error=failure?.ToString()},Newtonsoft.Json.Formatting.Indented));
            if(failure!=null)Debug.LogException(failure);
        }
    }
}
