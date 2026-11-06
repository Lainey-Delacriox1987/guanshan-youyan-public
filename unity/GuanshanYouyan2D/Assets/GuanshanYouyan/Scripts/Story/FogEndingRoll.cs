using UnityEngine;

namespace GuanshanYouyan.Story
{
    public sealed class FogEndingRoll : MonoBehaviour
    {
        public const float InitialHold=.35f,ManualHold=1.2f,ScreenHeightsPerSecond=.045f;
        RectTransform viewport,art;FogEndingScrollRect scroll;
        float aspect,maximum,elapsed,manualRemaining;bool dragging,configured;Vector2 previousSize;
        public float Progress=>maximum>0?Mathf.Clamp01(art.anchoredPosition.y/maximum):1;
        public bool Finished=>maximum<=0||art.anchoredPosition.y>=maximum-.01f;
        public bool AutoPlaying=>configured&&!dragging&&manualRemaining<=0&&elapsed>=InitialHold&&!Finished;
        public float Maximum=>maximum;
        public void Configure(RectTransform view,RectTransform content,FogEndingScrollRect controller,float heightOverWidth)
        {
            viewport=view;art=content;scroll=controller;aspect=heightOverWidth;scroll.Roll=this;
            elapsed=manualRemaining=0;dragging=false;configured=true;Canvas.ForceUpdateCanvases();Fit();
        }
        void Fit()
        {
            float progress=maximum>0?Progress:0;previousSize=viewport.rect.size;
            float height=previousSize.x*aspect;art.sizeDelta=new Vector2(0,height);
            maximum=Mathf.Max(0,height-previousSize.y);art.anchoredPosition=new Vector2(0,progress*maximum);scroll.StopMovement();
        }
        public void Advance(float delta)
        {
            if(!configured||!isActiveAndEnabled||viewport==null||art==null)return;
            if(viewport.rect.size!=previousSize)Fit();
            delta=Mathf.Clamp(delta,0,.1f);elapsed+=delta;
            // Always read the actual scroll position, including wheel and drag changes.
            float offset=Mathf.Clamp(art.anchoredPosition.y,0,maximum);
            if(dragging)return;
            if(manualRemaining>0){manualRemaining=Mathf.Max(0,manualRemaining-delta);return;}
            if(elapsed<InitialHold||offset>=maximum-.01f)return;
            scroll.StopMovement();offset=Mathf.Min(maximum,offset+previousSize.y*ScreenHeightsPerSecond*delta);
            art.anchoredPosition=new Vector2(0,offset);
        }
        public void BeginManualDrag(){dragging=true;scroll.StopMovement();}
        public void EndManualDrag(){dragging=false;manualRemaining=ManualHold;}
        public void ManualScroll(){dragging=false;manualRemaining=ManualHold;}
        public void CancelManualDrag(){dragging=false;}
        void OnApplicationFocus(bool focus){if(!focus)CancelManualDrag();}
        void OnApplicationPause(bool pause){if(pause)CancelManualDrag();}
    }
}
