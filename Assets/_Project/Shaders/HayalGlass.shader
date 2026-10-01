// Hayal Garajı çizgi film camı (URP): yarı saydam, kenarda parlayan, keskin ışık lekeli, dış çizgili.
Shader "HayalGaraji/Glass"
{
    Properties
    {
        _BaseColor("Cam rengi (alfa = saydamlık)", Color) = (0.72,0.9,1,0.28)
        _EdgeColor("Kenar parlaması", Color) = (1,1,1,0.85)
        _FresnelPower("Kenar keskinliği", Range(0.5,6)) = 2.4
        _GlossSize("Parlama boyutu", Range(0,0.3)) = 0.03
        _OutlineColor("Çizgi rengi", Color) = (0.17,0.14,0.31,1)
        _OutlinePx("Çizgi kalınlığı (piksel)", Range(0,6)) = 2.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _EdgeColor;
            float  _FresnelPower;
            float  _GlossSize;
            float4 _OutlineColor;
            float  _OutlinePx;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            // Ekranda sabit piksel kalınlığında çizgi: uzakta kesik kesik olmaz
            Varyings vert(Attributes i)
            {
                Varyings o;
                float4 pos = TransformObjectToHClip(i.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(i.normalOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float len = max(length(nCS), 1e-5);
                float px = _OutlinePx * (_ScreenParams.y / 1080.0);
                pos.xy += nCS / len * (px * 2.0 / _ScreenParams.xy) * pos.w;
                o.positionCS = pos;
                return o;
            }
            half4 frag(Varyings i) : SV_Target { return half4(_OutlineColor.rgb, 1); }
            ENDHLSL
        }

        Pass
        {
            Name "GlassForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; };
            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float nv = abs(dot(n, v));
                float fres = pow(1.0 - nv, _FresnelPower);
                Light L = GetMainLight();
                float3 h = normalize(L.direction + v);
                float g = smoothstep(1.0 - _GlossSize - 0.012, 1.0 - _GlossSize + 0.012, abs(dot(n, h)));
                float3 col = lerp(_BaseColor.rgb, _EdgeColor.rgb, fres) + g;
                float a = saturate(_BaseColor.a + fres * _EdgeColor.a + g);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
