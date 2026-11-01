Shader "Paridhwani/Village Surface"
{
    Properties
    {
        _BaseColor("Pigment",Color)=(.5,.4,.3,1)
        _BaseMap("Base",2D)="white"{}
        _Roughness("Roughness",Range(0,1))=.8
        _Wet("Wetness",Range(0,1))=0
        _Grain("Grain scale",Float)=12
        _TextureStrength("Surface texture",Range(0,1))=0
        _TextureScale("World texture scale",Float)=1
        _WindStrength("Foliage wind",Float)=0
        _VertexSurface("Vertex pigment and wetness",Range(0,1))=0
        _Relief("Material relief",Range(0,.12))=0
        _Weathering("Wall weathering",Range(0,1))=0
        _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode"="UniversalForward"}
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float4 _BaseMap_ST;
                float _Roughness,_Wet,_Grain,_Cull,_TextureStrength,_TextureScale,_WindStrength,_VertexSurface,_Relief,_Weathering;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float _VillageWindScale, _VillageRainAmount;
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float4 color:COLOR; float fog:TEXCOORD2; half3 vertexLight:TEXCOORD3; };
            float hash(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
            float noise(float3 p) {
                float3 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),
                    lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),
                    lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);
            }
            V vert(A a) {
                V o; o.positionWS=TransformObjectToWorld(a.positionOS.xyz);
                o.positionWS.x+=sin(_Time.y*.75+o.positionWS.z*.2+o.positionWS.x*.3)*_WindStrength*_VillageWindScale;
                o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(a.normalOS); o.color=a.color;
                o.fog=ComputeFogFactor(o.positionCS.z);
                o.vertexLight=VertexLighting(o.positionWS,normalize(o.normalWS)); return o;
            }
            half4 frag(V i,bool frontFace:SV_IsFrontFace):SV_Target {
                float3 p=i.positionWS;
                float broad=noise(p*1.8), fine=noise(p*_Grain);
                float wear=noise(p*.32);
                float variation=.79+.26*broad+.11*fine;
                float damp=_Wet*lerp(.55,1,saturate(_VillageRainAmount))*lerp(1,saturate(i.color.a),_VertexSurface)*smoothstep(.3,.8,wear);
                SurfaceData surface=(SurfaceData)0;
                float3 weights=pow(abs(normalize(i.normalWS)),4);weights/=max(dot(weights,float3(1,1,1)),.0001);
                float3 tex=float3(1,1,1);
                if(_TextureStrength>.001) tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.yz*_TextureScale).rgb*weights.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz*_TextureScale).rgb*weights.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy*_TextureScale).rgb*weights.z;
                surface.albedo=_BaseColor.rgb*variation*lerp(float3(1,1,1),tex*1.18,_TextureStrength)*lerp(1,.72,damp)*lerp(float3(1,1,1),i.color.rgb,_VertexSurface);
                float wall=1-saturate(abs(i.normalWS.y));
                float tide=(1-smoothstep(.18,1.05,p.y))*lerp(.35,1,noise(p*float3(3,.18,3)));
                float streak=noise(p*float3(5,.13,5))*.07;
                surface.albedo*=1-_Weathering*wall*(tide*.22+streak);
                surface.metallic=0; surface.specular=half3(.04,.04,.04);
                surface.smoothness=min(.45,saturate(1-_Roughness+damp*.22+(fine-.5)*_Relief));
                surface.normalTS=half3(0,0,1); surface.occlusion=1; surface.alpha=1;
                InputData input=(InputData)0;
                input.positionWS=p; input.normalWS=normalize(i.normalWS)*(frontFace?1:-1);
                // Screen derivatives of world-space height give stable, scale-aware
                // relief without UV seams or extra texture reads on every surface.
                if(_Relief>.0001)
                {
                    float height=broad*.55+fine*.12+dot(tex,float3(.2126,.7152,.0722))*.33;
                    float3 dx=ddx(p),dy=ddy(p),r1=cross(dy,input.normalWS),r2=cross(input.normalWS,dx);
                    float determinant=dot(dx,r1);
                    float3 gradient=sign(determinant)*(ddx(height)*r1+ddy(height)*r2);
                    input.normalWS=normalize(abs(determinant)*input.normalWS-gradient*_Relief+input.normalWS*1e-10);
                }
                input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(p);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    input.shadowCoord=ComputeScreenPos(TransformWorldToHClip(p));
                #else
                    input.shadowCoord=TransformWorldToShadowCoord(p);
                #endif
                input.bakedGI=SampleSH(input.normalWS);
                input.vertexLighting=i.vertexLight;
                input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                input.shadowMask=half4(1,1,1,1);
                half4 col=UniversalFragmentPBR(input,surface);
                col.rgb=MixFog(col.rgb,i.fog); return col;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
