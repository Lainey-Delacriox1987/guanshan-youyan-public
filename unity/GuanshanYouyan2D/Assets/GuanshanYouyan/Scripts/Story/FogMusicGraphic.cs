using UnityEngine;
using UnityEngine.UI;

namespace GuanshanYouyan.Story
{
    // A native UI glyph stays sharp at any display size.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FogMusicGraphic : MaskableGraphic
    {
        bool muted;
        public bool Muted {get=>muted;set{if(muted==value)return;muted=value;SetVerticesDirty();}}
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=GetPixelAdjustedRect();float size=Mathf.Min(rect.width,rect.height);
            Vector2 center=rect.center;Color pale=new Color(.91f,.86f,.70f,1);
            var background=new Color(.06f,.075f,.08f,.68f);
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;
                Triangle(mesh,center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*size*.49f,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*size*.49f,background);
            }
            System.Action<Vector2,Vector2,float,Color> line=(a,b,w,c)=>Line(mesh,center+a*size,center+b*size,w*size,c);
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;
                line(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.48f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.48f,.012f,new Color(.8f,.75f,.60f,.45f));
            }
            var speaker=new[]{new Vector2(-.26f,-.09f),new Vector2(-.15f,-.09f),new Vector2(-.015f,-.21f),new Vector2(-.015f,.21f),new Vector2(-.15f,.09f),new Vector2(-.26f,.09f)};
            for(int i=0;i<speaker.Length;i++)line(speaker[i],speaker[(i+1)%speaker.Length],.032f,pale);
            if(muted){line(new Vector2(.12f,-.085f),new Vector2(.29f,.085f),.032f,pale);line(new Vector2(.12f,.085f),new Vector2(.29f,-.085f),.032f,pale);}
            else foreach(float radius in new[]{.14f,.24f})for(int i=0;i<12;i++)
            {
                float a=Mathf.Lerp(-.9f,.9f,i/12f),b=Mathf.Lerp(-.9f,.9f,(i+1)/12f);
                line(new Vector2(.025f+Mathf.Cos(a)*radius,Mathf.Sin(a)*radius),new Vector2(.025f+Mathf.Cos(b)*radius,Mathf.Sin(b)*radius),.026f,pale);
            }
        }
        static void Triangle(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Color color)
        {int i=mesh.currentVertCount;mesh.AddVert(a,color,Vector2.zero);mesh.AddVert(b,color,Vector2.zero);mesh.AddVert(c,color,Vector2.zero);mesh.AddTriangle(i,i+1,i+2);}
        static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color color)
        {var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Triangle(mesh,a+n,b+n,b-n,color);Triangle(mesh,a+n,b-n,a-n,color);}
    }
}
