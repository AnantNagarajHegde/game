Shader "Paridhwani/Overcast Sky"
{
    Properties
    {
        _Horizon("Horizon",Color)=(.55,.61,.59,1)
        _Zenith("Cloud ceiling",Color)=(.27,.34,.36,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _Horizon,_Zenith;
            struct A {float4 positionOS:POSITION;};
            struct V {float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
            V vert(A a) {V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.direction=a.positionOS.xyz;return o;}
            half4 frag(V i):SV_Target
            {
                float3 d=normalize(i.direction);
                float h=smoothstep(-.08,.85,d.y);
                float clouds=sin(d.x*8+d.z*5)*sin(d.z*11-d.x*3)*.022;
                return half4(saturate(lerp(_Horizon.rgb,_Zenith.rgb,h)+clouds*h),1);
            }
            ENDHLSL
        }
    }
}
