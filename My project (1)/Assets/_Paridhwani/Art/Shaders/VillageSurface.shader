Shader "Paridhwani/Village Surface"
{
    Properties
    {
        _BaseColor("Pigment",Color)=(.5,.4,.3,1)
        _BaseMap("Base",2D)="white"{}
        _Roughness("Roughness",Range(0,1))=.8
        _Wet("Wetness",Range(0,1))=0
        _Grain("Grain scale",Float)=12
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float4 _BaseMap_ST;
                float _Roughness,_Wet,_Grain,_Cull;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float4 color:COLOR; float fog:TEXCOORD2; };
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
                o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(a.normalOS); o.color=a.color;
                o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 frag(V i,bool frontFace:SV_IsFrontFace):SV_Target {
                float3 p=i.positionWS;
                float broad=noise(p*1.8), fine=noise(p*_Grain);
                float wear=noise(p*.32);
                float variation=.79+.26*broad+.11*fine;
                float damp=_Wet*smoothstep(.3,.8,wear);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=_BaseColor.rgb*variation*lerp(1,.72,damp);
                surface.metallic=0; surface.specular=half3(.04,.04,.04);
                surface.smoothness=min(.45,saturate(1-_Roughness+damp*.22));
                surface.normalTS=half3(0,0,1); surface.occlusion=1; surface.alpha=1;
                InputData input=(InputData)0;
                input.positionWS=p; input.normalWS=normalize(i.normalWS)*(frontFace?1:-1);
                input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(p);
                input.shadowCoord=TransformWorldToShadowCoord(p);
                input.bakedGI=SampleSH(input.normalWS);
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
