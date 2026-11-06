using UnityEngine;

namespace GuanshanYouyan.Story
{
    public sealed partial class FogGame
    {
        Sprite notebookToolArt;
        void NotebookToolButton(RectTransform parent)
        {
            if(notebookToolArt==null)
            {
                var texture=Resources.Load<Texture2D>("UI/NotebookButton-v2");
                // The edited asset contains only the book and lettering over true transparency.
                notebookToolArt=Sprite.Create(texture,new Rect(232,128,1611,486),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            }
            var go=new GameObject("NotebookButton",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent,false);Stretch((RectTransform)go.transform);
            var image=go.GetComponent<UnityEngine.UI.Image>();image.sprite=notebookToolArt;image.color=Color.white;
            // Image defaults to rectangular hit testing. Avoid its alpha-test setter:
            // Unity validates CPU readability even when assigning the default zero.
            image.preserveAspect=true;
            var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.white;colors.selectedColor=Color.white;
            colors.pressedColor=new Color(.82f,.82f,.82f,1);button.colors=colors;
            button.onClick.AddListener(ShowNotebook);
        }
    }
}
