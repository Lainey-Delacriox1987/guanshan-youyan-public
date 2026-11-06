using UnityEngine;

namespace GuanshanYouyan.Story
{
    public sealed class FogLetterLayout : MonoBehaviour
    {
        RectTransform root,sheet,words,controls;Vector2 previous;float aspect;
        public void Configure(RectTransform paper,RectTransform writing,RectTransform button,float ratio)
        {root=(RectTransform)transform;sheet=paper;words=writing;controls=button;aspect=ratio;Refresh();}
        void LateUpdate(){if(root!=null&&root.rect.size!=previous)Refresh();}
        void Refresh()
        {
            previous=root.rect.size;float width=Mathf.Max(previous.x,previous.y*aspect),height=width/aspect;
            sheet.anchorMin=sheet.anchorMax=sheet.pivot=new Vector2(.5f,.5f);sheet.sizeDelta=new Vector2(width,height);sheet.anchoredPosition=Vector2.zero;
            // Center the complete text block, with symmetric room for wrapping.
            words.anchorMin=words.anchorMax=words.pivot=new Vector2(.5f,.5f);
            words.anchoredPosition=Vector2.zero;
            words.sizeDelta=new Vector2(Mathf.Min(previous.x*.80f,width*.65f),Mathf.Min(previous.y*.66f,height*.55f));
            float pixelsToCanvas=previous.y/Mathf.Max(1,Screen.height);
            controls.anchorMin=controls.anchorMax=controls.pivot=new Vector2(1,0);
            controls.anchoredPosition=new Vector2(-previous.x*.06f,previous.y*.05f);
            controls.sizeDelta=new Vector2(Mathf.Min(previous.x*.32f,Mathf.Max(130,150*pixelsToCanvas)),Mathf.Max(52,44*pixelsToCanvas));
        }
    }
}
