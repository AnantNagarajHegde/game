using System;
using System.Collections.Generic;
using UnityEngine;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        static float TreeRange(System.Random rng,float min,float max)=>Mathf.Lerp(min,max,(float)rng.NextDouble());
        static void TreeTube(SurfaceGeometry geometry,Vector3[] path,float[] radius,int sides,System.Random rng)
        {
            int start=geometry.vertices.Count;
            for(int row=0;row<path.Length;row++)
            {
                var tangent=(path[Mathf.Min(row+1,path.Length-1)]-path[Mathf.Max(0,row-1)]).normalized;
                var a=Vector3.Cross(tangent,Mathf.Abs(tangent.z)<.9f?Vector3.forward:Vector3.right).normalized;var b=Vector3.Cross(tangent,a);
                float tint=TreeRange(rng,.85f,1.07f);
                for(int side=0;side<sides;side++)
                {
                    float angle=side*Mathf.PI*2/sides,r=radius[row]*(1+.035f*Mathf.Sin(side*2.7f));
                    geometry.Point(path[row]+(a*Mathf.Cos(angle)+b*Mathf.Sin(angle))*r,new Color(tint,tint,tint*.97f,.5f));
                }
            }
            for(int row=0;row<path.Length-1;row++)for(int side=0;side<sides;side++)
            {int a=start+row*sides+side,b=start+row*sides+(side+1)%sides,c=a+sides,d=b+sides;geometry.Triangle(a,b,c);geometry.Triangle(b,d,c);}
            int bottom=geometry.Point(path[0],Color.white),top=geometry.Point(path[path.Length-1],Color.white);
            for(int side=0;side<sides;side++)
            {geometry.Triangle(bottom,start+(side+1)%sides,start+side);geometry.Triangle(top,start+(path.Length-1)*sides+side,start+(path.Length-1)*sides+(side+1)%sides);}
        }
        static void TreeCrown(SurfaceGeometry geometry,Vector3 center,Vector3 radius,System.Random rng)
        {
            const int sides=22,rows=9;float phase=TreeRange(rng,0,6.28f);int top=geometry.Point(center+Vector3.up*radius.y,new Color(1.12f,1.12f,1.08f,.2f));
            for(int row=1;row<rows;row++)for(int side=0;side<sides;side++)
            {
                float polar=row*Mathf.PI/rows,angle=side*Mathf.PI*2/sides;
                float shape=1+.095f*Mathf.Sin(angle*3+phase)+.055f*Mathf.Cos(angle*7-polar*2+phase);
                float y=Mathf.Cos(polar)*radius.y+.09f*Mathf.Sin(angle*4+phase)*Mathf.Sin(polar);
                float tint=Mathf.Lerp(1.08f,.66f,row/(float)rows)+.08f*Mathf.Sin(angle*5+phase);
                geometry.Point(center+V(Mathf.Sin(angle)*Mathf.Sin(polar)*radius.x*shape,y,Mathf.Cos(angle)*Mathf.Sin(polar)*radius.z*shape),new Color(tint,tint,tint*.97f,.3f));
            }
            for(int side=0;side<sides;side++)geometry.Triangle(top,top+1+side,top+1+(side+1)%sides);
            for(int row=0;row<rows-2;row++)for(int side=0;side<sides;side++)
            {int a=top+1+row*sides+side,b=top+1+row*sides+(side+1)%sides,c=a+sides,d=b+sides;geometry.Triangle(a,c,b);geometry.Triangle(b,c,d);}
            int bottom=geometry.Point(center-Vector3.up*radius.y,new Color(.68f,.68f,.66f,.3f));
            for(int side=0;side<sides;side++)geometry.Triangle(bottom,top+1+(rows-2)*sides+(side+1)%sides,top+1+(rows-2)*sides+side);
        }
        static void PalmBlade(SurfaceGeometry geometry,Vector3 root,Vector3 tip,float width,float tint)
        {
            var side=Vector3.Cross(tip-root,Vector3.up).normalized*width;var mid=Vector3.Lerp(root,tip,.46f);
            var color=new Color(tint,tint,tint*.95f,.25f);int a=geometry.Point(root,color);
            geometry.Point(mid-side,color);geometry.Point(mid+side,color);geometry.Point(mid+Vector3.up*.025f,color);geometry.Point(tip,color);
            geometry.Triangle(a,a+3,a+1);geometry.Triangle(a,a+2,a+3);geometry.Triangle(a+1,a+3,a+4);geometry.Triangle(a+3,a+2,a+4);
        }
        static Vector3 PalmSpine(Vector3 top,Vector3 direction,float length,float arch,float drop,float t)
            =>top+direction*(length*t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*arch-t*t*drop);
        static void FeatheredPalm(SurfaceGeometry leaves,Vector3 top,int id,System.Random rng)
        {
            int count=10+id%3;float phase=TreeRange(rng,0,6.28f);
            for(int frond=0;frond<count;frond++)
            {
                float angle=phase+frond*Mathf.PI*2/count+TreeRange(rng,-.1f,.1f);
                var direction=V(Mathf.Cos(angle),0,Mathf.Sin(angle));var side=Vector3.Cross(direction,Vector3.up);
                float length=TreeRange(rng,1.9f,2.7f),arch=TreeRange(rng,.35f,.65f),drop=TreeRange(rng,.55f,1.10f);
                if(frond<2){length*=.65f;arch=1.05f;drop=.2f;}
                int start=leaves.vertices.Count;
                for(int step=0;step<=12;step++)
                {
                    float t=step/12f,w=Mathf.Lerp(.016f,.004f,t);var p=PalmSpine(top,direction,length,arch,drop,t);
                    leaves.Point(p-side*w,new Color(.85f,.85f,.82f,.25f));leaves.Point(p+side*w,new Color(.85f,.85f,.82f,.25f));
                }
                for(int step=0;step<12;step++){int a=start+step*2;leaves.Triangle(a,a+2,a+1);leaves.Triangle(a+1,a+2,a+3);}
                for(int step=0;step<12;step++)
                {
                    float t=.12f+step*.069f;var p=PalmSpine(top,direction,length,arch,drop,t);
                    float blade=length*(.105f+.19f*Mathf.Sin(t*Mathf.PI)),tint=TreeRange(rng,.80f,1.18f);
                    foreach(float sign in new[]{-1f,1f})
                        PalmBlade(leaves,p,p+(side*sign*.85f+direction*.4f+Vector3.down*(.10f+t*.2f))*blade,.032f+.018f*Mathf.Sin(t*Mathf.PI),tint);
                }
            }
        }
        static Material TreeMaterial(string name,string source,string color)
        {
            var material=new Material(mats[source]);material.SetColor("_BaseColor",Hex(color));material.SetFloat("_VertexSurface",1);
            return Save(material,Root+"/Generated/Materials/"+name+".mat");
        }
        static void VillageVegetationArt()
        {
            var parent=new GameObject("Village Vegetation Art").transform;parent.SetParent(environment,false);
            var rng=new System.Random(28031976);var wood=new SurfaceGeometry();var leaves=new SurfaceGeometry();
            var bark=TreeMaterial("VillageTreeBark","Trunk","69634C");var canopy=TreeMaterial("VillageBanyanLeaf","Leaf","4B5B3C");
            var palmLeaf=TreeMaterial("VillagePalmLeaf","LeafLight","526547");
            foreach(var renderer in environment.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.name=="Heritage banyan trunk" || renderer.name=="Banyan branch" || renderer.name=="Banyan canopy" || renderer.name=="Banyan aerial root")renderer.enabled=false;
            }
            var basePoint=V(-9,0,9.4f);
            TreeTube(wood,new[]{basePoint,basePoint+V(.02f,1.3f,.01f),basePoint+V(-.03f,2.6f,.02f),basePoint+V(.03f,3.5f,.03f),basePoint+V(0,4.4f,0)},new[]{.35f,.31f,.26f,.18f,.055f},12,rng);
            TreeCrown(leaves,basePoint+V(0,4.85f,0),V(1.45f,.78f,1.35f),rng);
            for(int branch=0;branch<7;branch++)
            {
                float angle=branch*Mathf.PI*2/7+TreeRange(rng,-.13f,.13f);var direction=V(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var side=Vector3.Cross(direction,Vector3.up);float reach=TreeRange(rng,1.65f,2.1f);
                var tip=basePoint+direction*reach+Vector3.up*TreeRange(rng,4.0f,4.35f);
                TreeTube(wood,new[]{basePoint+V(0,2.65f,0),basePoint+direction*.72f+Vector3.up*3.45f,tip},new[]{.15f,.09f,.025f},9,rng);
                TreeCrown(leaves,tip+Vector3.up*.25f,V(TreeRange(rng,1.05f,1.35f),TreeRange(rng,.6f,.85f),TreeRange(rng,1.0f,1.35f)),rng);
                TreeCrown(leaves,tip+side*.65f-direction*.2f,V(.87f,.53f,.84f),rng);
                // Small broad leaves interrupt the volume silhouette at the outer
                // twigs; this keeps the hero crown from reading as smooth boulders.
                for(int leaf=0;leaf<14;leaf++)
                {
                    float a=leaf*Mathf.PI*2/14;var outward=V(Mathf.Sin(a),0,Mathf.Cos(a));
                    var root=tip+outward*TreeRange(rng,1.0f,1.18f)+Vector3.up*TreeRange(rng,.02f,.38f);
                    PalmBlade(leaves,root,root+outward*.26f+Vector3.down*.08f,.065f,TreeRange(rng,.88f,1.12f));
                }
            }
            foreach(float x in new[]{-9.5f,-8.1f})TreeTube(wood,new[]{V(x,0,9.6f),V(x+.035f,1.3f,9.57f),V(x-.02f,2.6f,9.62f),V(x,3.7f,9.6f)},new[]{.028f,.022f,.019f,.024f},7,rng);
            FrontageMesh("HeroBanyanWood",wood,bark,parent);FrontageMesh("HeroBanyanCanopy",leaves,canopy,parent);
            var stems=new List<MeshRenderer>();var crowns=new List<MeshRenderer>();
            foreach(var renderer in environment.GetComponentsInChildren<MeshRenderer>())
            {if(renderer.name=="Areca palm trunk")stems.Add(renderer);if(renderer.name=="Areca crown")crowns.Add(renderer);}
            var palmWood=new SurfaceGeometry();var palmLeaves=new SurfaceGeometry();
            foreach(var crown in crowns)
            {
                var mesh=crown.GetComponent<MeshFilter>().sharedMesh;var points=mesh.vertices;var top=(points[0]+points[1])*.5f;
                MeshRenderer trunk=null;
                foreach(var stem in stems)if(Vector3.Distance(stem.transform.position+stem.transform.up*stem.transform.lossyScale.y,top)<.01f){trunk=stem;break;}
                if(!trunk)throw new InvalidOperationException("Cannot match original palm stem: "+mesh.name);
                var bottom=trunk.transform.position-trunk.transform.up*trunk.transform.lossyScale.y;
                if(bottom.z>14)continue; // Leave the northern/background palms and distant canopy for a later batch.
                int id=int.Parse(mesh.name.Substring("Palm_".Length));crown.enabled=false;trunk.enabled=false;
                var path=new Vector3[29];var radius=new float[29];float bend=TreeRange(rng,-.16f,.16f),width=TreeRange(rng,.105f,.14f);
                for(int row=0;row<path.Length;row++)
                {float t=row/(float)(path.Length-1);path[row]=Vector3.Lerp(bottom,top,t)+V(Mathf.Sin(t*Mathf.PI)*bend,0,Mathf.Sin(t*Mathf.PI)*bend*.55f);radius[row]=Mathf.Lerp(width,.058f,t)*(row%2==0?1:.955f);}
                TreeTube(palmWood,path,radius,9,rng);FeatheredPalm(palmLeaves,top,id,rng);
            }
            FrontageMesh("NearVillagePalmWood",palmWood,bark,parent);FrontageMesh("NearVillagePalmLeaves",palmLeaves,palmLeaf,parent);
        }
    }
}
