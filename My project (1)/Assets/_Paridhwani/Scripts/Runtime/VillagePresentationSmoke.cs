using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;
namespace Paridhwani {
 // Called only by the opt-in smoke test, after text panels have had time to shape.
 public static class VillagePresentationSmoke {
 public static IEnumerator CheckSigns() {
 yield return new WaitForEndOfFrame();
 var signs=UnityEngine.Object.FindObjectsByType<VillageSign>();
 if(signs.Length==0)throw new InvalidOperationException("No active world signs found for presentation checks.");
 var fonts=new System.Collections.Generic.Dictionary<Font,FontAsset>();
 foreach(var sign in signs) {
 var doc=sign.GetComponent<UIDocument>();var label=doc?doc.rootVisualElement.Q<Label>("world-sign-text"):null;
 var texture=doc?doc.panelSettings.targetTexture:null;
 if(label==null || texture==null || label.enableRichText || label.text!=(sign.text??""))throw new InvalidOperationException("Missing or non-literal world sign: "+sign.name);
 var scale=sign.transform.lossyScale;var expected=new VillageSignLayout(scale.x,scale.y,sign.kannada);
 if(texture.width!=expected.Width || texture.height!=expected.Height)throw new InvalidOperationException("Wrong world-sign texture dimensions: "+sign.name);
 var measured=label.MeasureTextSize(label.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined);
 if(measured.x>texture.width-expected.Padding*2+2 || measured.y>texture.height-expected.Padding*2+2)throw new InvalidOperationException("World-sign text exceeds its padded texture: "+sign.name);
 var asset=label.resolvedStyle.unityFontDefinition.fontAsset;
 if(!asset)throw new InvalidOperationException("World sign has no shaped font asset: "+sign.name);
 if(fonts.TryGetValue(sign.font,out var shared) && shared!=asset)throw new InvalidOperationException("World signs duplicated the same font atlas.");
 fonts[sign.font]=asset;
 }
 if(VillageSign.SharedFontCount>2)throw new InvalidOperationException("World signs allocated more than the two bundled font atlases.");
 Debug.Log("VILLAGE_SIGN_LAYOUT_CHECKED signs="+signs.Length+" fonts="+VillageSign.SharedFontCount);
 }
 }
}
