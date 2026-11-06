using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace GuanshanYouyan.Story
{
    public sealed partial class FogGame : MonoBehaviour
    {
        public TextAsset storyData; public TMP_FontAsset chineseFont;
        public Sprite harborArt, officeArt, castArt,otherCastArt,objectArt,shuttleArt,coverArt;public Sprite[] roomArts; public Material spriteMaterial;
        public Camera sceneCamera;
        public FogStory Story {get;private set;}
        public bool IsModal => modal != null;
        public string CurrentView {get;private set;} = "";
        public int WalkingFrames {get;private set;}
        public int TickCount {get;private set;}
        RectTransform canvasRoot,modal,body,marker,arrivalHint; TMP_Text placeLabel; UnityEngine.UI.Button nearButton;
        float arrivalHintUntil,coverRemaining;bool coverPresented=true;
        bool Portrait => (float)Screen.width/Mathf.Max(1,Screen.height)<.85f;
        SpriteRenderer backdrop,foreground,hero,mentor,guard; Transform shuttle;
        float Floor => Story.Room.floor;
        TMP_Text nearbyLabel;StorySpot nearbySpot;bool nearbyPaused,movementUnsaved;
        readonly List<RaycastResult> pointerHits=new List<RaycastResult>(8);
        PointerEventData pointerData;
        int ignoreScenePointerFrame=-1;
        Sprite[] foregroundStrips;
        Sprite[] people=new Sprite[10],portraits=new Sprite[10],objects=new Sprite[6];
        float targetX,walkClock,lastSaved,arrival=-1,placeUntil; string pendingTarget="";
        bool home=true,layoutPortrait; int hintLevel,lastNpcActor;string activeRelation;Action evidenceRedraw; readonly Color ink=new Color(.055f,.09f,.12f,.96f);
        MentorChatClient mentorClient=null;TMP_InputField chatInput;bool renderingChat;string chatDraft="",chatError="",lastQuestion="";
        readonly List<KeyValuePair<string,string>> chatHistory=new List<KeyValuePair<string,string>>();
        readonly Color paper=new Color(.91f,.89f,.80f,1),gold=new Color(.91f,.65f,.30f,1);
        StorySpot[] Spots => Story.Room.spots;
        const string MusicPreference="guanshan-mist-music-gain-v1";
        [Serializable] class MusicSaveHeader {public int version;public float musicGain=1;}
#if UNITY_WEBGL && !UNITY_EDITOR
        float nextMusicPoll;
        [DllImport("__Internal")] static extern float FogReadMusicPreference();
        [DllImport("__Internal")] static extern void FogWriteMusicPreference(float gain);
        [DllImport("__Internal")] static extern void FogInstallSaveLifecycle();
#endif
        void Start()
        {
            Application.targetFrameRate=Application.platform==RuntimePlatform.WebGLPlayer?-1:60;
            QualitySettings.vSyncCount=0;
            Story=new FogStory(JsonUtility.FromJson<PortBook>(storyData.text),Path.Combine(Application.persistentDataPath,"guanshan-mist-story-v2.json"));
            RestoreMusicPreference();
            BuildWorld();BuildUI();ShowHome();
#if UNITY_WEBGL && !UNITY_EDITOR
            FogInstallSaveLifecycle();
#endif
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
            bool verify=Application.absoluteURL.Contains("verifyStory=1")||System.Environment.GetCommandLineArgs().Contains("--verify-story");
            if(verify)gameObject.AddComponent<FogWalkthrough>().Begin(Application.absoluteURL.Contains("ecology=1")||System.Environment.GetCommandLineArgs().Contains("--verify-ecology"),false,"full-player-playtest.json");
#endif
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
                int index=row*3+col;if(index>=3)portraits[index]=FogPortraitLayout.Create(tex,index);
            }
            var extra=otherCastArt.texture;
            for(int row=0;row<2;row++)for(int col=0;col<2;col++)
            {
                int index=6+row*2+col;var rect=new Rect(col*extra.width/2f,(1-row)*extra.height/2f,extra.width/2f,extra.height/2f);
                people[index]=Sprite.Create(extra,rect,new Vector2(.5f,row==0?.045f:.065f),100,0,SpriteMeshType.FullRect);
                portraits[index]=FogPortraitLayout.Create(extra,index);
            }
            var props=objectArt.texture;
            for(int row=0;row<2;row++)for(int col=0;col<3;col++)objects[row*3+col]=Sprite.Create(props,new Rect(col*props.width/3f,(1-row)*props.height/2f,props.width/3f,props.height/2f),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            backdrop=Actor("PortBackdrop",harborArt,0);foreground=Actor("Foreground",harborArt,8);
            mentor=Actor("XuLan",people[4],3);guard=Actor("DutyGuard",people[5],3);hero=Actor("LinJian",people[0],5);
            // Use the illustrated vehicle as one uniformly scaled sprite, keeping circular tires round.
            shuttle=new GameObject("港区接驳车").transform;shuttle.SetParent(transform,false);
            var vehicle=Actor("接驳车插画",shuttleArt,2);vehicle.transform.SetParent(shuttle,false);
            vehicle.transform.localPosition=new Vector3(.25f,0,0);
            vehicle.transform.localScale=Vector3.one*(5.2f/shuttleArt.bounds.size.x);
            ApplyRoom(true);
        }
        void ApplyRoom(bool snap=false)
        {
            var outdoor=Story.State.room=="harbor";int roomIndex=Array.FindIndex(Story.Book.rooms,r=>r.id==Story.State.room);backdrop.sprite=roomArts[roomIndex];
            backdrop.transform.position=new Vector3(12,0,1);backdrop.transform.localScale=new Vector3(24/backdrop.sprite.bounds.size.x,8/backdrop.sprite.bounds.size.y,1);
            var actors=Story.Room.actors;var renderers=new[]{mentor,guard};
            for(int i=0;i<renderers.Length;i++){renderers[i].gameObject.SetActive(i<actors.Length);if(i<actors.Length){renderers[i].sprite=people[actors[i].actor];renderers[i].transform.position=new Vector3(actors[i].x,Floor,0);}}
            foreach(var a in new[]{hero,mentor,guard})a.transform.localScale=Vector3.one*(2.7f/a.sprite.bounds.size.y);
            shuttle.gameObject.SetActive(outdoor);shuttle.position=new Vector3(2.4f,Floor,0);
            var texture=backdrop.sprite.texture;float strip=Story.Room.strip;
            if(foregroundStrips==null)foregroundStrips=new Sprite[roomArts.Length];
            if(foregroundStrips[roomIndex]==null)foregroundStrips[roomIndex]=Sprite.Create(texture,new Rect(0,0,texture.width,Mathf.Floor(texture.height*strip)),new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);
            foreground.sprite=foregroundStrips[roomIndex];
            foreground.transform.position=new Vector3(12,-4,-.2f);foreground.transform.localScale=backdrop.transform.localScale;
            hero.transform.position=new Vector3(Story.X,Floor,-.1f);hero.flipX=Story.State.facingLeft;
            targetX=Story.X;pendingTarget="";
            if(snap)sceneCamera.transform.position=new Vector3(CameraX(),0,-10);
            placeUntil=Time.unscaledTime+5;
            if(placeLabel!=null)placeLabel.text=Story.Room.title+"\n"+Story.Book.chapters[Story.Act].time;
        }
        float CameraX()
        {
            float half=sceneCamera.orthographicSize*sceneCamera.aspect;
            if(half>=12)return 12;
            return Mathf.Clamp(Story.X,half,24-half);
        }
        void BuildUI()
        {
            layoutPortrait=Portrait;
            var c=new GameObject("StoryCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            c.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvasRoot=(RectTransform)c.transform;
            var scaler=c.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=Portrait?new Vector2(900,1600):new Vector2(1600,900);scaler.matchWidthOrHeight=Portrait?0:1;
            if(EventSystem.current==null){var es=new GameObject("StoryEventSystem",typeof(EventSystem));es.AddComponent<InputSystemUIInputModule>();}
            var place=Panel("Location",canvasRoot,new Vector2(.035f,.84f),new Vector2(.52f,.96f),Color.clear);
            placeLabel=Text(place,"雾港 · 查验区",32,gold);
            var menu=Panel("Tools",canvasRoot,Vector2.one,Vector2.one,Color.clear);
            LayoutTools(menu);
            NotebookToolButton(menu);
            var n=Panel("Nearby",canvasRoot,new Vector2(Portrait?.12f:.34f,.08f),new Vector2(Portrait?.88f:.66f,.16f),Color.clear);
            n.gameObject.AddComponent<Canvas>();n.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            nearButton=Button(n,"交谈 / 查看",()=>Interact(Nearest().id),"NearbyButton");Stretch((RectTransform)nearButton.transform);
            nearbyLabel=nearButton.GetComponentInChildren<TMP_Text>();
            nearButton.gameObject.SetActive(false);
            marker=Panel("LocalMarker",canvasRoot,Vector2.zero,Vector2.zero,Color.clear);marker.sizeDelta=new Vector2(160,70);Text(marker,"◇",30,gold).alignment=TextAlignmentOptions.Center;
            marker.gameObject.AddComponent<Canvas>();
            marker.gameObject.SetActive(false);
            BuildMusicButton();
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
            if(CharacterContent(parent))size=size>=31?35:30;
            var t=Text(parent,text,size,color);var l=t.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();l.minHeight=size+25;
            // TMP supplies its preferred height to the layout group as the available width changes.
            return t;
        }
        UnityEngine.UI.Button Button(Transform parent,string label,Action click,string name="Choice")
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(parent,false);
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.15f,.23f,.27f,.98f);
            var b=go.GetComponent<UnityEngine.UI.Button>();var cols=b.colors;cols.highlightedColor=new Color(1,.88f,.64f);cols.pressedColor=new Color(.8f,.68f,.43f);b.colors=cols;
            bool character=CharacterContent(parent)||parent.name=="Next";
            var t=Text(go.transform,label,character?32:26);t.alignment=TextAlignmentOptions.MidlineLeft;t.margin=new Vector4(20,8,20,8);
            var l=go.AddComponent<UnityEngine.UI.LayoutElement>();l.minHeight=Portrait?(character?120:104):(character?82:62);l.preferredHeight=Portrait?(character?128:112):(character?90:70);
            b.onClick.AddListener(()=>click());return b;
        }
        Action dismissModal;
        void ClearModal()
        {
            dismissModal=null;
            SetProloguePage(false);
            if(CurrentView=="MentorChat"&&!renderingChat&&mentorClient!=null)mentorClient.Cancel();
            if(modal!=null){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}modal=null;body=null;CurrentView="";
            if(canvasRoot!=null&&canvasRoot.Find("Tools")!=null)canvasRoot.Find("Tools").gameObject.SetActive(true);
            RefreshMusicButton();
        }
        void Open(string view,Color? color=null)
        {
            ClearModal();targetX=Story.X;pendingTarget="";CurrentView=view;
            modal=Panel(view,canvasRoot,Vector2.zero,Vector2.one,color??new Color(0,0,0,.13f));
            modal.gameObject.AddComponent<Canvas>();modal.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasRoot.Find("Tools").gameObject.SetActive(false);
            // Full-screen input shield freezes walking while retaining the scene behind dialogue.
            if(modal.GetComponent<UnityEngine.UI.Image>()==null){var im=modal.gameObject.AddComponent<UnityEngine.UI.Image>();im.color=Color.clear;}
            RefreshMusicButton();
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
        void AllowBlankDismiss(Action then=null)
        {
            EnableBlankDismiss(()=>{ClearModal();then?.Invoke();});
        }
        void EnableBlankDismiss(Action dismiss)
        {
            dismissModal=dismiss;
            var backdrop=modal.GetComponent<UnityEngine.UI.Button>()??modal.gameObject.AddComponent<UnityEngine.UI.Button>();
            backdrop.transition=UnityEngine.UI.Selectable.Transition.None;
            backdrop.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            backdrop.onClick.RemoveAllListeners();backdrop.onClick.AddListener(DismissModal);
            // Content consumes blank clicks; nested controls and ScrollRects keep their own input.
            foreach(Transform panel in modal)
            {
                if(panel.GetComponent<UnityEngine.UI.Image>()==null||panel.GetComponent<UnityEngine.UI.Button>()!=null)continue;
                var shield=panel.gameObject.AddComponent<UnityEngine.UI.Button>();
                shield.transition=UnityEngine.UI.Selectable.Transition.None;
                shield.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            }
        }
        void DismissModal()
        {
            ignoreScenePointerFrame=Time.frameCount;
            var dismiss=dismissModal;if(dismiss!=null)dismiss();
        }
        public void ShowHome()
        {
            bool showingCover=CurrentView=="Cover";
            ApplyMusic();
            home=true;Open("Cover",new Color(.025f,.055f,.075f,1));
            SetProloguePage(true);
            if(!showingCover)coverRemaining=3f;
            var art=Panel("CoverArtwork",modal,Vector2.zero,Vector2.one,Color.white);
            art.GetComponent<UnityEngine.UI.Image>().sprite=coverArt;
            if(coverArt!=null)
            {
                var fit=art.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio=coverArt.rect.width/coverArt.rect.height;
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            coverPresented=false;StartCoroutine(WaitForPresentedCover(modal.gameObject));
#else
            coverPresented=true;
#endif
        }
        void EnterFromCover()
        {
            if(Story.ReadSlot(0).Valid){ContinueGame();return;}
            // Preserve damaged or manual saves for recovery rather than silently resetting them.
            if(new[]{-1,0,1,2,3}.Any(slot=>{var record=Story.ReadSlot(slot);return record.Valid||record.Error!="";}))
            {ShowSaves();return;}
            if(File.Exists(Path.Combine(Application.persistentDataPath,"guanshan-mist-story-v2-port.json"))){ImportPortProgress();return;}
            ConfirmNewGame();
        }
        public void ContinueGame()
        {
            ResetChat();
            if(!Story.Load()){Message("继续调查",Story.LoadNotice,ShowHome);return;}
            home=false;ApplyRoom(true);ApplyMusic();ClearModal();
            if(Story.State.openingDrivePending)ShowOpeningDrive();
            else if(!string.IsNullOrEmpty(Story.State.pending))ShowDialogue();
            else if(!string.IsNullOrEmpty(Story.State.ending))ShowEnding();
            if(!string.IsNullOrEmpty(Story.LoadNotice))Message("读取存档",Story.LoadNotice,ResumeSavedGame);
        }
        void ImportPortProgress()
        {
            ResetChat();
            var error=Story.ImportPort(File.ReadAllText(Path.Combine(Application.persistentDataPath,"guanshan-mist-story-v2-port.json")));
            if(error!=null){Message("接续进度",error,ShowHome);return;}home=false;ApplyRoom(true);ApplyMusic();ClearModal();if(Story.Current!=null)ShowDialogue();
        }
        public void ShowDialogue()
        {
            var line=Story.Current;if(line==null){ClearModal();return;}
            if(Story.State.pending=="prologue"){if(Story.State.line==0)ShowPrologue();else ShowLetter(FinishLetterAndArrive);return;}
            Open("Dialogue",new Color(0,0,0,.10f));
            int actor=ActorIndex(line.speaker);
            if(actor>=0)
            {
                BottomPortrait("SpeakerPortrait",actor,FogPortraitLayout.ColumnRight);
            }
            var box=Panel("DialogueBox",modal,FogPortraitLayout.PanelMin,FogPortraitLayout.PanelMax,ink);
            var speaker=Panel("Speaker",box,new Vector2(.02f,.84f),new Vector2(.96f,.99f),Color.clear);Text(speaker,line.speaker,35,gold);
            var words=Panel("Words",box,new Vector2(.02f,.23f),new Vector2(.98f,.83f),Color.clear);var t=Text(words,Emphasis(line.text),32);t.fontSizeMax=Portrait?44:32;t.fontSizeMin=Portrait?36:28;t.enableAutoSizing=true;
            var next=Panel("Next",box,new Vector2(.55f,.02f),new Vector2(.98f,.17f),Color.clear);
            var nextB=Button(next,"下一句  →",AdvanceDialogue,"NextButton");Stretch((RectTransform)nextB.transform);
            EnableBlankDismiss(()=>{if(Story.Save())ClearModal();else Message("保存失败",Story.SaveError,ShowDialogue);});
        }
        string Emphasis(string text)
        {foreach(var word in new[]{"QJ-017","SD-0615","460公斤","K17"})text=text.Replace(word,"<color=#E8B669>"+word+"</color>");return text;}
        int ActorIndex(string name)=>name=="林见"?3:name=="许岚"?4:name=="值班员"?5:name=="老赵"?6:name=="苏禾"?7:name=="周叔"?8:name=="陈渡"?9:-1;
        public void AdvanceDialogue()
        {
            if(Story.State.pending=="prologue"){CompletePrologue();return;}
            var was=Story.State.pending;Story.Next();
            if(Story.Current!=null){ShowDialogue();return;}
            if(was=="prologue"){StartArrival();return;}
            if(!string.IsNullOrEmpty(Story.Action(was).destination)){ApplyRoom(true);Message(Story.Room.title,Story.Book.chapters[Story.Act].time+"\n\n"+Story.Book.chapters[Story.Act].question);return;}
            ClearModal();
        }
        void StartArrival()
        {if(Story.State.openingDrivePending)ShowOpeningDrive();else BeginHarborArrival();}
        public void RequestAction(string id)
        {
            if(!string.IsNullOrEmpty(Story.State.pending)){ShowDialogue();return;}
            var a=Story.Action(id);var missing=Story.Missing(a.required);
            var location=a.room==""?null:a.room=="*"?Story.At(Story.State.room,Story.MentorX):Story.At(a.room,a.x);
            if(location!=null||a.act!=Story.Act){Message("现场调查",location??"请返回对应阶段的调查现场。");return;}
            if(missing!=null){Message(a.title,missing+"\n\n"+Story.Hint(2));return;}
            if(Story.State.completed.Contains(id)){ReviewAction(id);return;}
            if(id=="weight") { ShowWeight();return; }
            if(id=="cargo") { ShowCargo();return; }
            if(a.mode=="inquiry"){ChooseCards(a.title,a.evidence,3,cards=>{var e=Story.Begin(id,-1,cards);if(e!=null)Message("陈渡 · 核对材料",e,ShowInquiries);else ShowDialogue();});return;}
            if(!string.IsNullOrEmpty(a.inspect)||a.choices.Length>0){ShowInspection(id);return;}
            Begin(id);
        }
        void Begin(string id,int choice=-1)
        {var error=Story.Begin(id,choice);if(error!=null){Message("现场核对",error);return;}ShowDialogue();}
        void ShowWeight()
        {
            ShowWeightPage();
        }
        void ShowCargo()
        {
            ShowCargoPage();
        }
        void ReviewAction(string id)
        {
            ShowReviewPage(id);
        }
        public void Interact(string id)
        {
            if(home||arrival>=0)return;
            if(!string.IsNullOrEmpty(Story.State.pending)){ShowDialogue();return;}
            var spot=Spots.FirstOrDefault(s=>s.id==id);if(spot==null)return;
            var reach=Story.At(Story.State.room,spot.x);if(reach!=null){WalkTo(id);return;}
            if(spot.kind=="door"||spot.kind=="travel")
            {
                string travelAction=Story.Act==0&&spot.target=="warehouse"?"departure":Story.Act==1&&spot.target=="town"?"warehouseDeparture":Story.Act==2&&spot.target=="interview"?"townDeparture":null;
                if(travelAction!=null&&!Story.State.completed.Contains(travelAction)){RequestAction(travelAction);return;}
                var error=Story.Enter(spot.target);
                if(error!=null)Message("通道",error);else {ApplyRoom(true);Story.Save();}return;
            }
            if(spot.kind=="mentor"){ShowMentor();return;}
            if(spot.kind=="worker"){if(!Story.Has("workerMet"))RequestAction("workerIntro");else ShowNpc(6);return;}
            if(spot.kind=="shop"){if(!Story.Has("shopRecordsShared"))RequestAction("shopShare");else ShowNpc(7);return;}
            if(spot.kind=="inquiries"){ShowInquiries();return;}
            if(spot.kind=="ecology"&&Story.Has("observed")){Message("待核物件",Story.Book.materials.First(m=>m.id=="wildlife").text+"\n\n"+(Story.Has("referred")?"已另行提交，结果待核。":Story.Has("sourceAsked")?"可以向店内许岚提出另行核验。":"点击苏禾询问来源。"));return;}
            if(spot.kind=="board"||spot.kind=="notes"){ShowNotebook();return;}
            RequestAction(id);
        }
        public void WalkTo(string id)
        {
            if(IsModal||arrival>=0||home)return;
            var spot=Spots.First(s=>s.id==id);SetWalkTarget(spot);
        }
        void SetWalkTarget(StorySpot spot)
        {targetX=spot.x+(Story.X<spot.x?-.9f:.9f);pendingTarget=spot.id;}
        StorySpot Nearest()=>Nearest(Story.X);
        StorySpot Nearest(float x)
        {
            var spots=Spots;var nearest=spots[0];float distance=Mathf.Abs(nearest.x-x);
            for(int i=1;i<spots.Length;i++){float next=Mathf.Abs(spots[i].x-x);if(next<distance){distance=next;nearest=spots[i];}}
            return nearest;
        }
        void ShowMentor()
        {
            Open("MentorTopics");body=CharacterBody();
            SpeakerFrame(4);
            Paragraph(body,"许岚",35,gold);Paragraph(body,"“"+Story.Hint(0)+"”");
            if(Story.Act==0)
            {
                if(!Story.Has("introDone")&&Story.State.room=="harbor")Button(body,"先从哪里查起？",()=>Begin("intro"),"IntroTopic");
                if(Story.Has("weight")&&!Story.Has("inspectionReady"))Button(body,"报告重量差异",()=>Begin("reportWeight"),"ReportWeightTopic");
            }
            foreach(var relation in Story.Book.relations.Where(r=>r.act==Story.Act&&Story.Missing(r.required)==null))
            {var r=relation;Button(body,(Story.Has(r.id)?"复看 · ":"")+r.title,()=>{if(Story.Has(r.id))Message("许岚",r.success,ShowMentor);else ShowRelation(r.id);},"RelationTopic_"+r.id);}
            if(Story.Act==2&&Story.State.room=="records"&&Story.Has("sourceAsked")&&!Story.Has("referred"))Button(body,"生态观察另行提交核验",()=>RequestAction("wildlifeRefer"),"EcologyReferTopic");
            if(Story.Act==3)
            {
                if(Story.State.room=="interview"&&Story.Has("receiptQuestioned")&&Story.Has("roleQuestioned"))Button(body,Story.Has("scopeReviewed")?"复看责任边界":"复盘其他人员责任边界",()=>RequestAction("scopeReview"),"ScopeTopic");
                if(Story.State.room=="archive")Button(body,Story.State.ending=="accepted"?"回看调查建议":"开始 / 继续复盘",()=>{var e=Story.ReportGuard();if(e!=null)Message("许岚",e,ShowMentor);else if(Story.State.ending=="accepted")ShowEnding();else ShowFinalIntro();},"FinalReviewTopic");
            }
            Button(body,"我还想核对一下",()=>{ClearModal();},"KeepInvestigatingButton");
            Button(body,"问许岚：如何区分线索与结论？",()=>Message("许岚 · 核查方法","信件和观察给出方向。记录能支持的部分写清楚；原因、知情与责任，要留下待核事项。\n\n"+Story.Hint(1),ShowMentor),"FixedQuestionTopic");
#if UNITY_EDITOR
            if(mentorClient!=null&&mentorClient.Ready)Button(body,"向许岚自由提问",ShowMentorChat,"FreeQuestionTopic");
#endif
            AllowBlankDismiss();
        }
        RectTransform CharacterBody()
        {
            var content=Scroll(modal,FogPortraitLayout.PanelMin,FogPortraitLayout.PanelMax);
            content.GetComponentInParent<UnityEngine.UI.ScrollRect>().gameObject.name="CharacterPanel";return content;
        }
        bool CharacterContent(Transform parent)=>parent.GetComponentInParent<UnityEngine.UI.ScrollRect>()?.gameObject.name=="CharacterPanel";
        void SpeakerFrame(int actor)
        {
            BottomPortrait("TopicPortrait",actor,FogPortraitLayout.ColumnRight);
        }
        void BottomPortrait(string name,int actor,float right)
        {
            var p=Panel(name,modal,Vector2.zero,Vector2.zero,Color.clear);
            var image=p.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=portraits[actor];image.preserveAspect=true;image.raycastTarget=false;
            p.gameObject.AddComponent<FogPortraitView>().Configure(canvasRoot,right);
        }
        void Prop(Transform parent,int index)
        {
            var p=Panel("InspectedObject",parent,Vector2.zero,Vector2.one,Color.clear);FixedHeight(p.gameObject,Portrait?310:220);
            var image=p.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=objects[index];image.preserveAspect=true;image.raycastTarget=false;
        }
        void ShowInspection(string id)
        {
            ShowInspectionPage(id);
        }
        void ShowNpc(int actor)
        {
            lastNpcActor=actor;
            Open("NpcTopics");SpeakerFrame(actor);body=CharacterBody();
            Paragraph(body,actor==6?"老赵":"苏禾",34,gold);
            if(actor==6)
            {
                Paragraph(body,"“我能确认自己经手的收发。”");
                Button(body,"核对具体日期与经手记录",()=>RequestAction("worker"),"WorkerRecordTopic");
                Button(body,"复看初次交谈",()=>ReviewAction("workerIntro"),"WorkerIntroReview");
            }
            else
            {
                Paragraph(body,"“本次到货清单和账簿可以核对。”");
                Button(body,"复看资料提供范围",()=>ReviewAction("shopShare"),"ShopShareReview");
                if(Story.Has("observed"))Button(body,Story.Has("sourceAsked")?"复看物件来源回答":"询问待核物件来源",()=>RequestAction("wildlifeSource"),"EcologySourceTopic");
            }
            AllowBlankDismiss();
        }
        void ShowInquiries()
        {
            Open("InquiryTopics");SpeakerFrame(9);body=CharacterBody();
            Paragraph(body,"陈渡",34,gold);Paragraph(body,"“请把要核对的记录拿出来，我们逐项说。”");
            foreach(var id in new[]{"receiptInquiry","roleInquiry"}){var key=id;var a=Story.Action(key);Button(body,(Story.State.completed.Contains(key)?"复看回应 · ":"")+a.title,()=>RequestAction(key),"InquiryTopic_"+key);}
            AllowBlankDismiss();
        }
        void ChooseCards(string title,string[] needed,int max,Action<string[]> submit,IEnumerable<string> initial=null)
        {
            Open("EvidencePresentation");body=Scroll(modal,new Vector2(.23f,.10f),new Vector2(.94f,.87f));Paragraph(body,title,31,gold);
            Paragraph(body,"点选已取得的材料，最多"+max+"份；可在手册复看原文。",25);
            var selected=new HashSet<string>(initial??new string[0]);evidenceRedraw=()=>ChooseCards(title,needed,max,submit,selected.ToArray());
            foreach(var card in Story.Book.materials.Where(m=>Story.Has(m.id)))
            {
                var m=card;UnityEngine.UI.Button b=null;b=Button(body,(selected.Contains(m.id)?"■  ":"□  ")+m.title,()=>{if(selected.Contains(m.id))selected.Remove(m.id);else if(selected.Count<max)selected.Add(m.id);b.GetComponentInChildren<TMP_Text>().text=(selected.Contains(m.id)?"■  ":"□  ")+m.title;},"Evidence_"+m.id);
            }
            Button(body,"出示这些材料",()=>submit(selected.ToArray()),"PresentButton");AllowBlankDismiss();
        }
        void ShowRelation(string id)
        {
            var r=Story.Book.relations.First(x=>x.id==id);var missing=Story.Missing(r.required);if(missing!=null){Message("许岚",missing,ShowMentor);return;}
            activeRelation=id;
            Open("RelationClaims");SpeakerFrame(4);body=CharacterBody();
            Paragraph(body,"许岚：“"+r.title+"”",32,gold);
            for(int i=0;i<r.claims.Length;i++){int claim=i;Button(body,r.claims[i],()=>ChooseEvidence(id,claim),"Claim"+i);}
            AllowBlankDismiss();
        }
        void ChooseEvidence(string id,int claim)
        {
            var r=Story.Book.relations.First(x=>x.id==id);
            ChooseCards("出示依据 · "+r.claims[claim],r.evidence,2,selected=>{var error=Story.Relate(id,claim,selected);
                if(error!=null)Message("许岚 · 再核对一下",error,()=>ShowRelation(id));
                else Message("许岚",r.success,ShowMentor);});
        }
        void ShowFinalIntro()
        {
            Open("FinalPeople");body=Scroll(modal,new Vector2(.14f,.10f),new Vector2(.88f,.87f));
            Paragraph(body,"许岚：“开始复盘，还是再核对一下？”",34,gold);
            Paragraph(body,"先从哪一种已有记录的参与联系说起？选择的是核查重点，不是宣告定罪或免责。",27);
            foreach(int index in new[]{9,6,7,8})
            {
                int actor=index;var p=Panel("PersonSummary",body,Vector2.zero,Vector2.one,Color.clear);FixedHeight(p.gameObject,Portrait?250:185);Horizontal(p);
                var avatar=Panel("Portrait",p,Vector2.zero,Vector2.one,Color.clear);var im=avatar.gameObject.AddComponent<UnityEngine.UI.Image>();im.sprite=portraits[index];im.preserveAspect=true;im.raycastTarget=false;
                // Give every row the same two columns, independent of sprite and label widths.
                var avatarColumn=avatar.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();avatarColumn.minWidth=avatarColumn.preferredWidth=0;avatarColumn.flexibleWidth=1;
                var personChoice=Button(p,index==9?"陈渡 · 多环节联系":index==6?"老赵 · 经手记录":index==7?"苏禾 · 到货与账簿":"周叔 · 交接记录",()=>{
                    if(actor!=9){Message("许岚","这位的记录要按经手范围分别核查。先从与S1存在冲突、又涉及调度和结算的联系开始。",ShowFinalIntro);return;}
                    int next=Story.State.ending=="review"?Story.State.reviewStep:Enumerable.Range(0,4).Where(i=>!Story.State.answers.Any(a=>a.step==i)).DefaultIfEmpty(0).First();ShowReportStep(next);
                },"ReviewPerson_"+index);
                var choiceColumn=personChoice.GetComponent<UnityEngine.UI.LayoutElement>();choiceColumn.minWidth=choiceColumn.preferredWidth=0;choiceColumn.flexibleWidth=1;
            }
            Button(body,"继续核对现场",ClearModal,"KeepInvestigatingButton");AllowBlankDismiss();
        }
        void ShowReportStep(int step)
        {
            var error=Story.ReportGuard();if(error!=null){Message("许岚",error);return;}
            bool revising=Story.State.ending=="review"&&Story.State.answers.Count==4;
            var r=Story.Book.reports[step];Open("ReportStep_"+step);SpeakerFrame(4);body=CharacterBody();
            Paragraph(body,"许岚 · "+r.title,33,gold);Paragraph(body,r.question,28);
            for(int i=0;i<r.claims.Length;i++)
            {
                int claim=i;Button(body,r.claims[i],()=>ChooseCards("林见 · "+r.claims[claim],r.evidence,2,cards=>{
                    var e=Story.AnswerReport(step,claim,cards);if(e!=null){Message("复盘材料",e,()=>ShowReportStep(step));return;}
                    if(revising){Story.SubmitReport();ShowEnding();}
                    else if(step<3)Message("许岚",step==0?"把眼前这票的发现记下。再把历史往来单独说清。":step==1?"历史路径写下了。接着说人分别参与了哪些环节。":"把参与和责任的边界分开。最后说下一步还要查什么。",()=>ShowReportStep(step+1));
                    else {var result=Story.SubmitReport();if(!string.IsNullOrEmpty(result)&&Story.State.ending!="review")Message("许岚",result);else ShowEnding();}
                }),"ReportClaim_"+i);
            }
            AllowBlankDismiss();
        }
        public void ShowEnding()
        {
            if(Story.State.ending=="accepted"){ShowSuccessEnding();return;}
            Open("EndingReview",new Color(.03f,.055f,.07f,.8f));body=Scroll(modal,new Vector2(.14f,.10f),new Vector2(.87f,.88f));
            Paragraph(body,"还差一份解释",42,gold);Paragraph(body,Story.EndingText(),28);
            Button(body,"回到第"+(Story.State.reviewStep+1)+"段修正",()=>ShowReportStep(Story.State.reviewStep),"RetryReportButton");
            Button(body,"回到现场，复看材料",ClearModal,"ReturnToSceneButton");Button(body,"保存并返回封面",SaveAndHome,"EndingSaveButton");
        }
        public void Message(string title,string message,Action then=null)
        {
            int actor=ActorIndex(title.Split(new[]{' ','·'},StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()??"");
            Open("Message");
            if(actor>=3)
            {
                SpeakerFrame(actor);body=CharacterBody();Paragraph(body,title,35,gold);Paragraph(body,message,30);
                AllowBlankDismiss(then);return;
            }
            body=Scroll(modal,new Vector2(.08f,.23f),new Vector2(.92f,.86f));Paragraph(body,title,34,gold);Paragraph(body,message,29);
            AllowBlankDismiss(then);
        }
        void ShowArrivalHint()
        {
            if(arrivalHint!=null)Destroy(arrivalHint.gameObject);
            arrivalHint=Panel("ArrivalHint",canvasRoot,new Vector2(.12f,.17f),new Vector2(.88f,.29f),ink);
            arrivalHint.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var text=Text(arrivalHint,"点击空白处行走 · 点击人物交谈",24);text.enableAutoSizing=true;text.fontSizeMin=18;text.fontSizeMax=Portrait?30:24;text.alignment=TextAlignmentOptions.Center;
            arrivalHintUntil=Time.unscaledTime+1.8f;
        }
        public void ShowNotebook()
        {
            if(home)return;
            Open("Notebook");body=Scroll(modal,new Vector2(.13f,.10f),new Vector2(.87f,.87f));Paragraph(body,"调查手册",36,gold);
            AllowBlankDismiss();
            if(!string.IsNullOrEmpty(Story.State.pending))Button(body,"回到尚未结束的现场对话",ShowDialogue,"ResumeDialogueButton");
            Button(body,"到访地图",ShowMap,"MapButton");
            foreach(var kind in new[]{"记录","陈述","经手陈述","待核观察","旁支观察"})
            {var owned=Story.Book.materials.Where(m=>m.kind==kind&&Story.Has(m.id)).ToArray();if(owned.Length==0)continue;Paragraph(body,kind,31,gold);foreach(var m in owned){Paragraph(body,m.title,28,gold);Paragraph(body,m.text);}}
            if(Story.Has("inspectionReady"))Paragraph(body,"核验说明：交接标签覆盖旧胶痕。收到说明不等于亲自观察，痕迹不能证明擅自启封。",26);
            Paragraph(body,"已支持的关系",29,gold);foreach(var r in Story.Book.relations.Where(r=>Story.Has(r.id)))Paragraph(body,r.success);
            Paragraph(body,"当前待核",29,gold);Paragraph(body,Story.Hint(1));
            Paragraph(body,"人物 · 仅复看已知信息",29,gold);
            foreach(var actor in new[]{3,4,5,6,7,8,9})if(actor<=4||actor==5&&Story.Has("handover")||actor==6&&Story.Has("workerMet")||actor==7&&Story.Has("shopRecordsShared")||actor==8&&Story.Has("courier")||actor==9&&Story.Has("handover"))
            {int index=actor;Button(body,"人物 · "+ActorName(index),()=>ShowProfile(index),"Profile_"+index);}
            if(Story.Has("act1Complete")) {Paragraph(body,"核查方法 · 字段与身份",29,gold);Paragraph(body,"票号对应一票货，生产批次提示可能联系，境内作业单对应仓库作业。三者不能相互替代。来源：本次已核对的票单和逐项记录。",26);}
            if(Story.Has("act2Complete")) {Paragraph(body,"核查方法 · 来源与经手",29,gold);Paragraph(body,"主单、装车、签收和结算各自回答不同问题。陈述确实被记录，不代表其中所有内容已经证实。来源：本次材料与问询记录。",26);}
            if(Story.Book.knowledge!=null)foreach(var knowledge in Story.Book.knowledge.Where(k=>Story.Has(k.unlock))){var k=knowledge;Button(body,"知识卡 · "+k.title,()=>ShowKnowledge(k),"Knowledge_"+k.unlock);}
            foreach(var a in Story.Book.actions.Where(a=>Story.State.completed.Contains(a.id)))Button(body,"复看 · "+a.title,()=>ReviewAction(a.id),"Review_"+a.id);
            Button(body,"重新开始故事",ConfirmNewGame,"RestartStoryButton");
        }
        float SharedMusicGain()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            float web=FogReadMusicPreference();if(web>=0)return Mathf.Clamp01(web);
#endif
            return PlayerPrefs.HasKey(MusicPreference)?Mathf.Clamp01(PlayerPrefs.GetFloat(MusicPreference)):-1;
        }
        void RestoreMusicPreference()
        {
            float shared=SharedMusicGain();if(shared>=0){Story.State.musicGain=shared;return;}
            // Read only the existing setting at startup; do not load or rewrite case progress.
            foreach(var path in new[]{Story.SavePath,Story.SavePath+".bak"})
            {
                if(!File.Exists(path))continue;
                try{var saved=JsonUtility.FromJson<MusicSaveHeader>(File.ReadAllText(path));if(saved!=null&&saved.version==2&&float.IsFinite(saved.musicGain)){Story.State.musicGain=Mathf.Clamp01(saved.musicGain);return;}}
                catch(Exception){/* The normal Continue action handles a damaged save. */}
            }
        }
        public void ToggleMusic()
        {
            Story.State.musicGain=Story.State.musicGain>0?0:1;
#if UNITY_WEBGL && !UNITY_EDITOR
            FogWriteMusicPreference(Story.State.musicGain);
#else
            PlayerPrefs.SetFloat(MusicPreference,Story.State.musicGain);PlayerPrefs.Save();
#endif
            ApplyMusic();
            RefreshMusicButton();
        }
        void ApplyMusic()
        {
            float shared=SharedMusicGain();if(shared>=0)Story.State.musicGain=shared;
            bool enabled=Story.State.musicGain>0;
            RefreshMusicButton();
            foreach(var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                a.mute=!enabled;a.volume=(a.clip!=null&&a.clip.name.Contains("underwater")?.65f:.55f)*Story.State.musicGain;
                if(!enabled)a.Pause();else if(!a.isPlaying){a.UnPause();if(!a.isPlaying)a.Play();}
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            FogWriteMusicPreference(Story.State.musicGain);
#endif
        }
        void ShowHelp()
        {if(home)return;Message("现场提示",Story.Hint(Mathf.Min(hintLevel++,2))+"\n\n点击场景空白处行走；点击人物、物件或出口即可直接交谈、查看或通行，无需走近。手册可随时复看已记录内容。\n\n点击面板外空白处关闭；对话会保存后暂停。\nTab：手册　Esc：关闭当前面板。");}
        string ActorName(int actor)=>actor==3?"林见":actor==4?"许岚":actor==5?"值班员":actor==6?"老赵":actor==7?"苏禾":actor==8?"周叔":"陈渡";
        void ShowProfile(int actor)
        {
            Open("Profile");SpeakerFrame(actor);body=CharacterBody();Paragraph(body,ActorName(actor),34,gold);
            string text=actor==3?"第一次参与现场核查，负责协助观察、比对和整理。":actor==4?"组织核查并带教林见，提醒区分事实、推测与待核事项。":actor==5?"港口值班员，提供计划交接的记录副本。":actor==6?(Story.Has("letterIdentity")?"仓库收发经手人，已确认是开场来信的提供者。":"仓库收发经手人，只愿确认自己经手的记录。自述范围尚需逐项核对。"):actor==7?"店主，已同意提供本次到货和结算资料。配合不直接等于责任已经排除。":actor==8?"承运人，已提供SD-0615的交接记录与附件。":"交接资料所列联系人。具体参与需按记录分别核查。";
            if(actor==9){if(Story.Has("dispatch"))text+="\n调度登记有其修改集中交接地址的签字。";if(Story.Has("S1"))text+="\n首次电话陈述已记为S1。";if(Story.Has("courier"))text+="\n历史签收附件记载其参与现场交接。";if(Story.Has("account"))text+="\n结算账簿列有联系，资金最终归属仍待核对。";}
            Paragraph(body,text);Button(body,"返回调查手册",ShowNotebook,"BackNotebookButton");AllowBlankDismiss();
        }
        void ShowMap()
        {
            ShowMapPage();
        }
        void ShowKnowledge(StoryKnowledge card)
        {
            ShowKnowledgePage(card);
        }
#if UNITY_EDITOR
        // Explicit QA opt-in only. Normal Play and exported offline games never attach a client.
        public void EnableExperimentalMentorChatForEditor()
        {
            if(mentorClient!=null)return;
            mentorClient=gameObject.AddComponent<MentorChatClient>();mentorClient.AvailabilityChanged+=_=>{if(CurrentView=="MentorTopics")ShowMentor();};
        }
#endif
        void ShowMentorChat()
        {
            if(Story.At(Story.State.room,Story.MentorX)!=null){Message("许岚","先走近现场许岚，再继续提问。");return;}
            renderingChat=true;Open("MentorChat");renderingChat=false;body=Scroll(modal,new Vector2(.12f,.36f),new Vector2(.92f,.87f));
            Paragraph(body,"向许岚提问",34,gold);Paragraph(body,"只使用已取得的资料，回答不会替你完成调查。",24);
            foreach(var item in chatHistory){var text=Paragraph(body,item.Key+"："+item.Value,27);text.richText=false;}
            if(!string.IsNullOrEmpty(chatError))Paragraph(body,chatError,26,gold);
            if(mentorClient.Sending)Paragraph(body,"许岚正在整理已有记录……",27,gold);
            var inputRoot=Panel("ChatInput",modal,new Vector2(.12f,.17f),new Vector2(.92f,.32f),ink);
            chatInput=inputRoot.gameObject.AddComponent<TMP_InputField>();chatInput.targetGraphic=inputRoot.GetComponent<UnityEngine.UI.Image>();
            var viewport=Panel("TextViewport",inputRoot,new Vector2(.02f,.06f),new Vector2(.98f,.94f),Color.clear);viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var inputText=Text(viewport,"",Portrait?27:26);inputText.richText=false;inputText.alignment=TextAlignmentOptions.TopLeft;
            var placeholder=Text(viewport,"问一个当前调查的问题……",Portrait?27:26,new Color(.6f,.65f,.66f));
            chatInput.textViewport=viewport;chatInput.textComponent=(TextMeshProUGUI)inputText;chatInput.placeholder=placeholder;chatInput.characterLimit=240;chatInput.lineType=TMP_InputField.LineType.MultiLineSubmit;chatInput.text=chatDraft;chatInput.interactable=!mentorClient.Sending;
            chatInput.onValueChanged.AddListener(v=>chatDraft=v);chatInput.onSubmit.AddListener(_=>SendQuestion(false));
            var controls=Panel("ChatControls",modal,new Vector2(.12f,.055f),new Vector2(.92f,.14f),Color.clear);Horizontal(controls);
            if(mentorClient.Sending)Button(controls,"取消这次提问",()=>{mentorClient.Cancel();chatError="已取消，可以继续核对现场。";ShowMentorChat();},"CancelQuestionButton");
            else {Button(controls,"发送问题",()=>SendQuestion(false),"SendQuestionButton");if(!string.IsNullOrEmpty(chatError)&&!string.IsNullOrEmpty(lastQuestion))Button(controls,"重试上个问题",()=>SendQuestion(true),"RetryQuestionButton");}
            Button(controls,"继续调查",ClearModal,"CloseChatButton");AllowBlankDismiss();
        }
        void SendQuestion(bool retry)
        {
            if(mentorClient==null||mentorClient.Sending)return;string question=(retry?lastQuestion:chatDraft).Trim();if(question.Length==0)return;
            if(!retry){lastQuestion=question;chatHistory.Add(new KeyValuePair<string,string>("林见",question));}chatDraft="";chatError="";
            mentorClient.Ask(Story,question,reply=>{if(!string.IsNullOrEmpty(reply.error))chatError=reply.error;else chatHistory.Add(new KeyValuePair<string,string>("许岚",reply.answer));while(chatHistory.Count>12)chatHistory.RemoveAt(0);if(CurrentView=="MentorChat")ShowMentorChat();});
            ShowMentorChat();
        }
        void ResetChat()
        {if(mentorClient!=null)mentorClient.Cancel();chatHistory.Clear();chatDraft=chatError=lastQuestion="";}
        void Relayout()
        {
            layoutPortrait=Portrait;string view=CurrentView;
            var scaler=canvasRoot.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.referenceResolution=Portrait?new Vector2(900,1600):new Vector2(1600,900);scaler.matchWidthOrHeight=Portrait?0:1;
            var tools=(RectTransform)canvasRoot.Find("Tools");LayoutTools(tools);
            var near=(RectTransform)canvasRoot.Find("Nearby");near.anchorMin=new Vector2(Portrait?.12f:.34f,.08f);near.anchorMax=new Vector2(Portrait?.88f:.66f,.16f);
            foreach(var t in tools.GetComponentsInChildren<TMP_Text>(true))t.fontSize=Portrait?37.7f:26;
            nearButton.GetComponentInChildren<TMP_Text>(true).fontSize=Portrait?37.7f:26;placeLabel.fontSize=Portrait?46.4f:32;
            if(view=="OpeningDrive"||view=="ArrivalReveal"||view=="EndingAccepted")return;
            // Anchored reading/confirmation panels resize in place. Replacing them
            // here would discard their callbacks or restart the opening timer.
            if(view=="Letter"||view=="Prologue"||view=="Message"||view=="SaveConfirmation")return;
            if(view=="Review"&&!string.IsNullOrEmpty(pageReviewId)){ShowReviewPage(pageReviewId);return;}
            if(view=="KnowledgeCard"&&pageKnowledge!=null){ShowKnowledgePage(pageKnowledge);return;}
            if(view=="SaveSlots"){ShowSaves();return;}
            if(view=="Cover"){ShowHome();return;}if(home)return;
            if(view=="Dialogue"){ShowDialogue();return;}
            if(view=="EvidencePresentation"&&evidenceRedraw!=null){evidenceRedraw();return;}
            if(view.StartsWith("ReportStep_")){ShowReportStep(int.Parse(view.Substring(11)));return;}
            if(view.StartsWith("Inspection_")){ShowInspection(view.Substring(11));return;}
            if(view=="RelationClaims"&&!string.IsNullOrEmpty(activeRelation)){ShowRelation(activeRelation);return;}
            if(view=="MentorTopics"){ShowMentor();return;}if(view=="InquiryTopics"){ShowInquiries();return;}if(view=="NpcTopics"){ShowNpc(lastNpcActor);return;}
            if(view=="Notebook"){ShowNotebook();return;}if(view=="Map"){ShowMap();return;}if(view=="WeightInspection"){ShowWeight();return;}if(view=="CargoInspection"){ShowCargo();return;}
            if(view=="MentorChat"){ShowMentorChat();return;}
            if(view=="FinalPeople"){ShowFinalIntro();return;}if(view=="EndingAccepted"||view=="EndingReview"){ShowEnding();return;}ClearModal();
        }
        void Update()
        {
            TickCount++;
            if(Story==null||hero==null)return;
#if UNITY_WEBGL && !UNITY_EDITOR
            if(Time.unscaledTime>=nextMusicPoll)
            {
                nextMusicPoll=Time.unscaledTime+.5f;float shared=SharedMusicGain();
                if(shared>=0&&!Mathf.Approximately(shared,Story.State.musicGain)){Story.State.musicGain=shared;ApplyMusic();}
            }
#endif
            if(arrivalHint!=null&&Time.unscaledTime>=arrivalHintUntil){Destroy(arrivalHint.gameObject);arrivalHint=null;}
            if(layoutPortrait!=Portrait)Relayout();
            if(CurrentView=="Cover")
            {
                if(!coverPresented)return;
                coverRemaining-=Time.deltaTime;
                if(coverRemaining<=0)EnterFromCover();
                return;
            }
            if(CurrentView=="Prologue"||CurrentView=="OpeningDrive")return;
            if(CurrentView=="EndingAccepted")
            {
                if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)SaveAndHome();
                return;
            }
            if(CurrentView=="Letter")
            {
                var letterKeys=Keyboard.current;
                if(letterKeys!=null&&(letterKeys.escapeKey.wasPressedThisFrame||letterKeys.enterKey.wasPressedThisFrame||letterKeys.spaceKey.wasPressedThisFrame))modal.GetComponent<FogLetterView>().Continue();
                return;
            }
            if(CurrentView=="MentorChat")
            {
                float shift=TouchScreenKeyboard.visible?Mathf.Max(TouchScreenKeyboard.area.height,Screen.height*.30f)/Screen.height*canvasRoot.rect.height:0;
                foreach(var name in new[]{"ChatInput","ChatControls"}){var panel=modal.Find(name) as RectTransform;if(panel!=null)panel.anchoredPosition=new Vector2(0,shift);}
            }
            if(arrival>=0)
            {
                arrival+=Time.deltaTime;
                if(arrivalReveal!=null){arrivalReveal.color=new Color(0,0,0,1-Mathf.SmoothStep(0,1,arrival/.6f));if(arrival>=.6f){if(CurrentView=="ArrivalReveal")ClearModal();arrivalReveal=null;}}
                if(arrival>=.6f){arrival=-1;ShowArrivalHint();}return;
            }
            var keyboard=Keyboard.current;
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame){if(IsModal&&!home){if(dismissModal!=null)DismissModal();else{Story.Save();ClearModal();}}else if(!home)ShowNotebook();}
            if(keyboard!=null&&keyboard.tabKey.wasPressedThisFrame&&!home){if(IsModal){if(dismissModal!=null)DismissModal();else ClearModal();}else ShowNotebook();}
            if(home)return;
            bool pausedDialogue=!string.IsNullOrEmpty(Story.State.pending);
            var nearest=Nearest();
            nearButton.gameObject.SetActive(!IsModal&&(pausedDialogue||Mathf.Abs(nearest.x-Story.X)<1.25f));
            if(nearButton.gameObject.activeSelf&&(nearbySpot!=nearest||nearbyPaused!=pausedDialogue))
            {nearbyLabel.text=pausedDialogue?"继续现场对话  ·  E / 点按":nearest.title+"  ·  E / 点按";nearbySpot=nearest;nearbyPaused=pausedDialogue;}
            placeLabel.color=!IsModal&&Time.unscaledTime<placeUntil?gold:Color.clear;
            marker.gameObject.SetActive(nearButton.gameObject.activeSelf&&!pausedDialogue);
            if(marker.gameObject.activeSelf){var view=sceneCamera.WorldToViewportPoint(new Vector3(nearest.x,Floor+2.7f,0));marker.anchoredPosition=new Vector2(view.x*canvasRoot.rect.width,view.y*canvasRoot.rect.height);}
            if(IsModal){hero.sprite=people[0];return;}
            if(pausedDialogue){if(keyboard!=null&&keyboard.eKey.wasPressedThisFrame)ShowDialogue();return;}
            if(keyboard!=null&&(keyboard.eKey.wasPressedThisFrame||keyboard.spaceKey.wasPressedThisFrame)){Interact(nearest.id);return;}
            var pointer=Pointer.current;
            var touch=Touchscreen.current?.primaryTouch;
            bool touchPressed=touch!=null&&touch.press.wasPressedThisFrame;
            bool pressed=touchPressed||(pointer!=null&&pointer.press.wasPressedThisFrame);
            if(pressed&&Time.frameCount!=ignoreScenePointerFrame)
            {
                var pos=touchPressed?touch.position.ReadValue():pointer.position.ReadValue();
                if(pointerData==null)pointerData=new PointerEventData(EventSystem.current);else pointerData.Reset();
                pointerData.position=pos;pointerHits.Clear();EventSystem.current.RaycastAll(pointerData,pointerHits);
                if(pointerHits.Count==0)
                {
                    var world=sceneCamera.ScreenToWorldPoint(new Vector3(pos.x,pos.y,10));targetX=Mathf.Clamp(world.x,.7f,23.3f);pendingTarget="";
                    var s=Nearest(world.x);
                    // Visible targets take priority over the extended staircase hit area.
                    // Otherwise the upper part of the town exit (x=23.1) opens the stairs.
                    if(Mathf.Abs(s.x-world.x)<.8f&&world.y<Floor+2.65f&&world.y>Floor-.25f){Interact(s.id);return;}
                    if(Story.State.room=="warehouse"&&world.x>18&&world.x<21.5f&&world.y>-.5f&&world.y<2.6f){Interact(Spots.First(p=>p.id=="door").id);return;}
                }
            }
            float dir=0;if(keyboard!=null){if(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed)dir=-1;if(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed)dir=1;}
            if(dir!=0){targetX=Mathf.Clamp(Story.X+dir,.7f,23.3f);pendingTarget="";}
            float old=Story.X;float next=Mathf.MoveTowards(old,targetX,3.1f*Time.deltaTime);bool moving=Mathf.Abs(next-old)>.0001f;
            if(moving)
            {
                Story.Position(next,next<old);movementUnsaved=true;walkClock+=Time.deltaTime;WalkingFrames++;int pose=Mathf.FloorToInt(walkClock*7)%4;hero.sprite=people[pose==0?1:pose==2?2:0];
                hero.flipX=Story.State.facingLeft;hero.transform.position=new Vector3(next,Floor+Mathf.Sin(walkClock*10)*.018f,-.1f);
            }
            else
            {
                hero.sprite=people[0];hero.transform.position=new Vector3(Story.X,Floor,-.1f);
                if(!string.IsNullOrEmpty(pendingTarget)){var id=pendingTarget;pendingTarget="";Interact(id);}
            }
            var goal=new Vector3(CameraX(),0,-10);sceneCamera.transform.position=Vector3.Lerp(sceneCamera.transform.position,goal,1-Mathf.Exp(-5*Time.deltaTime));
            if(Time.unscaledTime-lastSaved>3){lastSaved=Time.unscaledTime;if(movementUnsaved&&Story.Save())movementUnsaved=false;}
        }
        void OnApplicationPause(bool pause){if(pause&&!home)Story?.Save();}
        void OnApplicationQuit(){if(!home)Story?.Save();}
        public void SaveForPageHide(){if(!home)Story?.Save();}
        void OnDestroy()
        {
            foreach(var sprite in people)if(sprite!=null)Destroy(sprite);
            foreach(var sprite in portraits)if(sprite!=null)Destroy(sprite);
            foreach(var sprite in objects)if(sprite!=null)Destroy(sprite);
            if(foregroundStrips!=null)foreach(var sprite in foregroundStrips)if(sprite!=null)Destroy(sprite);
            if(notebookToolArt!=null)Destroy(notebookToolArt);
        }
    }
}
