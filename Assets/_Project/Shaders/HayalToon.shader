// Hayal Garajı çizgi film shader'ı (URP).
// - İki tonlu yumuşak gölge + hafif kenar ışığı
// - Dış çizgi (ters kabuk yöntemi, aynı shader içinde ikinci pass)
// - Parmakla boyama katmanı (_PaintTex, UV0)
// - Kaplamalar: 0 düz, 1 gökkuşağı, 2 iki renk, 3 neon (neon = _Emission)
Shader "HayalGaraji/Toon"
{
    Properties
    {
        _BaseColor("Ana renk", Color) = (1,1,1,1)
        _BaseMap("Doku", 2D) = "white" {}
        _SecondColor("İkinci renk (iki renk kaplama)", Color) = (1,1,1,1)
        [Enum(Solid,0,Rainbow,1,TwoTone,2,Neon,3)] _Finish("Kaplama", Float) = 0
        _RainbowRange("Gökkuşağı uzunluğu (nesne Z, arkadan öne)", Vector) = (-1.1, 1.1, 0, 0)
        _TwoToneHeight("İki renk sınırı (nesne Y)", Float) = 0.3
        _PaintTex("Boya katmanı", 2D) = "black" {}
        _ShadeColor("Gölge tonu", Color) = (0.7,0.6,0.9,1)
        _ShadeThreshold("Gölge eşiği", Range(-1,1)) = 0.1
        _ShadeSoftness("Gölge yumuşaklığı", Range(0.001,0.5)) = 0.04
        _RimColor("Kenar ışığı", Color) = (1,1,1,1)
        _RimPower("Kenar ışığı keskinliği", Range(0.5,8)) = 3
        _RimStrength("Kenar ışığı gücü", Range(0,1)) = 0.14
        _GlossColor("Parlama rengi (vinil oyuncak)", Color) = (1,1,1,1)
        _GlossSize("Parlama boyutu", Range(0,0.3)) = 0.06
        _GlossStrength("Parlama gücü", Range(0,1)) = 0.45
        [HDR] _Emission("Işıma", Color) = (0,0,0,0)
        _OutlineColor("Çizgi rengi", Color) = (0.17,0.14,0.31,1)
        _OutlinePx("Çizgi kalınlığı (piksel)", Range(0,6)) = 2.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _SecondColor;
            float  _Finish;
            float4 _RainbowRange;
            float  _TwoToneHeight;
            float4 _PaintTex_ST;
            float4 _ShadeColor;
            float  _ShadeThreshold;
            float  _ShadeSoftness;
            float4 _RimColor;
            float  _RimPower;
            float  _RimStrength;
            float4 _GlossColor;
            float  _GlossSize;
            float  _GlossStrength;
            float4 _Emission;
            float4 _OutlineColor;
            float  _OutlinePx;
        CBUFFER_END

        TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
        TEXTURE2D(_PaintTex); SAMPLER(sampler_PaintTex);

        float3 HueToRGB(float h)
        {
            float3 k = abs(frac(h + float3(0.0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0) - 1.0;
            return saturate(k);
        }
        ENDHLSL

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.positionOS = i.positionOS.xyz;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw).rgb;

                float3 body = _BaseColor.rgb;
                if (_Finish > 0.5 && _Finish < 1.5)
                {
                    float t = saturate((i.positionOS.z - _RainbowRange.x) / max(0.001, _RainbowRange.y - _RainbowRange.x));
                    body = lerp(HueToRGB(t * 0.85), float3(1,1,1), 0.25);
                }
                else if (_Finish > 1.5 && _Finish < 2.5)
                {
                    body = i.positionOS.y > _TwoToneHeight ? _SecondColor.rgb : _BaseColor.rgb;
                }

                float4 paint = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, i.uv);
                float3 albedo = lerp(baseTex * body, paint.rgb, paint.a);

                Light mainLight = GetMainLight();
                float ndl = dot(n, mainLight.direction);
                float lit = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, ndl);
                // Işık rengi alınır ama parlaklığı sınırlanır: renkler "yıkanmış" görünmesin
                float3 lc = mainLight.color / max(1.0, max(mainLight.color.r, max(mainLight.color.g, mainLight.color.b)));
                float3 col = lerp(albedo * _ShadeColor.rgb, albedo * lc, lit);
                col += albedo * SampleSH(n) * 0.12;

                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                col += _RimColor.rgb * pow(1.0 - saturate(dot(n, v)), _RimPower) * _RimStrength;

                // Vinil oyuncak parlaması: keskin kenarlı yumuşak ışık lekesi
                float3 h = normalize(mainLight.direction + v);
                float g = smoothstep(1.0 - _GlossSize - 0.012, 1.0 - _GlossSize + 0.012, dot(n, h));
                col += _GlossColor.rgb * g * _GlossStrength * lit;
                col += _Emission.rgb;
                return half4(col, 1);
            }
            ENDHLSL
        }

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
            // Kamera arabanın içindeyken çizgi kabukları görüşü kaplamasın
            float _HG_Interior;
            half4 frag(Varyings i) : SV_Target { clip(0.5 - _HG_Interior); return half4(_OutlineColor.rgb, 1); }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R

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
