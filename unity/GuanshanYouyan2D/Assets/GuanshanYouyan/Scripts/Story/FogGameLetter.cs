using System;
using TMPro;
using UnityEngine;

namespace GuanshanYouyan.Story
{
    public sealed partial class FogGame
    {
        Action letterReturn;
        float letterOpenedAt;
        public void ShowLetter(Action then=null)
        {
            if(CurrentView!="Letter")letterOpenedAt=Time.time;
            letterReturn=then;
            Open("Letter",Color.black);SetProloguePage(true);
            // Keep this opaque backing outside the fading content group.
            var area=Panel("LetterContent",modal,Vector2.zero,Vector2.one,Color.clear);
            var sheet=Panel("LetterSheet",area,Vector2.zero,Vector2.one,Color.clear);
            var image=sheet.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite=Resources.Load<Sprite>("Letter/LetterPaper");image.preserveAspect=true;image.raycastTarget=false;
            if(image.sprite==null)throw new InvalidOperationException("Missing supplied letter paper");
            var words=Panel("LetterWritingArea",area,Vector2.zero,Vector2.one,Color.clear);
            var text=Text(words,LetterText(Story.Book),40,new Color(.19f,.105f,.055f,1));text.name="LetterHandwriting";
            text.font=Resources.Load<TMP_FontAsset>("Letter/LongCang-Letter");
            if(text.font==null)throw new InvalidOperationException("Missing handwritten letter font");
            text.fontSize=text.fontSizeMax=52;text.fontSizeMin=18;text.enableAutoSizing=true;text.characterWidthAdjustment=0;
            text.alignment=TextAlignmentOptions.Center;text.margin=Vector4.zero;
            text.lineSpacing=12;text.paragraphSpacing=14;text.characterSpacing=2;
            text.richText=false;text.overflowMode=TextOverflowModes.Truncate;
            var fade=area.gameObject.AddComponent<CanvasGroup>();
            var view=modal.gameObject.AddComponent<FogLetterView>();view.Configure(fade,letterOpenedAt,CompleteLetter);
            var controls=Panel("LetterContinue",area,Vector2.zero,Vector2.zero,Color.clear);
            var button=Button(controls,"继续",view.Continue,"FinishLetterButton");Stretch((RectTransform)button.transform);
            button.GetComponent<UnityEngine.UI.Image>().color=new Color(.24f,.18f,.115f,.95f);
            button.GetComponentInChildren<TMP_Text>().alignment=TextAlignmentOptions.Center;
            var layout=area.gameObject.AddComponent<FogLetterLayout>();layout.Configure(sheet,words,controls,image.sprite.rect.width/image.sprite.rect.height);
        }
        public static string LetterText(PortBook book)
        {
            var action=Array.Find(book.actions,a=>a.id=="prologue");
            return "致林见：\n\n　　"+action.lines[1].text.Trim('“','”');
        }
        void CompleteLetter()
        {
            var then=letterReturn;letterReturn=null;ClearModal();then?.Invoke();
        }
        void FinishLetterAndArrive()
        {
            Story.State.openingDrivePending=true;Story.State.openingDriveShot=0;
            while(Story.Current!=null)Story.Next();
            StartArrival();
        }
    }
}
