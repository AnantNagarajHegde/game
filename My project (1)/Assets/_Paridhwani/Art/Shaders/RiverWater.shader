Shader "Paridhwani/River Water"
{
    Properties { _BaseColor("River sediment",Color)=(.18,.25,.23,1) _FoamColor("Foam",Color)=(.48,.53,.46,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor,_FoamColor;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float fog:TEXCOORD2; };
            V vert(A a)
            {
                V o; o.positionWS=TransformObjectToWorld(a.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(a.normalOS); o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 frag(V i):SV_Target
            {
                float3 p=i.positionWS;
                float t=_Time.y;
                float2 uv=abs(i.normalWS.y)>.5?p.xz:float2(p.x,p.y*2);
                float waves=sin(uv.x*7+uv.y*12-t*3)*.5+sin(uv.x*15-uv.y*5+t*2)*.25;
                float foam=smoothstep(.48,.72,waves)*.35;
                foam+=exp(-abs(p.z-2.05)*7)*(.2+.2*sin(p.x*10+t*4));
                SurfaceData s=(SurfaceData)0;
                s.albedo=lerp(_BaseColor.rgb*(.82+waves*.12),_FoamColor.rgb,saturate(foam));
                s.normalTS=half3(0,0,1); s.smoothness=.28; s.occlusion=1; s.alpha=1;
                InputData d=(InputData)0; d.positionWS=p;
                d.normalWS=normalize(i.normalWS+float3(sin(uv.y*11+t*3)*.045,0,cos(uv.x*9-t*2)*.045));
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(p); d.shadowCoord=TransformWorldToShadowCoord(p);
                d.bakedGI=SampleSH(d.normalWS); d.shadowMask=1;
                half4 col=UniversalFragmentPBR(d,s); col.rgb=MixFog(col.rgb,i.fog); return col;
            }
            ENDHLSL
        }
    }
}
