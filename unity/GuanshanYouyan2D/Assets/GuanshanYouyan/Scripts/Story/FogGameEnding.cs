using UnityEngine;

namespace GuanshanYouyan.Story
{
    public sealed partial class FogGame
    {
        void ShowSuccessEnding()
        {
            Open("EndingAccepted",Color.black);SetProloguePage(true);
            var viewport=Panel("EndingViewport",modal,Vector2.zero,Vector2.one,Color.black);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<FogEndingScrollRect>();
            var art=Panel("EndingPoster",viewport,new Vector2(0,1),Vector2.one,Color.clear);art.pivot=new Vector2(.5f,1);
            var image=art.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite=Resources.Load<Sprite>("Ending/SuccessPosterClean");image.raycastTarget=false;
            if(image.sprite==null)throw new System.InvalidOperationException("Missing cleaned original ending poster");
            art.gameObject.AddComponent<FogEndingPosterText>().Configure();
            scroll.viewport=viewport;scroll.content=art;scroll.horizontal=false;scroll.vertical=true;
            scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;scroll.inertia=false;scroll.scrollSensitivity=36;
            viewport.gameObject.AddComponent<FogEndingRoll>().Configure(viewport,art,scroll,3);
        }
    }
}
