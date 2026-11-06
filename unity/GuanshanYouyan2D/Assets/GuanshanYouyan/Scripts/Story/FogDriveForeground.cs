using UnityEngine;
using UnityEngine.UI;

namespace GuanshanYouyan.Story
{
    // Reuse the original road pixels, aligned to the road's actual cropped rect.
    // Only the near-side scenery belongs in front of the vehicle.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FogDriveForeground : MaskableGraphic
    {
        Image road;bool foliage;int shot;Material ownedMaterial;
        public override Texture mainTexture=>road!=null&&road.sprite!=null?road.sprite.texture:Texture2D.whiteTexture;
        public void Configure(Image source,bool leaves)
        {
            road=source;foliage=leaves;raycastTarget=false;
            if(leaves)
            {
                ownedMaterial=new Material(Resources.Load<Shader>("OpeningDrive/DriveForeground"));
                float cutoff=.085f,feather=.025f;
                if(QualitySettings.activeColorSpace==ColorSpace.Linear){feather=Mathf.GammaToLinearSpace(cutoff+feather)-Mathf.GammaToLinearSpace(cutoff);cutoff=Mathf.GammaToLinearSpace(cutoff);}
                ownedMaterial.SetFloat("_DarkCutoff",cutoff);ownedMaterial.SetFloat("_Feather",feather);material=ownedMaterial;
            }
            SetAllDirty();
        }
        public void SetShot(int value){shot=value;SetAllDirty();}
        public void RefreshGeometry()=>SetVerticesDirty();
        protected override void OnDestroy(){if(ownedMaterial!=null)Destroy(ownedMaterial);base.OnDestroy();}
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(road==null||road.sprite==null)return;
            if(!foliage)
            {
                if(shot==0){Quad(mesh,441,350,584,941);Quad(mesh,1600,350,1672,941);}
                else if(shot==1)
                {
                    Quad(mesh,99,350,180,941);Quad(mesh,1339,350,1432,941);
                    // The near guardrail is on this side of the lane.
                    Polygon(mesh,new Vector2(0,784),new Vector2(271,795),new Vector2(271,816),new Vector2(0,808));
                    Polygon(mesh,new Vector2(0,832),new Vector2(271,842),new Vector2(271,864),new Vector2(0,854));
                    Quad(mesh,271,780,345,941);Quad(mesh,343,800,778,822);Quad(mesh,343,841,778,861);Quad(mesh,778,786,849,941);
                }
                else
                {
                    Polygon(mesh,new Vector2(217,470),new Vector2(283,470),new Vector2(281,750),new Vector2(208,750));
                    Polygon(mesh,new Vector2(0,755),new Vector2(335,749),new Vector2(340,798),new Vector2(0,819));
                    Polygon(mesh,new Vector2(0,819),new Vector2(12,819),new Vector2(12,879),new Vector2(0,882));
                    Quad(mesh,341,733,373,850);
                }
            }
            else if(shot==0)
            {
                Polygon(mesh,new Vector2(0,730),new Vector2(140,680),new Vector2(216,787),new Vector2(300,754),new Vector2(389,871),new Vector2(389,941),new Vector2(0,941));
                Polygon(mesh,new Vector2(584,848),new Vector2(714,778),new Vector2(861,900),new Vector2(1195,848),new Vector2(1195,941),new Vector2(584,941));
                Polygon(mesh,new Vector2(1195,909),new Vector2(1550,778),new Vector2(1672,804),new Vector2(1672,941),new Vector2(1195,941));
            }
            else if(shot==1)
            {
                Quad(mesh,0,858,778,941);Quad(mesh,849,865,1339,941);
                Polygon(mesh,new Vector2(1432,826),new Vector2(1550,741),new Vector2(1672,753),new Vector2(1672,941),new Vector2(1432,941));
            }
            else
            {
                Quad(mesh,0,636,208,754);Quad(mesh,1270,878,1672,941);
            }
        }
        void Quad(VertexHelper mesh,float x0,float y0,float x1,float y1)=>Polygon(mesh,new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1));
        void Polygon(VertexHelper mesh,params Vector2[] points)
        {
            var sprite=road.sprite;var rect=road.rectTransform.rect;var uv=sprite.textureRect;int first=mesh.currentVertCount;
            foreach(var pixel in points)
            {
                var p=new Vector2(pixel.x/1672f,1-pixel.y/941f);
                var position=rectTransform.InverseTransformPoint(road.rectTransform.TransformPoint(new Vector3(rect.xMin+p.x*rect.width,rect.yMin+p.y*rect.height,0)));
                mesh.AddVert(position,color,new Vector2((uv.x+p.x*uv.width)/sprite.texture.width,(uv.y+p.y*uv.height)/sprite.texture.height));
            }
            for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(first,first+i,first+i+1);
        }
    }
}
