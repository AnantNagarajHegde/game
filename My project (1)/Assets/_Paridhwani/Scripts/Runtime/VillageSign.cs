using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;

namespace Paridhwani
{
    // Render shaped text into a world sign; never use legacy TextMesh for Kannada.
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class VillageSign : MonoBehaviour
    {
        public string text;
        public Font font;
        public bool kannada;
        RenderTexture texture;
        PanelSettings settings;
        FontAsset fontAsset;
        Material material;
        void Start()
        {
            texture=new RenderTexture(1536,256,0){name="Sign: "+text};
            texture.Create();
            settings=ScriptableObject.CreateInstance<PanelSettings>();
            settings.targetTexture=texture;
            settings.clearColor=true; settings.colorClearValue=new Color(.09f,.16f,.13f,1);
            settings.scaleMode=PanelScaleMode.ConstantPixelSize;
            var document=gameObject.AddComponent<UIDocument>();
            document.panelSettings=settings;
            var root=document.rootVisualElement;
            root.style.width=1536; root.style.height=256;
            root.style.justifyContent=Justify.Center; root.style.alignItems=Align.Center;
            root.style.backgroundColor=new Color(.09f,.16f,.13f,1);
            fontAsset=FontAsset.CreateFontAsset(font);
            var label=new Label(text);
            label.style.unityFontDefinition=FontDefinition.FromSDFFont(fontAsset);
            label.style.unityTextGenerator=TextGeneratorType.Advanced;
            label.style.fontSize=kannada?138:96;
            label.style.color=new Color(.87f,.82f,.66f);
            label.style.unityTextAlign=TextAnchor.MiddleCenter;
            root.Add(label); root.pickingMode=PickingMode.Ignore;
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetTexture("_BaseMap",texture);
            GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        void OnDestroy()
        {
            if(texture) { texture.Release(); Destroy(texture); }
            if(settings) Destroy(settings);
            if(fontAsset) Destroy(fontAsset);
            if(material) Destroy(material);
        }
    }
}
