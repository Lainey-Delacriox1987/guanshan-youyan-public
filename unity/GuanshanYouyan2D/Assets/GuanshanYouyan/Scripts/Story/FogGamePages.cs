using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuanshanYouyan.Story
{
    public sealed partial class FogGame
    {
        string pageReviewId;StoryKnowledge pageKnowledge;
        readonly struct PageAction
        {
            public readonly string Label,Name;public readonly Action Click;
            public PageAction(string label,Action click,string name){Label=label;Click=click;Name=name;}
        }
        RectTransform PageShell(string title)
        {
            var page=Panel("SinglePagePanel",modal,new Vector2(.07f,.11f),new Vector2(.93f,.86f),ink);
            PageText(page,"PageTitle",title,new Vector2(.04f,.87f),new Vector2(.96f,.98f),35,gold);
            return page;
        }
        TMP_Text PageText(Transform parent,string name,string value,Vector2 min,Vector2 max,float size,Color color)
        {
            var t=Text(parent,value,size,color);t.name=name;
            var rt=t.rectTransform;rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;
            t.fontSize=t.fontSizeMax=size;t.fontSizeMin=size-2;t.enableAutoSizing=true;
            t.margin=new Vector4(10,6,10,6);t.alignment=TextAlignmentOptions.TopLeft;t.paragraphSpacing=10;
            t.overflowMode=TextOverflowModes.Overflow;return t;
        }
        void PageButtons(RectTransform page,IList<PageAction> actions,bool horizontal=false)
        {
            if(actions.Count==0)return;
            float top=horizontal||actions.Count==1?.19f:actions.Count==2?.28f:.39f;
            var footer=Panel("PageActions",page,new Vector2(.04f,.055f),new Vector2(.96f,top),Color.clear);
            for(int i=0;i<actions.Count;i++)
            {
                var action=actions[i];var b=Button(footer,action.Label,action.Click,action.Name);
                var rt=(RectTransform)b.transform;
                if(horizontal){float cell=1f/actions.Count;rt.anchorMin=new Vector2(i*cell+.005f,0);rt.anchorMax=new Vector2((i+1)*cell-.005f,1);}
                else{float cell=1f/actions.Count;rt.anchorMin=new Vector2(0,1-(i+1)*cell+.012f);rt.anchorMax=new Vector2(1,1-i*cell-.012f);}
                rt.offsetMin=rt.offsetMax=Vector2.zero;
                var label=b.GetComponentInChildren<TMP_Text>();label.fontSize=label.fontSizeMax=27;label.fontSizeMin=24;label.enableAutoSizing=true;
                label.margin=new Vector4(18,5,18,5);label.overflowMode=TextOverflowModes.Overflow;
            }
        }
        void ObjectPage(string title,int objectIndex,string description,string note,IList<PageAction> actions)
        {
            var page=PageShell(title);float bottom=actions.Count>1?.44f:.27f;
            float left=objectIndex>=0?.35f:.045f;
            if(objectIndex>=0)
            {
                var art=Panel("InspectedObject",page,new Vector2(.035f,bottom),new Vector2(.31f,.83f),Color.clear);
                var image=art.gameObject.AddComponent<Image>();image.sprite=objects[objectIndex];image.preserveAspect=true;image.raycastTarget=false;
            }
            bool hasNote=!string.IsNullOrEmpty(note);
            PageText(page,"PageDescription",description,new Vector2(left,hasNote?.43f:bottom),new Vector2(.96f,.82f),actions.Count>1?30:32,paper);
            if(hasNote)PageText(page,"PageNote",note,new Vector2(left,.26f),new Vector2(.96f,.41f),26,paper);
            PageButtons(page,actions);AllowBlankDismiss();
        }
        void DocumentPage(string title,string text,IList<PageAction> actions=null)
        {
            var page=PageShell(title);
            PageText(page,"DocumentText",text,new Vector2(.04f,actions==null?.07f:.27f),new Vector2(.96f,.83f),29,paper);
            if(actions!=null)PageButtons(page,actions,true);AllowBlankDismiss();
        }
        void ShowCargoPage()
        {
            Open("CargoInspection");
            ObjectPage("查验点 · 当前票 QJ-017",0,"外箱记录：收纳盒\n\n实物记录：乳霜等化妆品\n\n生产批次：K17","把实物与申报分开记录。\n生产批次不能替代货物身份。",new[]{new PageAction("记录查验结果",()=>Begin("cargo"),"RecordCargoButton")});
        }
        void ShowWeightPage()
        {
            Open("WeightInspection");var a=Story.Action("weight");var actions=new List<PageAction>();
            for(int i=0;i<a.choices.Length;i++){int choice=i;actions.Add(new PageAction(a.choices[i],()=>{var e=Story.Begin("weight",choice);if(e!=null)Message("再核对一下",e,ShowWeight);else ShowDialogue();},"WeightChoice"+i));}
            ObjectPage("台面上的两份记录",-1,"第一日 16:40 · 两份记录票号均为 QJ-017\n申报：收纳盒 · 净重 800 kg\n复核：同一净重口径 · 净重 1260 kg\n票号与口径一致。哪一个判断成立？",null,actions);
        }
        void ShowInspectionPage(string id)
        {
            var a=Story.Action(id);Open("Inspection_"+id);
            int index=a.inspect=="parcels"?1:a.inspect=="camera"?2:a.inspect=="dispatch"?3:a.inspect=="shelf"||a.inspect=="account"?4:a.inspect=="wildlife"?5:-1;
            var actions=new List<PageAction>();
            if(a.choices.Length==0)actions.Add(new PageAction("核对并记录",()=>Begin(id),"Record_"+id));
            else for(int i=0;i<a.choices.Length;i++){int choice=i;actions.Add(new PageAction(a.choices[i],()=>{var e=Story.Begin(id,choice);if(e!=null)Message("再核对一下",e,()=>ShowInspection(id));else ShowDialogue();},"ActionChoice_"+i));}
            ObjectPage(a.title,index,a.lines[0].text,null,actions);
        }
        void ShowReviewPage(string id)
        {
            pageReviewId=id;
            var a=Story.Action(id);Open("Review");DocumentPage(a.title+" · 已记录",string.Join("\n\n",a.lines.Select(l=>l.speaker+"："+l.text)));
        }
        void ShowKnowledgePage(StoryKnowledge card)
        {
            pageKnowledge=card;
            Open("KnowledgeCard");DocumentPage(card.title,card.text+"\n\n来源："+card.source,new[]{
                new PageAction("查看官方来源",()=>{if(Uri.TryCreate(card.url,UriKind.Absolute,out var url)&&url.Scheme=="https")Application.OpenURL(card.url);},"KnowledgeSourceButton"),
                new PageAction("返回调查手册",ShowNotebook,"BackNotebookButton")});
        }
        void ShowMapPage()
        {
            Open("Map");var page=PageShell("到访地图");PageText(page,"MapNotice","可返回已经亲自到访的区域。新记录仍要在现场取得。",new Vector2(.04f,.74f),new Vector2(.96f,.85f),26,paper);
            var grid=Panel("MapRooms",page,new Vector2(.04f,.07f),new Vector2(.96f,.71f),Color.clear);
            int rows=(Story.Book.rooms.Length+1)/2;
            for(int i=0;i<Story.Book.rooms.Length;i++)
            {
                var room=Story.Book.rooms[i];bool visited=Story.State.visited.Contains(room.id);
                var b=Button(grid,room.title+(visited?"":" · 尚未到访"),()=>{var e=Story.Enter(room.id,true);if(e!=null)Message("到访地图",e,ShowMap);else{ClearModal();ApplyRoom(true);}},"MapRoom_"+room.id);
                b.interactable=visited&&Story.IsUnlocked(room.id);var rt=(RectTransform)b.transform;
                int row=i/2;
                rt.anchorMin=new Vector2(i%2*.5f+.008f,1-(row+1f)/rows+.012f);rt.anchorMax=new Vector2((i%2+1)*.5f-.008f,1-(float)row/rows-.012f);rt.offsetMin=rt.offsetMax=Vector2.zero;
            }
            AllowBlankDismiss();
        }
    }
}
