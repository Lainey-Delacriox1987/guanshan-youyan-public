using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

namespace GuanshanYouyan.Story
{
    public sealed partial class FogGame
    {
        int openingCameraMask;Color openingCameraColor;CameraClearFlags openingClearFlags;bool openingCameraHidden;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void FogSetPrologueVisible(int visible);
        [DllImport("__Internal")] static extern void FogCoverFrameReady();
        System.Collections.IEnumerator WaitForPresentedCover(GameObject page)
        {
            // The browser must not uncover Unity while its startup overlay fades.
            while(!UnityEngine.Rendering.SplashScreen.isFinished)
            {if(page==null||!page.activeInHierarchy)yield break;yield return null;}
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            if(page!=null&&page.activeInHierarchy&&CurrentView=="Cover")
            {coverPresented=true;FogCoverFrameReady();}
        }
#endif
        void SetProloguePage(bool visible)
        {
            // Opening screens are entirely UI. Never expose the harbor while
            // a new canvas is being registered or a letter is fading in.
            bool hideWorld=visible&&CurrentView!="ArrivalReveal";
            if(sceneCamera!=null)
            {
                if(hideWorld&&!openingCameraHidden){openingCameraMask=sceneCamera.cullingMask;openingCameraColor=sceneCamera.backgroundColor;openingClearFlags=sceneCamera.clearFlags;openingCameraHidden=true;}
                if(hideWorld){sceneCamera.cullingMask=0;sceneCamera.backgroundColor=Color.black;sceneCamera.clearFlags=CameraClearFlags.SolidColor;}
                else if(openingCameraHidden){sceneCamera.cullingMask=openingCameraMask;sceneCamera.backgroundColor=openingCameraColor;sceneCamera.clearFlags=openingClearFlags;openingCameraHidden=false;}
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            FogSetPrologueVisible(visible?1:0);
#endif
        }
        void ShowPrologue()
        {
            // Resizing must not restart the reading time or flash the text.
            if(CurrentView=="Prologue"&&modal!=null)return;
            Open("Prologue",Color.black);SetProloguePage(true);
            var label=Text(modal,FogPrologueView.Caption,42,Color.white);
            label.name="PrologueCaption";label.font=Resources.Load<TMP_FontAsset>("Intro/LongCang-Intro");
            label.alignment=TextAlignmentOptions.Center;label.margin=Vector4.zero;
            label.textWrappingMode=TextWrappingModes.NoWrap;label.enableAutoSizing=true;
            label.fontSize=label.fontSizeMax=42;label.fontSizeMin=8;label.richText=false;
            // Fit the complete line by font size alone, never squeeze glyph width.
            label.characterWidthAdjustment=0;
            label.overflowMode=TextOverflowModes.Truncate;
            var rt=label.rectTransform;rt.localScale=Vector3.one;rt.anchorMin=new Vector2(.06f,.43f);rt.anchorMax=new Vector2(.94f,.57f);rt.offsetMin=rt.offsetMax=Vector2.zero;
            var view=modal.gameObject.AddComponent<FogPrologueView>();view.Configure(label,CompletePrologue);
            var click=modal.gameObject.AddComponent<UnityEngine.UI.Button>();
            click.transition=UnityEngine.UI.Selectable.Transition.None;click.onClick.AddListener(view.Continue);
        }
        void CompletePrologue()
        {
            if(Story.State.pending!="prologue")return;
            // Persist the reading position before displaying the original letter.
            // The letterReceived prerequisite is awarded only after reading it.
            if(Story.State.line==0)Story.Next();
            ShowLetter(FinishLetterAndArrive);
        }
    }
}
