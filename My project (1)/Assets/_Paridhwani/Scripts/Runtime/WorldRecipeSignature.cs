using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace Paridhwani {
 public static class WorldRecipeSignature {
 public static string Compute(string directory) {
 if(!Directory.Exists(directory))return "";
 using(var sha=SHA256.Create()) {
 // Verification and assessment attachment menus do not author world geometry or bindings.
 foreach(string path in Directory.GetFiles(directory,"*.cs",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal)) {
 string relative=path.Substring(directory.TrimEnd(Path.DirectorySeparatorChar).Length).Replace('\\','/');
 if(string.Equals(relative,"/StandalonePlayerPlaytest.cs",StringComparison.OrdinalIgnoreCase) ||
    string.Equals(relative,"/TeaAssessmentMenu.cs",StringComparison.OrdinalIgnoreCase))continue;
 var bytes=Encoding.UTF8.GetBytes(relative+"\n"+File.ReadAllText(path).Replace("\r\n","\n")+"\n");sha.TransformBlock(bytes,0,bytes.Length,null,0);
 }
 sha.TransformFinalBlock(Array.Empty<byte>(),0,0);return Convert.ToBase64String(sha.Hash);
 }
 }
 }
 public sealed partial class TeaStallSession {
 public const int RequiredWorldRevision=3;
 [UnityEngine.HideInInspector] public int generatedWorldRevision;
 [UnityEngine.HideInInspector] public string generatedRecipeSignature;
 bool RuntimeWorldReady() {
 if(generatedWorldRevision!=RequiredWorldRevision || villagers==null || villagers.Length!=14 || !trunkAnchor || !councilPresident || !farmArrival || !checkpostArrival)return false;
#if UNITY_EDITOR
 try {return generatedRecipeSignature==WorldRecipeSignature.Compute(System.IO.Path.Combine(UnityEngine.Application.dataPath,"_Paridhwani","Scripts","Editor"));}catch(Exception){return false;}
#else
 return true;
#endif
 }
 }
}
