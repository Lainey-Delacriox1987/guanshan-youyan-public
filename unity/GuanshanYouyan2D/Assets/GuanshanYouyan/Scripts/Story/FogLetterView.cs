using System;
using UnityEngine;

namespace GuanshanYouyan.Story
{
    public sealed class FogLetterView : MonoBehaviour
    {
        public const float ReadUntil=15f;
        const float FadeIn=.8f;
        CanvasGroup canvas;Action completed;float openedAt,leavingAt=-1,leaveAlpha;bool finished;
        public void Configure(CanvasGroup group,float startedAt,Action then)
        {canvas=group;openedAt=startedAt;completed=then;Paint();}
        public void Continue()
        {
            if(Time.time-openedAt<.45f||leavingAt>=0||finished)return;
            leavingAt=Time.time;leaveAlpha=canvas.alpha;
        }
        void Update()
        {
            if(finished)return;
            if(leavingAt>=0)
            {
                float dt=Time.time-leavingAt;
                canvas.alpha=leaveAlpha*(1-Mathf.SmoothStep(0,1,dt/.65f));
                if(dt>=.65f)Finish();return;
            }
            Paint();
        }
        void Paint()
        {
            float elapsed=Time.time-openedAt;
            canvas.alpha=Mathf.SmoothStep(0,1,elapsed/FadeIn);
        }
        void Finish(){if(finished)return;finished=true;completed?.Invoke();}
    }
}
