// Hayal Garajı çayırı (URP): merkezde canlı yeşil, ufka doğru açılan yumuşak geçiş, hafif çimen benekleri.
// Gece/gündüz geçişi gökyüzüyle aynı _Night değeriyle yapılır. Işıktan bağımsızdır (masal kitabı düzlüğü).
Shader "HayalGaraji/Ground"
{
    Properties
    {
        _InnerDay("Gündüz iç", Color) = (0.55,0.85,0.58,1)
        _OuterDay("Gündüz dış", Color) = (0.72,0.91,0.74,1)
        _InnerNight("Gece iç", Color) = (0.16,0.3,0.36,1)
        _OuterNight("Gece dış", Color) = (0.2,0.24,0.42,1)
        _Radius("Geçiş (iç, dış)", Vector) = (6, 70, 0, 0)
        _Night("Gece (0-1)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _InnerDay, _OuterDay, _InnerNight, _OuterNight, _Radius;
            float _Night;
        CBUFFER_END
        ENDHLSL
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float d = length(i.positionWS.xz);
                float t = smoothstep(_Radius.x, _Radius.y, d);
                float3 day = lerp(_InnerDay.rgb, _OuterDay.rgb, t);
                float3 night = lerp(_InnerNight.rgb, _OuterNight.rgb, t);
                float3 col = lerp(day, night, _Night);
                float2 p = i.positionWS.xz * 0.45;
                float n = frac(sin(dot(floor(p), float2(12.9898, 78.233))) * 43758.5453);
                float tuft = smoothstep(0.34, 0.18, length(frac(p) - 0.5)) * step(0.55, n) * (1.0 - t);
                col *= 1.0 - tuft * 0.08;
                return half4(col, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes i) { Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); return o; }
            half frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}
