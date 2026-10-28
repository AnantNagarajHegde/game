using System;
using System.Collections.Generic;
using UnityEngine;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        // Original mesh sample for two characters only, pending the user's style review.
        // Ring meshes shape the silhouette; no capsule/sphere body primitives are used.
        readonly struct CharacterRing
        {
            public readonly float y,x,z,forward;
            public CharacterRing(float y,float x,float z,float forward=0){this.y=y;this.x=x;this.z=z;this.forward=forward;}
        }
        static Mesh CharacterLoft(CharacterRing[] rings,int sides=18)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            foreach(var ring in rings)for(int s=0;s<sides;s++)
            {float a=s*Mathf.PI*2/sides;vertices.Add(V(Mathf.Sin(a)*ring.x,ring.y,Mathf.Cos(a)*ring.z+ring.forward));}
            for(int r=0;r<rings.Length-1;r++)for(int s=0;s<sides;s++)
            {
                int a=r*sides+s,b=r*sides+(s+1)%sides,c=a+sides,d=b+sides;
                triangles.AddRange(new[]{a,b,c,b,d,c});
            }
            int bottom=vertices.Count;vertices.Add(V(0,rings[0].y,rings[0].forward));
            int top=vertices.Count;vertices.Add(V(0,rings[rings.Length-1].y,rings[rings.Length-1].forward));
            for(int s=0;s<sides;s++){int next=(s+1)%sides;triangles.AddRange(new[]{bottom,next,s,top,(rings.Length-1)*sides+s,(rings.Length-1)*sides+next});}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Material CharacterColour(string name,string hex)
        {
            string key="Character_"+name;if(mats.TryGetValue(key,out var found))return found;
            var shader=Shader.Find("Paridhwani/CharacterInk");if(!shader)throw new InvalidOperationException("Character sample shader did not import");
            var material=new Material(shader);material.SetColor("_BaseColor",Hex(hex));mats[key]=Save(material,Root+"/Generated/Materials/"+key+".mat");return mats[key];
        }
        static GameObject CharacterPart(string key,Mesh mesh,Material material,Transform parent,Vector3 offset=default)
        {
            var item=new GameObject(key,typeof(MeshFilter),typeof(MeshRenderer));item.transform.SetParent(parent,false);item.transform.localPosition=offset;
            mesh.name=key;item.GetComponent<MeshFilter>().sharedMesh=Save(mesh,Root+"/Generated/Meshes/"+key+".asset");item.GetComponent<MeshRenderer>().sharedMaterial=material;return item;
        }
        static float FaceSurface(float x)=>.123f*Mathf.Sqrt(Mathf.Max(.05f,1-x*x/(.155f*.155f)));
        static Mesh FaceOval(float x,float y,float width,float height,float raised)
        {
            const int sides=20;var v=new List<Vector3>{V(x,y,FaceSurface(x)+raised+.004f)};var t=new List<int>();
            for(int s=0;s<sides;s++){float a=s*Mathf.PI*2/sides;float px=x+Mathf.Cos(a)*width;v.Add(V(px,y+Mathf.Sin(a)*height,FaceSurface(px)+raised));}
            for(int s=0;s<sides;s++)t.AddRange(new[]{0,1+s,1+(s+1)%sides});
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
        }
        static Mesh FaceStroke(Vector2[] points,float width,float raised)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<points.Length;i++)
            {
                var p=points[i];var direction=(points[Mathf.Min(points.Length-1,i+1)]-points[Mathf.Max(0,i-1)]).normalized;
                var normal=new Vector2(-direction.y,direction.x)*width*.5f;
                var a=p-normal;var b=p+normal;v.Add(V(a.x,a.y,FaceSurface(a.x)+raised));v.Add(V(b.x,b.y,FaceSurface(b.x)+raised));
            }
            for(int s=0;s<points.Length-1;s++){int a=s*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
        }
        static CharacterRing[] CharacterFaceRings(bool mature)=>new[]{new CharacterRing(-.20f,.028f,.035f,.022f),new CharacterRing(-.15f,mature?.109f:.09f,.09f,.02f),new CharacterRing(-.06f,.138f,.118f,.008f),new CharacterRing(.055f,.149f,.12f),new CharacterRing(.15f,.128f,.103f,-.009f),new CharacterRing(.21f,.055f,.051f,-.015f),new CharacterRing(.225f,.015f,.014f,-.016f)};
        static float HairHem(float angle,bool mature)
        {
            float front=Mathf.Cos(angle),side=Mathf.Sin(angle);
            float temple=mature?.035f:.01f;
            if(front<0)return Mathf.Lerp(temple,mature?-.045f:-.07f,Mathf.Pow(-front,1.4f));
            float forehead=mature?.14f+.008f*side:.12f+.012f*side-.012f*Mathf.Exp(-Mathf.Pow((side+.2f)/.4f,2));
            return Mathf.Lerp(temple,forehead,front*front);
        }
        static float HairEdgePolar(float angle,bool mature)
        {return Mathf.Acos(Mathf.Clamp((HairHem(angle,mature)-.045f)/(mature?.195f:.20f),-1,1));}
        static Vector3 HairPoint(float angle,float polar,bool mature,float lift=0)
        {
            float x=mature?.168f:.174f,z=mature?.145f:.15f,y=mature?.195f:.20f,center=.045f;
            var p=V(Mathf.Sin(angle)*x*Mathf.Sin(polar),center+y*Mathf.Cos(polar),-.012f+Mathf.Cos(angle)*z*Mathf.Sin(polar));
            var normal=V(p.x/(x*x),(p.y-center)/(y*y),(p.z+.012f)/(z*z)).normalized;
            return p+normal*lift;
        }
        static Mesh HairCrown(bool mature)
        {
            // A continuous outer shell, with a shaped hairline and a narrow solid rim.
            // The unique pole avoids coincident/degenerate crown triangles.
            const int sides=40,rows=20;var v=new List<Vector3>{HairPoint(0,0,mature)};var t=new List<int>();
            for(int row=1;row<=rows;row++)for(int s=0;s<sides;s++)
            {
                float angle=s*Mathf.PI*2/sides;
                v.Add(HairPoint(angle,HairEdgePolar(angle,mature)*row/rows,mature));
            }
            for(int s=0;s<sides;s++)t.AddRange(new[]{0,1+s,1+(s+1)%sides});
            for(int row=0;row<rows-1;row++)for(int s=0;s<sides;s++)
            {int a=1+row*sides+s,b=1+row*sides+(s+1)%sides,c=a+sides,d=b+sides;t.AddRange(new[]{a,c,b,b,c,d});}
            int rim=v.Count;
            for(int s=0;s<sides;s++)
            {
                var p=v[1+(rows-1)*sides+s];p.x*=.94f;p.z=(p.z+.012f)*.94f-.012f;p.y-=.003f;v.Add(p);
            }
            for(int s=0;s<sides;s++)
            {int a=1+(rows-1)*sides+s,b=1+(rows-1)*sides+(s+1)%sides,c=rim+s,d=rim+(s+1)%sides;t.AddRange(new[]{a,c,b,b,c,d});}
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
        }
        static Mesh HairCombing(bool mature)
        {
            var v=new List<Vector3>();var t=new List<int>();
            const int steps=14;int strands=mature?1:2;
            for(int strand=0;strand<strands;strand++)
            {
                int start=v.Count;
                for(int step=0;step<=steps;step++)
                {
                    float f=step/(float)steps,angle=Mathf.Lerp(mature?.65f:-.85f+strand*.65f,mature?.30f:-.18f+strand*.65f,f);
                    float polar=Mathf.Lerp(.34f,HairEdgePolar(angle,mature)*.91f,f);
                    float width=.004f+.016f*Mathf.Sin(f*Mathf.PI);
                    v.Add(HairPoint(angle-width,polar,mature,.0015f));v.Add(HairPoint(angle+width,polar,mature,.0015f));
                }
                for(int step=0;step<steps;step++){int a=start+step*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
        }
        static GameObject CartoonCharacter(string name,Vector3 position,bool player)
        {
            bool mature=!player;string id=player?"CartoonPlayer":"CartoonManjunatha";
            var root=new GameObject(name);root.transform.position=position;
            var model=new GameObject("Character visual").transform;model.SetParent(root.transform,false);
            var rig=model.gameObject.AddComponent<VillageCharacter>();
            var skin=CharacterColour(player?"PlayerSkin":"VendorSkin",player?"B17B56":"A9704F");
            var shirt=CharacterColour(player?"ForestShirt":"CottonShirt",player?"526D5C":"C6BEA3");
            var trousers=CharacterColour("CharcoalCloth","394441");var hair=CharacterColour("Hair","262521");
            var hairLight=CharacterColour("HairLight","3C3830");var ink=CharacterColour("FaceInk","332825");
            var white=CharacterColour("EyeWhite","E5DDD0");var iris=CharacterColour("Iris","704D32");var light=CharacterColour("EyeGlint","FFF2D5");
            var trim=CharacterColour("CottonTrim",player?"718272":"A99E7B");
            CharacterPart(id+"_Shirt",CharacterLoft(new[]{new CharacterRing(.85f,.188f,.114f),new CharacterRing(.94f,.197f,.124f),new CharacterRing(1.12f,.202f,.127f),new CharacterRing(1.30f,.235f,.13f),new CharacterRing(1.40f,.239f,.108f),new CharacterRing(1.445f,.09f,.075f)}),shirt,model);
            CharacterPart(id+"_Waist",CharacterLoft(new[]{new CharacterRing(.76f,.168f,.107f),new CharacterRing(.88f,.187f,.12f),new CharacterRing(.94f,.184f,.112f)}),trousers,model);
            CharacterPart(id+"_Neck",CharacterLoft(new[]{new CharacterRing(1.40f,.067f,.064f),new CharacterRing(1.52f,.064f,.06f)}),skin,model);
            // Collar and placket follow the shaped shirt, rather than a box torso.
            CharacterPart(id+"_Placket",FaceStroke(new[]{new Vector2(0,1.39f),new Vector2(0,1.28f),new Vector2(0,1.06f)},.013f,.009f),trim,model);
            foreach(float side in new[]{-1f,1f})
            {
                string suffix=side<0?"Left":"Right";
                var arm=new GameObject(suffix+" arm pivot").transform;arm.SetParent(model,false);arm.localPosition=V(side*.235f,1.395f,0);
                CharacterPart(id+"_"+suffix+"Sleeve",CharacterLoft(new[]{new CharacterRing(-.245f,.073f,.074f),new CharacterRing(-.19f,.086f,.083f),new CharacterRing(-.075f,.089f,.087f),new CharacterRing(.014f,.055f,.061f)}),shirt,arm);
                CharacterPart(id+"_"+suffix+"Forearm",CharacterLoft(new[]{new CharacterRing(-.50f,.041f,.039f,.025f),new CharacterRing(-.43f,.049f,.044f,.014f),new CharacterRing(-.31f,.062f,.055f,.009f),new CharacterRing(-.22f,.062f,.06f)}),skin,arm);
                CharacterPart(id+"_"+suffix+"Hand",CharacterLoft(new[]{new CharacterRing(-.603f,.029f,.021f,.027f),new CharacterRing(-.57f,.048f,.026f,.03f),new CharacterRing(-.51f,.045f,.032f,.026f),new CharacterRing(-.48f,.035f,.032f,.025f)},12),skin,arm);
                var leg=new GameObject(suffix+" leg pivot").transform;leg.SetParent(model,false);leg.localPosition=V(side*.10f,.83f,0);
                CharacterPart(id+"_"+suffix+"Trouser",CharacterLoft(new[]{new CharacterRing(-.715f,.059f,.066f,.008f),new CharacterRing(-.56f,.064f,.066f),new CharacterRing(-.36f,.073f,.078f,.006f),new CharacterRing(-.13f,.084f,.093f),new CharacterRing(.04f,.083f,.092f)}),trousers,leg);
                CharacterPart(id+"_"+suffix+"Shoe",CharacterLoft(new[]{new CharacterRing(-.827f,.071f,.134f,.044f),new CharacterRing(-.794f,.078f,.151f,.055f),new CharacterRing(-.742f,.067f,.125f,.035f),new CharacterRing(-.69f,.057f,.067f)}),hair,leg);
                if(side<0){rig.leftArm=arm;rig.leftLeg=leg;}else{rig.rightArm=arm;rig.rightLeg=leg;}
            }
            var head=new GameObject("Head").transform;head.SetParent(model,false);head.localPosition=V(0,1.66f,0);head.localScale=Vector3.one*.85f;rig.head=head;
            CharacterPart(id+"_Face",CharacterLoft(CharacterFaceRings(mature),28),skin,head);
            foreach(float side in new[]{-1f,1f})
            {
                string suffix=side<0?"Left":"Right";float x=side*.061f;
                CharacterPart(id+"_"+suffix+"Ear",CharacterLoft(new[]{new CharacterRing(-.055f,.012f,.015f),new CharacterRing(-.015f,.026f,.027f),new CharacterRing(.029f,.016f,.021f)},12),skin,head,V(side*.144f,0,-.003f));
                CharacterPart(id+"_"+suffix+"Eye",FaceOval(x,.035f,.041f,mature?.021f:.027f,.006f),white,head);
                CharacterPart(id+"_"+suffix+"Iris",FaceOval(x,.035f,.016f,mature?.018f:.023f,.012f),iris,head);
                CharacterPart(id+"_"+suffix+"Pupil",FaceOval(x,.035f,.0075f,.014f,.019f),ink,head);
                CharacterPart(id+"_"+suffix+"Glint",FaceOval(x-.005f,.043f,.004f,.005f,.026f),light,head);
                CharacterPart(id+"_"+suffix+"Lid",FaceStroke(new[]{new Vector2(x-.039f,.038f),new Vector2(x-.02f,mature?.057f:.060f),new Vector2(x+.018f,mature?.057f:.060f),new Vector2(x+.039f,.038f)},.0045f,.013f),ink,head);
                CharacterPart(id+"_"+suffix+"Brow",FaceStroke(new[]{new Vector2(x-.036f,.087f),new Vector2(x,.098f),new Vector2(x+.034f,.089f)},mature?.010f:.007f,.009f),hair,head);
            }
            var nose=new Mesh();nose.vertices=new[]{V(-.016f,-.039f,.12f),V(.016f,-.039f,.12f),V(0,.024f,.122f),V(0,-.025f,.16f)};nose.triangles=new[]{0,3,2,3,1,2,0,1,3};nose.RecalculateNormals();
            CharacterPart(id+"_Nose",nose,skin,head);
            CharacterPart(id+"_Mouth",FaceStroke(new[]{new Vector2(-.030f,-.111f),new Vector2(0,-.116f),new Vector2(.030f,-.111f)},.004f,.006f),ink,head);
            CharacterPart(id+"_HairCrown",HairCrown(mature),hair,head);
            CharacterPart(id+"_HairCombing",HairCombing(mature),hairLight,head);
            if(mature)
            {
                CharacterPart(id+"_Moustache",FaceStroke(new[]{new Vector2(-.058f,-.082f),new Vector2(-.025f,-.070f),new Vector2(0,-.078f),new Vector2(.025f,-.070f),new Vector2(.058f,-.082f)},.014f,.012f),hair,head);
                Box("Shoulder towel",V(-.145f,1.24f,.139f),V(.10f,.47f,.025f),"Cream",false,model);
            }
            else
            {
                Box("Canvas shoulder bag",V(-.255f,1.02f,-.015f),V(.17f,.30f,.24f),"WoodLight",false,model);
                Beam("Bag strap",V(-.26f,1.04f,.155f),V(.17f,1.43f,.15f),.015f,"Wood",model);
            }
            if(!player){var body=root.AddComponent<CapsuleCollider>();body.height=1.8f;body.center=V(0,.9f,0);body.radius=.28f;}
            SetLayer(root,player?2:0);return root;
        }
    }
}
