using UnityEngine;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        // Visual descriptors only. Quest identities, role enums and anchors stay in
        // the scene builders. Both Raghavendra instances intentionally share a look.
        readonly struct CastLook
        {
            public readonly string skin,cloth,lower,hair;
            public readonly bool female,elder,moustache,glasses;
            public CastLook(string skin,string cloth,string lower,string hair="292822",bool female=false,bool elder=false,bool moustache=true,bool glasses=false)
            {this.skin=skin;this.cloth=cloth;this.lower=lower;this.hair=hair;this.female=female;this.elder=elder;this.moustache=moustache;this.glasses=glasses;}
        }
        static CastLook LookFor(string name)
        {
            switch(name)
            {
                case "Somanna":return new CastLook("A77855","BCB79F","686B60","9B9B8C",elder:true,glasses:true);
                case "Shanthakka":return new CastLook("A16A49","88604D","7B554C",female:true,moustache:false);
                case "Basavaraju":return new CastLook("9E704F","7D8C72","585B4C");
                case "Giri":return new CastLook("A4704C","6A8076","424F4A",moustache:false);
                case "Gurumurthy":return new CastLook("AD805E","CEC4A8","BBB095","72746B",elder:true);
                case "Rathnamma":return new CastLook("9B6A4F","936E7E","71616E","939387",female:true,elder:true,moustache:false);
                case "Shresthi":return new CastLook("B48360","A2ADB0","4B5358",glasses:true);
                case "Raghavendra":return new CastLook("9D6C4D","8E8566","595B4B");
                case "Kencha":return new CastLook("987151","817E63","5D624E");
                case "Ananya":return new CastLook("AF7C57","A48C46","526458",female:true,moustache:false);
                case "Kamalamma":return new CastLook("A77D61","817168","6B6462","BCB9A6",female:true,elder:true,moustache:false);
                case "Council president":return new CastLook("A87752","CAC2A8","A7A58F","777A6D",elder:true,glasses:true);
                default:return new CastLook("A9704F","C6BEA3","394441");
            }
        }
        static Mesh CastRibbon(Vector3[] path,float width)
        {
            var v=new Vector3[path.Length*2];var t=new int[(path.Length-1)*6];
            for(int i=0;i<path.Length;i++){v[i*2]=path[i]+Vector3.left*width;v[i*2+1]=path[i]+Vector3.right*width;}
            for(int i=0;i<path.Length-1;i++){int a=i*2,b=i*6;t[b]=a;t[b+1]=a+1;t[b+2]=a+2;t[b+3]=a+1;t[b+4]=a+3;t[b+5]=a+2;}
            var mesh=new Mesh();mesh.vertices=v;mesh.triangles=t;mesh.RecalculateNormals();return mesh;
        }
        static void CastDetails(string name,string id,CastLook look,Transform model,Transform head,Material hair,Material ink,Material trim,Material lower)
        {
            if(look.female)
            {
                if(name=="Ananya")
                {
                    CharacterPart(id+"_TiedHair",CharacterLoft(new[]{new CharacterRing(-.31f,.025f,.034f),new CharacterRing(-.22f,.045f,.044f),new CharacterRing(-.10f,.043f,.040f),new CharacterRing(.01f,.027f,.027f)},18),hair,head,V(0,-.025f,-.153f));
                    var bag=CharacterColour("AnanyaBag","4B6350");
                    CharacterPart(id+"_Bag",CharacterLoft(new[]{new CharacterRing(.80f,.06f,.09f),new CharacterRing(.85f,.085f,.10f),new CharacterRing(1.07f,.085f,.10f),new CharacterRing(1.11f,.06f,.08f)},12),bag,model,V(-.25f,0,0));
                    CharacterPart(id+"_BagStrap",CastRibbon(new[]{V(-.26f,1.06f,.145f),V(-.12f,1.24f,.15f),V(.16f,1.41f,.125f)},.013f),bag,model);
                    CharacterPart(id+"_Scarf",CastRibbon(new[]{V(-.12f,.66f,.139f),V(-.14f,1.10f,.147f),V(-.13f,1.38f,.136f)},.036f),trim,model);
                }
                else
                {
                    CharacterPart(id+"_HairBun",CharacterLoft(new[]{new CharacterRing(-.075f,.025f,.03f),new CharacterRing(-.04f,.073f,.067f),new CharacterRing(.04f,.075f,.069f),new CharacterRing(.085f,.024f,.03f)},20),hair,head,V(0,-.015f,-.145f));
                    CharacterPart(id+"_SariSkirt",CharacterLoft(new[]{new CharacterRing(.11f,.205f,.14f),new CharacterRing(.20f,.218f,.143f),new CharacterRing(.54f,.216f,.139f),new CharacterRing(.82f,.188f,.123f),new CharacterRing(.94f,.182f,.118f)}),lower,model);
                    CharacterPart(id+"_SariDrape",CastRibbon(new[]{V(.11f,.56f,.151f),V(.035f,.86f,.15f),V(-.07f,1.13f,.148f),V(-.14f,1.39f,.13f)},.071f),lower,model);
                    CharacterPart(id+"_SariBorder",CastRibbon(new[]{V(.18f,.56f,.153f),V(.105f,.86f,.153f),V(0,1.13f,.151f),V(-.07f,1.39f,.133f)},.009f),trim,model);
                }
                CharacterPart(id+"_Bindi",FaceOval(0,.095f,.006f,.008f,.010f),CharacterColour("Bindi","863F35"),head);
            }
            if(look.elder)
            {
                foreach(float side in new[]{-1f,1f})
                    CharacterPart(id+(side<0?"_LeftSmileLine":"_RightSmileLine"),FaceStroke(new[]{new Vector2(side*.082f,-.025f),new Vector2(side*.09f,-.060f),new Vector2(side*.081f,-.086f)},.0025f,.006f),trim,head);
            }
            if(look.glasses)
            {
                foreach(float side in new[]{-1f,1f})
                {
                    float x=side*.061f;
                    CharacterPart(id+(side<0?"_LeftGlasses":"_RightGlasses"),FaceStroke(new[]{new Vector2(x-.044f,.058f),new Vector2(x+.044f,.058f),new Vector2(x+.044f,.012f),new Vector2(x-.044f,.012f),new Vector2(x-.044f,.058f)},.0035f,.03f),ink,head);
                }
                CharacterPart(id+"_GlassesBridge",FaceStroke(new[]{new Vector2(-.016f,.043f),new Vector2(.016f,.043f)},.0035f,.03f),ink,head);
            }
            if(name=="Kencha")
            {
                var brass=CharacterColour("BadgeBrass","A39561");
                CharacterPart(id+"_UniformBelt",CharacterLoft(new[]{new CharacterRing(.92f,.20f,.128f),new CharacterRing(.955f,.20f,.128f)}),ink,model);
                CharacterPart(id+"_Badge",CastRibbon(new[]{V(-.12f,1.24f,.14f),V(-.12f,1.29f,.14f)},.021f),brass,model);
            }
            if(name=="Basavaraju")
                CharacterPart(id+"_WorkApron",CastRibbon(new[]{V(0,.62f,.145f),V(0,1.05f,.151f),V(0,1.23f,.147f)},.13f),CharacterColour("MarketApron","9C9075"),model);
            if(name=="Shresthi")
                CharacterPart(id+"_ShirtPocket",CastRibbon(new[]{V(-.12f,1.18f,.143f),V(-.12f,1.27f,.143f)},.037f),trim,model);
        }
    }
}
