using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace GuanshanYouyan.Story
{
    public sealed class PortGame : MonoBehaviour
    {
        public TextAsset storyData; public TMP_FontAsset chineseFont;
        public Sprite harborArt, officeArt, castArt; public Material spriteMaterial;
        public Camera sceneCamera;
        public PortStory Story {get;private set;}
        public bool IsModal => modal != null;
        public string CurrentView {get;private set;} = "";
        public int WalkingFrames {get;private set;}
        public int TickCount {get;private set;}
        RectTransform canvasRoot,modal,body,marker; TMP_Text placeLabel; UnityEngine.UI.Button nearButton;
        bool Portrait => Screen.height>Screen.width;
        SpriteRenderer backdrop,foreground,hero,mentor,guard; Transform shuttle;
        float Floor => Story.Book.rooms.First(r=>r.id==Story.State.room).floor;
        Sprite[] people=new Sprite[6],portraits=new Sprite[6];
        float targetX,walkClock,lastSaved,arrival=-1,placeUntil; string pendingTarget="";
        bool home=true; int hintLevel; readonly Color ink=new Color(.055f,.09f,.12f,.96f);
        readonly Color paper=new Color(.91f,.89f,.80f,1),gold=new Color(.91f,.65f,.30f,1);
        struct Hotspot {public string id,title;public float x;public Hotspot(string i,string t,float p){id=i;title=t;x=p;}}
        Hotspot[] Spots => Story.State.room=="harbor"?new[]{new Hotspot("departure","接驳车",2.4f),new Hotspot("mentor","许岚",5.4f),new Hotspot("handover","值班员",7.5f),new Hotspot("seal","交接标签",11.7f),new Hotspot("cargo","查验货箱",15.1f),new Hotspot("door","查验办公室",21.5f)}:
          new[]{new Hotspot("door","返回堆场",2.4f),new Hotspot("weight","重量记录",8.1f),new Hotspot("mentor","许岚",12.7f)};
        void Start()
        {
            Story=new PortStory(JsonUtility.FromJson<PortBook>(storyData.text),Path.Combine(Application.persistentDataPath,"guanshan-mist-story-v2-port.json"));
            BuildWorld();BuildUI();ShowHome();
        }
        SpriteRenderer Actor(string name,Sprite sprite,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;
            if(spriteMaterial!=null)sr.sharedMaterial=spriteMaterial;return sr;
        }
        void BuildWorld()
        {
            var tex=castArt.texture;
            for(int row=0;row<2;row++)for(int col=0;col<3;col++)
            {
                people[row*3+col]=Sprite.Create(tex,new Rect(col*tex.width/3f,row==0?tex.height*.53125f:0,tex.width/3f,tex.height*(row==0?.46875f:.53125f)),new Vector2(.5f,row==0?.033f:.08f),100,0,SpriteMeshType.FullRect);
                var rect=people[row*3+col].rect;portraits[row*3+col]=Sprite.Create(tex,new Rect(rect.x+rect.width*.18f,rect.y+rect.height*.4f,rect.width*.64f,rect.height*.6f),new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);
            }
            backdrop=Actor("PortBackdrop",harborArt,0);foreground=Actor("Foreground",harborArt,8);
            mentor=Actor("XuLan",people[4],3);guard=Actor("DutyGuard",people[5],3);hero=Actor("LinJian",people[0],5);
            // A separate geometric foreground vehicle supports the arrival and departure staging.
            shuttle=new GameObject("港区接驳车").transform;shuttle.SetParent(transform,false);
            BusPart("车身",new Vector3(0,.57f,0),new Vector2(2.8f,1.05f),new Color(.15f,.27f,.30f),2);
            BusPart("窗",new Vector3(.05f,.87f,0),new Vector2(2.24f,.38f),new Color(.68f,.77f,.75f),3);
            for(int i=0;i<3;i++)BusPart("窗框"+i,new Vector3(-.65f+i*.65f,.87f,-.01f),new Vector2(.06f,.40f),new Color(.12f,.22f,.26f),4);
            BusPart("车门",new Vector3(.82f,.55f,-.01f),new Vector2(.08f,.78f),new Color(.08f,.17f,.20f),4);
            BusPart("车灯",new Vector3(1.3f,.38f,-.01f),new Vector2(.13f,.13f),gold,4);
            BusPart("底盘",new Vector3(0,.14f,0),new Vector2(2.9f,.22f),new Color(.07f,.12f,.15f),3);
            BusPart("轮一",new Vector3(-.9f,.12f,0),new Vector2(.36f,.36f),new Color(.035f,.05f,.06f),4);
            BusPart("轮二",new Vector3(.9f,.12f,0),new Vector2(.36f,.36f),new Color(.035f,.05f,.06f),4);
            ApplyRoom(true);
        }
        void BusPart(string name,Vector3 pos,Vector2 size,Color color,int order)
        {
            var t=new Texture2D(1,1);t.SetPixel(0,0,Color.white);t.Apply();var s=Sprite.Create(t,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
            var sr=Actor(name,s,order);sr.transform.SetParent(shuttle,false);sr.transform.localPosition=pos;sr.transform.localScale=new Vector3(size.x,size.y,1);sr.color=color;
        }
        void ApplyRoom(bool snap=false)
        {
            var outdoor=Story.State.room=="harbor";backdrop.sprite=outdoor?harborArt:officeArt;
            backdrop.transform.position=new Vector3(12,0,1);backdrop.transform.localScale=new Vector3(24/backdrop.sprite.bounds.size.x,8/backdrop.sprite.bounds.size.y,1);
            mentor.transform.position=new Vector3(Story.MentorX,Floor,0);guard.transform.position=new Vector3(7.5f,Floor,0);guard.gameObject.SetActive(outdoor);
            foreach(var a in new[]{hero,mentor,guard})a.transform.localScale=Vector3.one*(2.35f/a.sprite.bounds.size.y);
            shuttle.gameObject.SetActive(outdoor);shuttle.position=new Vector3(2.4f,Floor,0);
            var texture=backdrop.sprite.texture;float strip=outdoor?.25f:.12f;
            if(foreground.sprite!=null&&foreground.sprite!=harborArt)Destroy(foreground.sprite);
            foreground.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,Mathf.Floor(texture.height*strip)),new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);
            foreground.transform.position=new Vector3(12,-4,-.2f);foreground.transform.localScale=backdrop.transform.localScale;
            hero.transform.position=new Vector3(Story.X,Floor,-.1f);hero.flipX=Story.State.facingLeft;
            targetX=Story.X;pendingTarget="";
            if(snap)sceneCamera.transform.position=new Vector3(CameraX(),0,-10);
            placeUntil=Time.unscaledTime+5;
            if(placeLabel!=null)placeLabel.text=Story.Book.rooms.First(r=>r.id==Story.State.room).title;
        }
        float CameraX()
        {
            float half=sceneCamera.orthographicSize*sceneCamera.aspect;
            if(half>=12)return 12;
            return Mathf.Clamp(Story.X,half,24-half);
        }
        void BuildUI()
        {
            var c=new GameObject("StoryCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            c.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvasRoot=(RectTransform)c.transform;
            var scaler=c.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=Portrait?new Vector2(900,1600):new Vector2(1600,900);scaler.matchWidthOrHeight=Portrait?0:1;
            if(EventSystem.current==null){var es=new GameObject("StoryEventSystem",typeof(EventSystem));es.AddComponent<InputSystemUIInputModule>();}
            var place=Panel("Location",canvasRoot,new Vector2(.035f,.84f),new Vector2(.52f,.96f),Color.clear);
            placeLabel=Text(place,"雾港 · 查验区",32,gold);
            var menu=Panel("Tools",canvasRoot,new Vector2(Portrait?.52f:.76f,.90f),new Vector2(.97f,.97f),Color.clear);Horizontal(menu);
            Button(menu,"调查手册",ShowNotebook,"NotebookButton");Button(menu,"?",ShowHelp,"HelpButton");
            var n=Panel("Nearby",canvasRoot,new Vector2(Portrait?.12f:.34f,.08f),new Vector2(Portrait?.88f:.66f,.16f),Color.clear);
            nearButton=Button(n,"交谈 / 查看",()=>Interact(Nearest().id),"NearbyButton");Stretch((RectTransform)nearButton.transform);
            nearButton.gameObject.SetActive(false);
            marker=Panel("LocalMarker",canvasRoot,Vector2.zero,Vector2.zero,Color.clear);marker.sizeDelta=new Vector2(160,70);Text(marker,"◇",30,gold).alignment=TextAlignmentOptions.Center;
            marker.gameObject.SetActive(false);
        }
        RectTransform Panel(string name,Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;
            if(color.a>0){var image=go.AddComponent<UnityEngine.UI.Image>();image.color=color;}
            return rt;
        }
        void Stretch(RectTransform rt){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}
        TMP_Text Text(Transform parent,string value,float size,Color? color=null)
        {
            var go=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);Stretch(rt);
            var t=go.GetComponent<TextMeshProUGUI>();t.font=chineseFont;t.text=value;t.fontSize=Portrait?size*1.45f:size;t.color=color??paper;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;
            t.margin=new Vector4(16,10,16,10);t.overflowMode=TextOverflowModes.Overflow;return t;
        }
        void Vertical(RectTransform rt,int padding=14)
        {
            var g=rt.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();g.spacing=12;g.padding=new RectOffset(padding,padding,padding,padding);g.childControlHeight=true;g.childControlWidth=true;g.childForceExpandHeight=false;g.childForceExpandWidth=true;
        }
        void Horizontal(RectTransform rt)
        {
            var g=rt.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();g.spacing=10;g.childControlWidth=true;g.childControlHeight=true;g.childForceExpandWidth=true;g.childForceExpandHeight=true;
        }
        void FixedHeight(GameObject go,float height)
        {var l=go.AddComponent<UnityEngine.UI.LayoutElement>();l.minHeight=height;l.preferredHeight=height;}
        TMP_Text Paragraph(Transform parent,string text,float size=27,Color?color=null)
        {
            var t=Text(parent,text,size,color);var l=t.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();l.minHeight=size+25;
            // TMP supplies its preferred height to the layout group as the available width changes.
            return t;
        }
        UnityEngine.UI.Button Button(Transform parent,string label,Action click,string name="Choice")
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(parent,false);
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.15f,.23f,.27f,.98f);
            var b=go.GetComponent<UnityEngine.UI.Button>();var cols=b.colors;cols.highlightedColor=new Color(1,.88f,.64f);cols.pressedColor=new Color(.8f,.68f,.43f);b.colors=cols;
            var t=Text(go.transform,label,26);t.alignment=TextAlignmentOptions.MidlineLeft;t.margin=new Vector4(20,8,20,8);
            var l=go.AddComponent<UnityEngine.UI.LayoutElement>();l.minHeight=Portrait?104:62;l.preferredHeight=Portrait?112:70;
            b.onClick.AddListener(()=>click());return b;
        }
        void ClearModal()
        {
            if(modal!=null){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}modal=null;body=null;CurrentView="";
            if(canvasRoot!=null&&canvasRoot.Find("Tools")!=null)canvasRoot.Find("Tools").gameObject.SetActive(true);
        }
        void Open(string view,Color? color=null)
        {
            ClearModal();targetX=Story.X;pendingTarget="";CurrentView=view;
            modal=Panel(view,canvasRoot,Vector2.zero,Vector2.one,color??new Color(0,0,0,.13f));
            canvasRoot.Find("Tools").gameObject.SetActive(false);
            // Full-screen input shield freezes walking while retaining the scene behind dialogue.
            if(modal.GetComponent<UnityEngine.UI.Image>()==null){var im=modal.gameObject.AddComponent<UnityEngine.UI.Image>();im.color=Color.clear;}
        }
        RectTransform Scroll(RectTransform parent,Vector2 min,Vector2 max)
        {
            var root=Panel("ScrollView",parent,min,max,ink);var sr=root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            var vp=Panel("Viewport",root,Vector2.zero,Vector2.one,Color.white);var mask=vp.gameObject.AddComponent<UnityEngine.UI.Mask>();mask.showMaskGraphic=false;
            var content=Panel("Content",vp,new Vector2(0,1),Vector2.one,Color.clear);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,1);Vertical(content,24);
            var fitter=content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();fitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport=vp;sr.content=content;sr.horizontal=false;sr.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;sr.scrollSensitivity=35;
            return content;
        }
        void CloseButton(Action then=null)
        {
            var p=Panel("Close",modal,new Vector2(.79f,.91f),new Vector2(.96f,.98f),Color.clear);
            var b=Button(p,"收起",()=>{ClearModal();then?.Invoke();},"CloseButton");Stretch((RectTransform)b.transform);
        }
        public void ShowHome()
        {
            home=true;Open("Home",new Color(.025f,.055f,.075f,.80f));
            body=Scroll(modal,new Vector2(.11f,.13f),new Vector2(.73f,.82f));
            Paragraph(body,"关山有眼",57,gold);Paragraph(body,"雾 港 来 信",40);Paragraph(body,"第一幕 · 多出的460公斤",29);
            Paragraph(body,"一封没有署名的信。\n一票尚未离港的货。\n先把眼前的记录核对清楚。",28);
            Button(body,"开始新版故事",()=>{Story.NewGame();home=false;ApplyRoom(true);ShowDialogue();},"NewGameButton");
            if(File.Exists(Story.SavePath)||File.Exists(Story.SavePath+".bak"))Button(body,"继续调查",ContinueGame,"ContinueButton");
            Paragraph(body,"点击地面行走 · 靠近后交谈或查看\n键盘 A / D 或方向键移动，E 调查；手机可全程点按。",23);
        }
        public void ContinueGame()
        {
            if(!Story.Load()){Message("继续调查",Story.LoadNotice,ShowHome);return;}
            home=false;ApplyRoom(true);ApplyMusic();ClearModal();
            if(!string.IsNullOrEmpty(Story.State.pending))ShowDialogue();
            else if(Story.Has("act1Complete"))ShowCompletion();
        }
        public void ShowDialogue()
        {
            var line=Story.Current;if(line==null){ClearModal();return;}
            bool prologue=Story.State.pending=="prologue";
            Open(prologue?"Prologue":"Dialogue",prologue?new Color(.02f,.035f,.05f,1):new Color(0,0,0,.10f));
            if(!prologue && (line.speaker=="许岚"||line.speaker=="林见"||line.speaker=="值班员"))
            {
                var frame=Panel("SpeakerPortrait",modal,new Vector2(.025f,Portrait?.39f:.04f),new Vector2(Portrait?.41f:.255f,Portrait?.80f:.80f),Color.clear);
                var image=frame.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=portraits[line.speaker=="许岚"?4:line.speaker=="林见"?3:5];image.preserveAspect=true;image.raycastTarget=false;
            }
            var box=Panel("DialogueBox",modal,new Vector2(prologue?.16f:Portrait?.04f:.26f,.055f),new Vector2(.96f,prologue?.64f:Portrait?.43f:.34f),ink);
            var speaker=Panel("Speaker",box,new Vector2(.02f,.76f),new Vector2(.96f,.99f),Color.clear);Text(speaker,line.speaker,29,gold);
            var words=Panel("Words",box,new Vector2(.02f,.25f),new Vector2(.98f,.77f),Color.clear);var t=Text(words,Emphasis(line.text),28);t.fontSizeMax=Portrait?40:28;t.fontSizeMin=Portrait?34:23;t.enableAutoSizing=true;
            var next=Panel("Next",box,new Vector2(.65f,.02f),new Vector2(.98f,.23f),Color.clear);
            var nextB=Button(next,"下一句  →",AdvanceDialogue,"NextButton");Stretch((RectTransform)nextB.transform);
            var close=Panel("SaveClose",modal,new Vector2(Portrait?.50f:.77f,.91f),new Vector2(.96f,.98f),Color.clear);
            var closeB=Button(close,prologue?"跳过序幕":"保存并收起",()=>{if(prologue){while(Story.Current!=null)Story.Next();StartArrival();}else {Story.Save();ClearModal();}},"PauseDialogueButton");Stretch((RectTransform)closeB.transform);
        }
        string Emphasis(string text)
        {foreach(var word in new[]{"QJ-017","SD-0615","460公斤","K17"})text=text.Replace(word,"<color=#E8B669>"+word+"</color>");return text;}
        public void AdvanceDialogue()
        {
            var was=Story.State.pending;Story.Next();
            if(Story.Current!=null){ShowDialogue();return;}
            if(was=="prologue"){StartArrival();return;}
            if(was=="departure"){ShowCompletion();return;}
            ClearModal();
        }
        void StartArrival()
        {ClearModal();arrival=0;shuttle.position=new Vector3(-1.5f,Floor,0);hero.gameObject.SetActive(false);placeUntil=Time.unscaledTime+8;}
        public void RequestAction(string id)
        {
            if(!string.IsNullOrEmpty(Story.State.pending)){ShowDialogue();return;}
            var a=Story.Action(id);var missing=Story.Missing(a.required);
            if(missing!=null){Message(a.title,missing+"\n\n"+Story.Hint(2));return;}
            if(Story.State.completed.Contains(id)){ReviewAction(id);return;}
            if(id=="weight") { ShowWeight();return; }
            if(id=="cargo") { ShowCargo();return; }
            Begin(id);
        }
        void Begin(string id,int choice=-1)
        {var error=Story.Begin(id,choice);if(error!=null){Message("现场核对",error);return;}ShowDialogue();}
        void ShowWeight()
        {
            Open("WeightInspection");body=Scroll(modal,new Vector2(.13f,.14f),new Vector2(.87f,.85f));
            Paragraph(body,"台面上的两份记录",35,gold);Paragraph(body,"第一日 16:40 · 两份记录票号均为 QJ-017",27);
            Paragraph(body,"申报记录  ｜  品名：收纳盒  ｜  净重：800 kg\n复核记录  ｜  同一净重口径  ｜  净重：1260 kg",32);
            Paragraph(body,"票号与口径一致。哪一个判断成立？",27);
            var a=Story.Action("weight");for(int i=0;i<a.choices.Length;i++){int choice=i;Button(body,a.choices[i],()=>{
              var error=Story.Begin("weight",choice);if(error!=null)Message("再核对一下",error,ShowWeight);else ShowDialogue();},"WeightChoice"+i);}
            CloseButton();
        }
        void ShowCargo()
        {
            Open("CargoInspection");body=Scroll(modal,new Vector2(.18f,.15f),new Vector2(.82f,.84f));
            Paragraph(body,"查验点 · 当前票 QJ-017",35,gold);Paragraph(body,"外箱记录：收纳盒\n实物记录：乳霜等化妆品\n生产批次：K17",36);
            Paragraph(body,"把实物与申报分开记录。生产批次不能替代货物身份。",26);
            Button(body,"记录查验结果",()=>Begin("cargo"),"RecordCargoButton");CloseButton();
        }
        void ReviewAction(string id)
        {
            var a=Story.Action(id);Open("Review");body=Scroll(modal,new Vector2(.16f,.12f),new Vector2(.84f,.86f));
            Paragraph(body,a.title+" · 已记录",34,gold);foreach(var l in a.lines)Paragraph(body,l.speaker+"："+l.text);CloseButton();
        }
        public void Interact(string id)
        {
            if(home||arrival>=0)return;
            if(!string.IsNullOrEmpty(Story.State.pending)){ShowDialogue();return;}
            var spot=Spots.FirstOrDefault(s=>s.id==id);if(string.IsNullOrEmpty(spot.id))return;
            var reach=Story.At(Story.State.room,spot.x);if(reach!=null){WalkTo(id);return;}
            if(id=="door")
            {
                var error=Story.Enter(Story.State.room=="harbor"?"inspection":"harbor");
                if(error!=null)Message("通道",error);else {ApplyRoom(true);Story.Save();}return;
            }
            if(id=="mentor"){ShowMentor();return;}
            if(id=="departure"&&Story.Has("act1Complete")){ShowCompletion();return;}
            RequestAction(id);
        }
        public void WalkTo(string id)
        {
            if(IsModal||arrival>=0||home)return;
            var spot=Spots.First(s=>s.id==id);SetWalkTarget(spot);
        }
        void SetWalkTarget(Hotspot spot)
        {targetX=spot.x+(Story.X<spot.x?-.9f:.9f);pendingTarget=spot.id;}
        Hotspot Nearest()=>Spots.OrderBy(s=>Mathf.Abs(s.x-Story.X)).First();
        void ShowMentor()
        {
            Open("MentorTopics");body=Scroll(modal,new Vector2(.49f,.19f),new Vector2(.95f,.80f));
            var portrait=Panel("XuLanPortrait",modal,new Vector2(.11f,.04f),new Vector2(.39f,.83f),Color.clear);var im=portrait.gameObject.AddComponent<UnityEngine.UI.Image>();im.sprite=people[4];im.preserveAspect=true;im.raycastTarget=false;
            Paragraph(body,"许岚",35,gold);Paragraph(body,"“"+Story.Hint(0)+"”");
            if(!Story.Has("introDone")&&Story.State.room=="harbor")Button(body,"先从哪里查起？",()=>Begin("intro"),"IntroTopic");
            if(Story.Has("weight")&&!Story.Has("inspectionReady"))Button(body,"报告重量差异",()=>Begin("reportWeight"),"ReportWeightTopic");
            if(Story.Has("cargo"))Button(body,"核对当前异常",()=>ShowRelation("portMismatch"),"MismatchTopic");
            if(Story.Has("portMismatch")&&Story.Has("handover"))Button(body,"确认下一站",()=>ShowRelation("portDestination"),"DestinationTopic");
            Button(body,"我还想核对一下",()=>{ClearModal();},"KeepInvestigatingButton");
            Button(body,"问许岚：如何区分线索与结论？",()=>Message("许岚 · 核查方法","信件和观察给出方向。记录能支持的部分写清楚；原因、知情与责任，要留下待核事项。\n\n"+Story.Hint(1),ShowMentor),"FixedQuestionTopic");
            CloseButton();
        }
        void ShowRelation(string id)
        {
            var r=Story.Book.relations.First(x=>x.id==id);var missing=Story.Missing(r.required);if(missing!=null){Message("许岚",missing,ShowMentor);return;}
            Open("RelationClaims");body=Scroll(modal,new Vector2(.36f,.22f),new Vector2(.94f,.82f));
            Paragraph(body,"许岚：“"+r.title+"”",32,gold);
            for(int i=0;i<r.claims.Length;i++){int claim=i;Button(body,r.claims[i],()=>ChooseEvidence(id,claim),"Claim"+i);}
            CloseButton();
        }
        void ChooseEvidence(string id,int claim)
        {
            var r=Story.Book.relations.First(x=>x.id==id);Open("EvidencePresentation");body=Scroll(modal,new Vector2(.27f,.12f),new Vector2(.94f,.85f));
            Paragraph(body,"出示依据 · "+r.claims[claim],31,gold);Paragraph(body,"点选一至两份已取得的材料。",25);
            var selected=new HashSet<string>();
            foreach(var card in Story.Book.materials.Where(m=>Story.Has(m.id)))
            {
                var m=card;UnityEngine.UI.Button b=null;b=Button(body,"□  "+m.title,()=>{
                    if(selected.Contains(m.id))selected.Remove(m.id);else if(selected.Count<2)selected.Add(m.id);
                    b.GetComponentInChildren<TMP_Text>().text=(selected.Contains(m.id)?"■  ":"□  ")+m.title;
                },"Evidence_"+m.id);
            }
            Button(body,"向许岚说明",()=>{var error=Story.Relate(id,claim,selected);
                if(error!=null)Message("许岚 · 再核对一下",error,()=>ShowRelation(id));
                else Message("许岚",r.success,ShowMentor);},"PresentButton");CloseButton();
        }
        public void Message(string title,string message,Action then=null)
        {
            Open("Message");body=Scroll(modal,new Vector2(.28f,.20f),new Vector2(.92f,.78f));Paragraph(body,title,34,gold);Paragraph(body,message,29);
            Button(body,"知道了",()=>{ClearModal();then?.Invoke();},"AcknowledgeButton");
        }
        public void ShowNotebook()
        {
            if(home)return;
            Open("Notebook");body=Scroll(modal,new Vector2(.13f,.10f),new Vector2(.87f,.87f));Paragraph(body,"调查手册",36,gold);
            if(!string.IsNullOrEmpty(Story.State.pending))Button(body,"回到尚未结束的现场对话",ShowDialogue,"ResumeDialogueButton");
            if(Story.Has("letterReceived")){Paragraph(body,"待核线索 · 来信摘要",29,gold);Paragraph(body,Story.Action("prologue").lines[1].text);}
            foreach(var m in Story.Book.materials.Where(m=>Story.Has(m.id))){Paragraph(body,m.kind+" · "+m.title,29,gold);Paragraph(body,m.text);}
            if(Story.Has("inspectionReady"))Paragraph(body,"核验说明：交接标签覆盖旧胶痕。收到说明不等于亲自观察，痕迹不能证明擅自启封。",26);
            Paragraph(body,"已支持的关系",29,gold);foreach(var r in Story.Book.relations.Where(r=>Story.Has(r.id)))Paragraph(body,r.success);
            Paragraph(body,"当前待核",29,gold);Paragraph(body,Story.Hint(1));
            Paragraph(body,"人物 · 仅复看已知信息",29,gold);
            Paragraph(body,"林见：第一次参与现场核查的实习调查员。\n许岚：组织核查，带教林见。"+(Story.Has("handover")?"\n值班员：提供计划交接与历史往来附件。\n陈渡：交接资料所列联系人；具体参与情况待核。":""));
            foreach(var a in Story.Book.actions.Where(a=>Story.State.completed.Contains(a.id)))Button(body,"复看 · "+a.title,()=>ReviewAction(a.id),"Review_"+a.id);
            Button(body,"音乐："+(Story.State.musicGain>0?"开（点按关闭）":"关（点按开启）"),()=>{Story.State.musicGain=Story.State.musicGain>0?0:1;ApplyMusic();Story.Save();ShowNotebook();},"MusicToggle");
            Button(body,"保存并返回标题",()=>{Story.Save();ShowHome();},"SaveHomeButton");CloseButton();
        }
        void ApplyMusic()
        {var audio=FindObjectsByType<AudioSource>(FindObjectsSortMode.None);foreach(var a in audio)a.volume=(a.clip!=null&&a.clip.name.Contains("underwater")?.65f:.55f)*Story.State.musicGain;}
        void ShowHelp()
        {if(home)return;Message("现场提示",Story.Hint(Mathf.Min(hintLevel++,2))+"\n\n点击地面行走；点远处的人或物会先走近。靠近后点“交谈 / 查看”，或按 E。手册可随时复看已记录内容。\n\nTab：手册　Esc：收起；对话可保存后继续。");}
        void ShowCompletion()
        {
            Open("Act1Complete",new Color(.03f,.055f,.07f,.75f));body=Scroll(modal,new Vector2(.17f,.12f),new Vector2(.83f,.87f));
            Paragraph(body,"多出的460公斤",44,gold);Paragraph(body,"第一幕完成",29);
            Paragraph(body,"QJ-017 的申报与实物不一致，仍留在港区。\n\n你们带着计划交接及历史往来附件，准备核对两天前的 SD-0615 旧出货记录。",30);
            Paragraph(body,"接下来还要查：历史作业的记录、实际流向，以及相关人员各自经手的环节。当前材料不能替代后续核查。",27);
            Button(body,"返回港区，再核对一下",ClearModal,"ReturnToPortButton");Button(body,"保存并返回标题",()=>{Story.Save();ShowHome();},"CompleteSaveButton");
        }
        void Update()
        {
            TickCount++;
            if(Story==null||hero==null)return;
            if(arrival>=0)
            {
                arrival+=Time.deltaTime;shuttle.position=Vector3.Lerp(new Vector3(-1.5f,Floor,0),new Vector3(2.4f,Floor,0),Mathf.SmoothStep(0,1,arrival/2.5f));
                if(arrival>2.7f){arrival=-1;hero.gameObject.SetActive(true);Story.Position(4.1f,false);ApplyRoom(true);Message("雾港 · 查验区","许岚在接驳车右边等你。先过去问问，从哪里查起。\n\n点击地面行走，靠近人物或物件后交谈、查看。");}return;
            }
            var keyboard=Keyboard.current;
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame){if(IsModal&&!home){Story.Save();ClearModal();}else if(!home)ShowNotebook();}
            if(keyboard!=null&&keyboard.tabKey.wasPressedThisFrame&&!home){if(IsModal)ClearModal();else ShowNotebook();}
            if(home)return;
            bool pausedDialogue=!string.IsNullOrEmpty(Story.State.pending);
            nearButton.gameObject.SetActive(!IsModal&&(pausedDialogue||Mathf.Abs(Nearest().x-Story.X)<1.25f));
            if(nearButton.gameObject.activeSelf)nearButton.GetComponentInChildren<TMP_Text>().text=pausedDialogue?"继续现场对话  ·  E / 点按":Nearest().title+"  ·  E / 点按";
            placeLabel.color=!IsModal&&Time.unscaledTime<placeUntil?gold:Color.clear;
            marker.gameObject.SetActive(nearButton.gameObject.activeSelf&&!pausedDialogue);
            if(marker.gameObject.activeSelf){var view=sceneCamera.WorldToViewportPoint(new Vector3(Nearest().x,Floor+2.7f,0));marker.anchoredPosition=new Vector2(view.x*canvasRoot.rect.width,view.y*canvasRoot.rect.height);}
            if(IsModal){hero.sprite=people[0];return;}
            if(pausedDialogue){if(keyboard!=null&&keyboard.eKey.wasPressedThisFrame)ShowDialogue();return;}
            if(keyboard!=null&&(keyboard.eKey.wasPressedThisFrame||keyboard.spaceKey.wasPressedThisFrame)&&Mathf.Abs(Nearest().x-Story.X)<1.25f){Interact(Nearest().id);return;}
            var pointer=Pointer.current;
            bool pressed=pointer!=null&&pointer.press.wasPressedThisFrame;
            if(pressed)
            {
                var pos=pointer.position.ReadValue();var data=new PointerEventData(EventSystem.current){position=pos};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
                if(hits.Count==0)
                {
                    var world=sceneCamera.ScreenToWorldPoint(new Vector3(pos.x,pos.y,10));targetX=Mathf.Clamp(world.x,.7f,23.3f);pendingTarget="";
                    var s=Spots.OrderBy(a=>Mathf.Abs(a.x-world.x)).First();
                    if(Mathf.Abs(s.x-world.x)<.8f&&world.y<1.3f)SetWalkTarget(s);
                }
            }
            float dir=0;if(keyboard!=null){if(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed)dir=-1;if(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed)dir=1;}
            if(dir!=0){targetX=Mathf.Clamp(Story.X+dir,.7f,23.3f);pendingTarget="";}
            float old=Story.X;float next=Mathf.MoveTowards(old,targetX,3.1f*Time.deltaTime);bool moving=Mathf.Abs(next-old)>.0001f;
            if(moving)
            {
                Story.Position(next,next<old);walkClock+=Time.deltaTime;WalkingFrames++;int pose=Mathf.FloorToInt(walkClock*7)%4;hero.sprite=people[pose==0?1:pose==2?2:0];
                hero.flipX=Story.State.facingLeft;hero.transform.position=new Vector3(next,Floor+Mathf.Sin(walkClock*10)*.018f,-.1f);
            }
            else
            {
                hero.sprite=people[0];hero.transform.position=new Vector3(Story.X,Floor,-.1f);
                if(!string.IsNullOrEmpty(pendingTarget)){var id=pendingTarget;pendingTarget="";Interact(id);}
            }
            var goal=new Vector3(CameraX(),0,-10);sceneCamera.transform.position=Vector3.Lerp(sceneCamera.transform.position,goal,1-Mathf.Exp(-5*Time.deltaTime));
            if(Time.unscaledTime-lastSaved>3){lastSaved=Time.unscaledTime;Story.Save();}
        }
        void OnApplicationPause(bool pause){if(pause&&!home)Story?.Save();}
        void OnApplicationQuit(){if(!home)Story?.Save();}
    }
}
