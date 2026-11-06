using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuanshanYouyan.Story
{
    public sealed class FogOpeningDriveView : MonoBehaviour
    {
        public const float ShotDuration=3.6f,FinalShotDuration=4.8f,ParkAt=3.2f,FadeDuration=.45f;
        RectTransform root,vehicle;UnityEngine.UI.Image background,veil;Sprite[] roads;
        FogDriveForeground[] foreground;
        Action<int> changed;Action completed;float elapsed,skipAt=-1,skipAlpha,busAspect;int shot;bool finished;Vector2 previousSize;
        public int Shot=>shot;
        public float Elapsed=>elapsed;
        public float BlackAlpha=>veil.color.a;
        public RectTransform Vehicle=>vehicle;
        public void Configure(RectTransform canvas,UnityEngine.UI.Image road,RectTransform bus,UnityEngine.UI.Image black,Sprite[] scenes,FogDriveForeground[] front,int first,Action<int> onShot,Action then)
        {
            root=canvas;background=road;vehicle=bus;veil=black;roads=scenes;shot=Mathf.Clamp(first,0,roads.Length-1);changed=onShot;completed=then;
            var sprite=bus.GetComponent<UnityEngine.UI.Image>().sprite;busAspect=sprite.rect.width/sprite.rect.height;
            foreground=front;ChangeRoad();Paint();
        }
        public void Continue()
        {
            if(elapsed<.45f||skipAt>=0||finished)return;
            skipAt=0;skipAlpha=veil.color.a;
        }
        void Update()
        {
            if(finished)return;
            var keys=Keyboard.current;
            if(keys!=null&&(keys.escapeKey.wasPressedThisFrame||keys.spaceKey.wasPressedThisFrame||keys.enterKey.wasPressedThisFrame))Continue();
            if(skipAt>=0)
            {
                skipAt+=Time.unscaledDeltaTime;
                veil.color=new Color(0,0,0,Mathf.Lerp(skipAlpha,1,Mathf.SmoothStep(0,1,skipAt/.45f)));
                if(skipAt>=.45f)Finish();return;
            }
            elapsed+=Time.unscaledDeltaTime;
            float duration=shot==roads.Length-1?FinalShotDuration:ShotDuration;
            if(elapsed>=duration)
            {
                if(shot==roads.Length-1){veil.color=Color.black;Finish();return;}
                elapsed-=duration;shot++;ChangeRoad();changed?.Invoke(shot);
            }
        }
        void LateUpdate(){if(!finished&&skipAt<0)Paint();}
        void ChangeRoad(){background.sprite=roads[shot];foreach(var front in foreground)front.SetShot(shot);}
        void Paint()
        {
            if(root==null)return;
            // Close roadside shots frame the van prominently; the gate is a wider shot.
            var rect=background.rectTransform.rect;float height=rect.height*(shot==roads.Length-1?.235f:.40f),width=height*busAspect;
            vehicle.localScale=Vector3.one;vehicle.sizeDelta=new Vector2(width,height);
            if(rect.size!=previousSize){previousSize=rect.size;foreach(var front in foreground)front.RefreshGeometry();}
            float start=-width*.55f/rect.width,roadY=shot==1?.155f:.105f;
            float x;
            if(shot==roads.Length-1)
            {
                // Stop with the cab at the gate, then hold before the harbor reveal.
                float t=Mathf.Clamp01(elapsed/ParkAt);float approach=1-Mathf.Pow(1-t,2);
                x=Mathf.Lerp(start,.60f,approach);roadY=Mathf.Lerp(.105f,.14f,approach);
            }
            else x=Mathf.Lerp(start,1-start,elapsed/ShotDuration);
            var local=root.InverseTransformPoint(background.rectTransform.TransformPoint(new Vector3(rect.xMin+x*rect.width,rect.yMin+roadY*rect.height,0)));
            vehicle.anchoredPosition=(Vector2)local-root.rect.min;
            float duration=shot==roads.Length-1?FinalShotDuration:ShotDuration;
            float alpha=elapsed<FadeDuration?1-Mathf.SmoothStep(0,1,elapsed/FadeDuration):Mathf.SmoothStep(0,1,(elapsed-(duration-FadeDuration))/FadeDuration);
            veil.color=new Color(0,0,0,alpha);
        }
        void Finish(){if(finished)return;finished=true;completed?.Invoke();}
    }
}
