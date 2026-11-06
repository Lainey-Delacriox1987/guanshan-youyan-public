using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuanshanYouyan.Story
{
    public sealed class FogPrologueView : MonoBehaviour
    {
        public const string Caption="雾港，你是林见，潮水浮现，港口的异动等你探明。";
        public const float Duration=3f;
        const float FadeIn=.55f,FadeOut=.45f;
        TMP_Text label;Action completed;float elapsed,leaving=-1,leaveAlpha;bool finished;
        public void Configure(TMP_Text text,Action then){label=text;completed=then;label.alpha=0;}
        public void Continue()
        {
            if(elapsed<.45f||leaving>=0||finished)return;
            leaving=0;leaveAlpha=label.alpha;
        }
        void Update()
        {
            elapsed+=Time.deltaTime;
            var key=Keyboard.current;
            if(key!=null&&(key.spaceKey.wasPressedThisFrame||key.enterKey.wasPressedThisFrame||key.escapeKey.wasPressedThisFrame))Continue();
            if(leaving>=0){leaving+=Time.deltaTime;label.alpha=leaveAlpha*(1-Mathf.SmoothStep(0,1,leaving/.65f));if(leaving>=.65f)Finish();return;}
            label.alpha=elapsed<FadeIn?Mathf.SmoothStep(0,1,elapsed/FadeIn):1-Mathf.SmoothStep(0,1,(elapsed-(Duration-FadeOut))/FadeOut);
            if(elapsed>=Duration)Finish();
        }
        void Finish(){if(finished)return;finished=true;completed?.Invoke();}
    }
}
