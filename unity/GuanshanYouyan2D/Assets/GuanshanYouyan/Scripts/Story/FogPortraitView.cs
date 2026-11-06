using UnityEngine;

namespace GuanshanYouyan.Story
{
    public static class FogPortraitLayout
    {
        // Visible upper-body bounds in the two 1254px atlases. Transparent cell
        // padding must not participate in sizing or each speaker changes scale.
        static readonly Rect[] VisibleBounds={
            new Rect(122,267,172,371),new Rect(548,267,168,370),new Rect(932,267,223,372),
            new Rect(192,878,241,359),new Rect(848,878,165,357),new Rect(192,251,241,365),new Rect(821,251,215,368)
        };
        public const float HeightFraction=.50f;
        public const float WideHeightFraction=.60f;
        public const float ColumnRight=.335f;
        public static readonly Vector2 PanelMin=new Vector2(.36f,.10f);
        public static readonly Vector2 PanelMax=new Vector2(.96f,.86f);
        public const float WidestAspect=241f/359f;
        public static Sprite Create(Texture2D texture,int actor)
        {
            var r=VisibleBounds[actor-3];r=new Rect(r.x*texture.width/1254f,r.y*texture.height/1254f,r.width*texture.width/1254f,r.height*texture.height/1254f);
            var sprite=Sprite.Create(texture,r,new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);sprite.name="Portrait_"+actor;return sprite;
        }
        public static float Height(Vector2 canvas)
        {
            float aspect=canvas.x/canvas.y;bool portrait=aspect<.85f;
            float fraction=Mathf.Lerp(HeightFraction,WideHeightFraction,Mathf.InverseLerp(1.25f,1.6f,aspect));
            // Shrink the whole cast together only when a narrow screen needs it.
            return Mathf.Min(canvas.y*fraction,canvas.x*(portrait?.32f:.31f)/WidestAspect);
        }
        public static void Fit(RectTransform target,Vector2 canvas,Sprite sprite,float columnRight)
        {
            bool portrait=canvas.x/canvas.y<.85f;float height=Height(canvas);
            target.anchorMin=target.anchorMax=Vector2.zero;target.pivot=new Vector2(.5f,0);target.localScale=Vector3.one;
            target.sizeDelta=new Vector2(height*sprite.rect.width/sprite.rect.height,height);
            float center=portrait?.185f:(.025f+columnRight)*.5f;target.anchoredPosition=new Vector2(canvas.x*center,0);
        }
    }
    public sealed class FogPortraitView : MonoBehaviour
    {
        RectTransform canvas,target;UnityEngine.UI.Image image;float columnRight;Vector2 previous;
        public void Configure(RectTransform root,float right)
        {canvas=root;target=(RectTransform)transform;image=GetComponent<UnityEngine.UI.Image>();columnRight=right;Refresh();}
        void LateUpdate(){if(canvas!=null&&canvas.rect.size!=previous)Refresh();}
        void Refresh(){previous=canvas.rect.size;FogPortraitLayout.Fit(target,previous,image.sprite,columnRight);}
    }
}
