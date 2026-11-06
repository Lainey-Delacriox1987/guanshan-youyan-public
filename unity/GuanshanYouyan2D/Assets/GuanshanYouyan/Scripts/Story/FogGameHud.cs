using UnityEngine;

namespace GuanshanYouyan.Story
{
    public sealed partial class FogGame
    {
        FogMusicGraphic musicGraphic;
        void LayoutTools(RectTransform tools)
        {
            tools.anchorMin=tools.anchorMax=Vector2.one;tools.pivot=Vector2.one;
            tools.sizeDelta=new Vector2(330,88);tools.anchoredPosition=new Vector2(-112,-20);
        }
        void BuildMusicButton()
        {
            var go=new GameObject("MusicButton",typeof(RectTransform),typeof(FogMusicGraphic),typeof(UnityEngine.UI.Button));go.transform.SetParent(canvasRoot,false);
            var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one;
            rt.sizeDelta=new Vector2(44,44);rt.anchoredPosition=new Vector2(-36,-36);
            musicGraphic=go.GetComponent<FogMusicGraphic>();musicGraphic.Muted=Story.State.musicGain<=0;
            var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=musicGraphic;
            button.transition=UnityEngine.UI.Selectable.Transition.None;button.onClick.AddListener(ToggleMusic);
        }
        void RefreshMusicButton()
        {
            if(musicGraphic==null)return;
            musicGraphic.Muted=Story.State.musicGain<=0;
            musicGraphic.gameObject.SetActive(true);
            musicGraphic.transform.SetAsLastSibling();
        }
    }
}
