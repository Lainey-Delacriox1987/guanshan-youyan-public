using System;
using TMPro;
using UnityEngine;

namespace GuanshanYouyan.Story
{
    // The user supplied illustration stays a bitmap. Only lettering is live text.
    public sealed class FogEndingPosterText : MonoBehaviour
    {
        public static readonly string[][] Copy={
            new[]{"看似遥远的走私犯罪","其实就潜伏在我们每一次不经意的选择之间","贪婪的深渊正在脚下悄然裂开","那些跨越国境的隐秘包裹","往往藏着足以摧毁一切的致命毒药"},
            new[]{"每一道被突破的防线背后","都是无数家庭在默默承受着的无声的痛"},
            new[]{"我们抽丝剥茧寻找的不仅是线索","更是守护这片土地安宁的初心","在这场没有硝烟的战争里","每一次清醒的选择都是对正义的投票"},
            new[]{"让那些试图挑战法律底线的阴暗角落","都在正义的阳光下无处遁形","守护国门不仅是海关卫士的职责","更是我们每一个公民共同的誓言"},
            new[]{"当正义的拼图终于完整","你会发现所有的坚持都拥有了最耀眼的意义"},
            new[]{"愿你的智慧永远用于守护光明","让走私的阴霾彻底消散在历史长河"}
        };
        struct Line{public TMP_Text label;public float x,y,width,size;}
        Line[] lines;Material titleMaterial;
        public void Configure()
        {
            var font=Resources.Load<TMP_FontAsset>("Ending/PosterSerif");if(font==null)throw new InvalidOperationException("Missing poster serif font");
            lines=new Line[20];int index=0;
            lines[index++]=Make(font,"关山有眼",370,78,330,70,true);
            float[] xs={362,240,560,236,548,362},ys={346,642,910,1202,1628,1808},widths={548,390,320,390,344,600};
            for(int group=0;group<Copy.Length;group++)for(int line=0;line<Copy[group].Length;line++)
            {
                var label=Make(font,Copy[group][line],xs[group],ys[group]+line*35,widths[group],18,false);
                if(group==5)label.label.color=new Color(.075f,.11f,.16f,1);lines[index++]=label;
            }
            Layout();
        }
        Line Make(TMP_FontAsset font,string text,float x,float y,float width,float size,bool title)
        {
            var go=new GameObject(title?"PosterTitle":"PosterBodyLine",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(transform,false);
            var label=go.GetComponent<TextMeshProUGUI>();label.font=font;label.text=text;label.color=new Color(.98f,.93f,.76f,1);
            label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.NoWrap;label.richText=false;label.raycastTarget=false;
            label.enableAutoSizing=false;label.overflowMode=TextOverflowModes.Overflow;
            if(title)
            {
                label.enableVertexGradient=true;label.colorGradient=new VertexGradient(new Color(1,.95f,.74f),new Color(1,.95f,.74f),new Color(.87f,.65f,.31f),new Color(.87f,.65f,.31f));
                titleMaterial=new Material(font.material);titleMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth,.012f);titleMaterial.SetColor(ShaderUtilities.ID_OutlineColor,new Color(.21f,.16f,.09f,1));label.fontSharedMaterial=titleMaterial;
            }
            return new Line{label=label,x=x,y=y,width=width,size=size};
        }
        void OnRectTransformDimensionsChange(){if(lines!=null)Layout();}
        void Layout()
        {
            float scale=((RectTransform)transform).rect.width/724f;if(scale<=0)return;
            foreach(var line in lines)
            {
                var rt=line.label.rectTransform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(.5f,.5f);
                rt.anchoredPosition=new Vector2(line.x*scale,-line.y*scale);rt.sizeDelta=new Vector2(line.width*scale,(line.size>60?120:34)*scale);
                line.label.fontSizeMax=line.size*scale;line.label.fontSizeMin=(line.size>60?68:15)*scale;line.label.fontSize=line.size*scale;
            }
        }
        void OnDestroy(){if(titleMaterial!=null)Destroy(titleMaterial);}
    }
}
