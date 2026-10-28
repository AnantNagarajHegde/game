Shader "Paridhwani/CharacterInk"
{
    Properties { _BaseColor("Pigment",Color)=(.6,.4,.25,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "CharacterColour"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END
            struct A { float4 positionOS:POSITION;float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;half fog:TEXCOORD2; };
            V vert(A a)
            {
                V o;VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;
                o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.fog=ComputeFogFactor(p.positionCS.z);return o;
            }
            half4 frag(V i):SV_Target
            {
                half3 n=normalize(i.normalWS);Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half band=lerp(.40,.95,smoothstep(.25,.52,dot(n,sun.direction)));
                half3 ambient=max(SampleSH(n),half3(.12,.13,.13));
                half3 colour=_BaseColor.rgb*(.24+ambient*.65+sun.color*band*.72*lerp(.65,1,sun.shadowAttenuation));
                return half4(MixFog(colour,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "CharacterShadow"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float3 _LightDirection;
            struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            float4 shadowVert(A a):SV_POSITION
            {
                float3 p=TransformObjectToWorld(a.positionOS.xyz);float3 n=TransformObjectToWorldNormal(a.normalOS);
                float4 clip=TransformWorldToHClip(ApplyShadowBias(p,n,_LightDirection));
                #if UNITY_REVERSED_Z
                clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #else
                clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #endif
                return clip;
            }
            half4 shadowFrag():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
