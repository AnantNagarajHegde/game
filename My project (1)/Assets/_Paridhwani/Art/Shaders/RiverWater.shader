Shader "Paridhwani/River Water"
{
    Properties
    {
        _BaseColor("Deep monsoon water", Color)=(.16,.30,.28,1)
        _ShallowColor("Shallow bank water", Color)=(.34,.49,.44,1)
        _FoamColor("Aerated water", Color)=(.75,.81,.74,1)
        _ReflectionColor("Overcast sky reflection", Color)=(.58,.71,.70,1)
        _FlowSpeed("Downstream current", Range(0,1))=.38
        _RippleStrength("Ripple normals", Range(0,1))=.32
        _Smoothness("Water smoothness", Range(0,1))=.83
        _Waterfall("Vertical cascade", Float)=0
    }
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _ShallowColor, _FoamColor, _ReflectionColor;
                float _FlowSpeed, _RippleStrength, _Smoothness, _Waterfall;
            CBUFFER_END
            float _VillageWindScale, _VillageRainAmount;
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float fog:TEXCOORD2; float2 uv:TEXCOORD3; };
            float hash21(float2 p) { p=frac(p*float2(123.34,456.21)); p+=dot(p,p+45.32); return frac(p.x*p.y); }
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+1),f.x),f.y);
            }
            float ripple(float2 p, float t)
            {
                float2 flow=p+float2(.08,t*_FlowSpeed);
                float swell=noise(flow*float2(.72,1.8));
                return swell*.65 + noise(flow*float2(2.3,4.1)+float2(t*.09,-t*.12)+swell*.7)*.25
                    + sin(p.x*3.2+p.y*5.3+t*.75+noise(p*.5)*5)*.045;
            }
            V vert(A a)
            {
                V o; o.positionWS=TransformObjectToWorld(a.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(a.normalOS); o.fog=ComputeFogFactor(o.positionCS.z); o.uv=a.uv; return o;
            }
            half4 frag(V i):SV_Target
            {
                float3 p=i.positionWS;
                float t=_Time.y*saturate(_VillageWindScale);
                float2 uv=lerp(p.xz,float2(p.x,p.y*2),_Waterfall);
                float h=ripple(uv,t),hx=ripple(uv+float2(.06,0),t),hz=ripple(uv+float2(0,.06),t);
                float3 n=normalize(i.normalWS+float3((h-hx)*_RippleStrength*5,0,(h-hz)*_RippleStrength*5));
                float bank=smoothstep(.28,.035,min(i.uv.x,1-i.uv.x));
                float flowNoise=noise(uv*float2(3.2,.85)+float2(h,t*_FlowSpeed));
                float fineFoam=noise(uv*float2(8,5)+float2(t*.15,t*.6));
                float shore=bank*smoothstep(.55,.8,flowNoise)*smoothstep(.24,.6,fineFoam)*.42;
                float turbulence=exp(-abs(p.z-1.8)*1.4)*(1-_Waterfall);
                float foam=shore+turbulence*smoothstep(.40,.72,flowNoise)*(.35+fineFoam*.35);
                float cascade=noise(float2(p.x*12,p.y*4+t*2.1));
                foam=lerp(foam,.18+cascade*.38,_Waterfall);
                // Sparse soft rain rings, advected with the current.
                float2 cell=floor(uv*1.25),local=frac(uv*1.25)-.5;
                float age=frac(t*.65+hash21(cell));
                float ring=exp(-abs(length(local)-age*.52)*65)*saturate(1-age)*.075*saturate(_VillageRainAmount);
                float3 water=lerp(_BaseColor.rgb,_ShallowColor.rgb,bank*.65+noise(uv*.22)*.12);
                water*=.93+h*.14;
                float3 view=GetWorldSpaceNormalizeViewDir(p);
                float fresnel=pow(1-saturate(dot(n,view)),4);
                water=lerp(water,_ReflectionColor.rgb,fresnel*.46);
                water=lerp(water,_FoamColor.rgb,saturate(foam+ring*(1-_Waterfall)));
                SurfaceData s=(SurfaceData)0;
                s.albedo=water; s.normalTS=half3(0,0,1); s.smoothness=lerp(_Smoothness,.28,saturate(foam)); s.occlusion=1; s.alpha=1;
                s.specular=half3(.04,.04,.04);
                InputData d=(InputData)0; d.positionWS=p; d.normalWS=n;
                d.viewDirectionWS=view; d.shadowCoord=TransformWorldToShadowCoord(p); d.bakedGI=SampleSH(n); d.shadowMask=1;
                half4 col=UniversalFragmentPBR(d,s); col.rgb=MixFog(col.rgb,i.fog); return col;
            }
            ENDHLSL
        }
    }
}
