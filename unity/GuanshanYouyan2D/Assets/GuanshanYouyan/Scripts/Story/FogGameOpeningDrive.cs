using System;
using UnityEngine;

namespace GuanshanYouyan.Story
{
    public sealed partial class FogGame
    {
        UnityEngine.UI.Image arrivalReveal;
        void ShowOpeningDrive()
        {
            if(CurrentView=="OpeningDrive"&&modal!=null)return;
            Open("OpeningDrive",Color.black);SetProloguePage(true);
            var scenes=new Sprite[3];for(int i=0;i<scenes.Length;i++)
            {scenes[i]=Resources.Load<Sprite>("OpeningDrive/Road"+(i+1));if(scenes[i]==null)throw new InvalidOperationException("Missing opening road "+(i+1));}
            modal.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var road=Panel("OpeningRoad",modal,Vector2.zero,Vector2.one,Color.clear);
            var roadImage=road.gameObject.AddComponent<UnityEngine.UI.Image>();roadImage.raycastTarget=false;
            var aspect=road.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;aspect.aspectRatio=scenes[0].rect.width/scenes[0].rect.height;
            var vehicle=Panel("OpeningVehicle",modal,Vector2.zero,Vector2.zero,Color.clear);vehicle.pivot=new Vector2(.5f,0);
            var busImage=vehicle.gameObject.AddComponent<UnityEngine.UI.Image>();busImage.sprite=shuttleArt;busImage.preserveAspect=true;busImage.raycastTarget=false;
            var foreground=new FogDriveForeground[2];
            for(int i=0;i<foreground.Length;i++)
            {
                var go=new GameObject(i==0?"OpeningForegroundTrunks":"OpeningForegroundLeaves",typeof(RectTransform),typeof(FogDriveForeground));
                var rt=(RectTransform)go.transform;rt.SetParent(modal,false);rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
                foreground[i]=go.GetComponent<FogDriveForeground>();foreground[i].Configure(roadImage,i==1);
            }
            var black=Panel("OpeningBlackFade",modal,Vector2.zero,Vector2.one,Color.black).GetComponent<UnityEngine.UI.Image>();black.raycastTarget=false;
            var view=modal.gameObject.AddComponent<FogOpeningDriveView>();
            view.Configure(modal,roadImage,vehicle,black,scenes,foreground,Story.State.openingDriveShot,index=>{Story.State.openingDriveShot=index;Story.Save();},FinishOpeningDrive);
            var click=modal.gameObject.AddComponent<UnityEngine.UI.Button>();click.transition=UnityEngine.UI.Selectable.Transition.None;click.onClick.AddListener(view.Continue);
        }
        void FinishOpeningDrive()
        {
            Story.State.openingDrivePending=false;Story.State.openingDriveShot=0;Story.Save();BeginHarborArrival(true);
        }
        void BeginHarborArrival(bool fade=false)
        {
            ClearModal();arrival=fade?0:-1;Story.Position(4.1f,false);ApplyRoom(true);hero.gameObject.SetActive(true);placeUntil=Time.unscaledTime+8;
            if(fade){Open("ArrivalReveal",Color.black);SetProloguePage(true);arrivalReveal=modal.GetComponent<UnityEngine.UI.Image>();}
            else ShowArrivalHint();
        }
    }
}
