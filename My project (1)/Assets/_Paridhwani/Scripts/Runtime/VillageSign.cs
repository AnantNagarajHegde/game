using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;

namespace Paridhwani
{
    public readonly struct VillageSignLayout
    {
        public readonly int Width,Height;
        public readonly float FontSize,Padding;
        public VillageSignLayout(float worldWidth,float worldHeight,bool kannada)
        {
            double width=float.IsFinite(worldWidth)?Math.Abs(worldWidth):1;
            double height=float.IsFinite(worldHeight)?Math.Abs(worldHeight):.25;
            if(width<.001)width=1;if(height<.001)height=.25;
            double aspect=Math.Min(32,Math.Max(.25,width/height));
            double pixels=Math.Min(2048,Math.Max(128,width*512));
            double rows=pixels/aspect;
            if(rows>512){rows=512;pixels=rows*aspect;}
            if(rows<64){rows=64;pixels=rows*aspect;}
            Width=(int)Math.Min(2048,Math.Max(32,Math.Round(pixels/8)*8));
            Height=(int)Math.Min(512,Math.Max(64,Math.Round(rows/8)*8));
            Padding=Height*.08f;FontSize=Height*(kannada?.62f:.56f);
        }
        public static float Fit(float current,float measuredWidth,float measuredHeight,float availableWidth,float availableHeight)
        {
            if(!float.IsFinite(current) || current<=0)return 1;
            if(!float.IsFinite(measuredWidth) || !float.IsFinite(measuredHeight) || measuredWidth<=0 || measuredHeight<=0)return current;
            if(!float.IsFinite(availableWidth) || !float.IsFinite(availableHeight) || availableWidth<=0 || availableHeight<=0)return current;
            float ratio=Math.Min(availableWidth/measuredWidth,availableHeight/measuredHeight);
            return ratio>=1?current:Math.Max(1,current*ratio*.98f);
        }
    }

    // Shape text once per board; share the two dynamic font atlases across all signs.
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class VillageSign : MonoBehaviour
    {
        sealed class FontEntry {public FontAsset asset;public int users;}
        static readonly Dictionary<Font,FontEntry> fonts=new Dictionary<Font,FontEntry>();
        public static int SharedFontCount => fonts.Count;
        public string text;
        public Font font;
        public ThemeStyleSheet theme;
        public bool kannada;
        RenderTexture texture;
        PanelSettings settings;
        UIDocument document;
        Font leasedFont;
        Material material,previousMaterial;
        MeshRenderer board;
        Label label;
        VillageSignLayout layout;
        static FontAsset AcquireFont(Font source)
        {
            if(!fonts.TryGetValue(source,out var entry)) {
                entry=new FontEntry{asset=FontAsset.CreateFontAsset(source)};fonts.Add(source,entry);
            }
            entry.users++;return entry.asset;
        }
        void Start()
        {
            if(!font){Debug.LogWarning("World sign has no assigned font: "+name,this);return;}
            var scale=transform.lossyScale;layout=new VillageSignLayout(scale.x,scale.y,kannada);
            texture=new RenderTexture(layout.Width,layout.Height,0){name="Sign: "+text,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.Create();
            settings=ScriptableObject.CreateInstance<PanelSettings>();
            settings.targetTexture=texture;settings.themeStyleSheet=theme;
            settings.clearColor=true;settings.colorClearValue=new Color(.09f,.16f,.13f,1);
            settings.scaleMode=PanelScaleMode.ConstantPixelSize;
            document=gameObject.AddComponent<UIDocument>();document.panelSettings=settings;
            var root=document.rootVisualElement;
            root.style.width=layout.Width;root.style.height=layout.Height;
            root.style.justifyContent=Justify.Center;root.style.alignItems=Align.Center;
            root.style.backgroundColor=new Color(.09f,.16f,.13f,1);
            leasedFont=font;var asset=AcquireFont(leasedFont);
            label=new Label(text){enableRichText=false,pickingMode=PickingMode.Ignore,name="world-sign-text"};
            label.style.unityFontDefinition=FontDefinition.FromSDFFont(asset);
            label.style.unityTextGenerator=TextGeneratorType.Advanced;
            label.style.fontSize=layout.FontSize;label.style.whiteSpace=WhiteSpace.NoWrap;
            label.style.color=new Color(.87f,.82f,.66f);
            label.style.unityTextAlign=TextAnchor.MiddleCenter;
            // Keep shaping extents inside the texture rather than clipping long names.
            label.style.width=layout.Width-layout.Padding*2;label.style.flexShrink=0;
            root.Add(label);root.pickingMode=PickingMode.Ignore;
            label.RegisterCallback<GeometryChangedEvent>(_=>FitText());
            root.schedule.Execute(FitText);
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetTexture("_BaseMap",texture);
            board=GetComponent<MeshRenderer>();previousMaterial=board.sharedMaterial;board.sharedMaterial=material;
        }
        void FitText()
        {
            if(label==null || label.panel==null)return;
            var measured=label.MeasureTextSize(text??"",0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined);
            float current=label.resolvedStyle.fontSize;
            float fitted=VillageSignLayout.Fit(current,measured.x,measured.y,layout.Width-layout.Padding*2,layout.Height-layout.Padding*2);
            if(Math.Abs(fitted-current)>.1f)label.style.fontSize=fitted;
        }
        void OnDestroy()
        {
            if(board && board.sharedMaterial==material)board.sharedMaterial=previousMaterial;
            if(document)Destroy(document);
            if(texture){texture.Release();Destroy(texture);}
            if(settings)Destroy(settings);
            if(leasedFont && fonts.TryGetValue(leasedFont,out var entry) && --entry.users<=0) {
                if(entry.asset)Destroy(entry.asset);fonts.Remove(leasedFont);
            }
            if(material)Destroy(material);
        }
    }
}
