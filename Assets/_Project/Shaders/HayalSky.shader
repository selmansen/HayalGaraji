// Hayal Garajı gökyüzü kubbesi (URP): gündüz pastel geçiş, gece mor-lacivert ve göz kırpan yıldızlar.
Shader "HayalGaraji/Sky"
{
    Properties
    {
        _Top("Gündüz tepe", Color) = (0.33,0.62,1,1)
        _Horizon("Gündüz ufuk", Color) = (0.78,0.91,1,1)
        _Bottom("Gündüz alt", Color) = (0.78,0.91,1,1)
        _TopNight("Gece tepe", Color) = (0.07,0.06,0.25,1)
        _HorizonNight("Gece ufuk", Color) = (0.32,0.2,0.52,1)
        _BottomNight("Gece alt", Color) = (0.14,0.1,0.3,1)
        _Night("Gece (0-1)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "RenderPipeline"="UniversalPipeline" }
        Cull Front
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Top, _Horizon, _Bottom, _TopNight, _HorizonNight, _BottomNight;
                float _Night;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.dir = i.positionOS.xyz;
                return o;
            }
            float3 Grad(float y, float3 top, float3 hor, float3 bot)
            {
                return y > 0 ? lerp(hor, top, pow(saturate(y), 0.6)) : lerp(hor, bot, saturate(-y * 3.0));
            }
            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 day = Grad(d.y, _Top.rgb, _Horizon.rgb, _Bottom.rgb);
                float3 night = Grad(d.y, _TopNight.rgb, _HorizonNight.rgb, _BottomNight.rgb);
                float3 col = lerp(day, night, _Night);

                float2 uv = float2(atan2(d.x, d.z), asin(clamp(d.y, -1, 1))) * 38.0;
                float2 id = floor(uv);
                float h = frac(sin(dot(id, float2(12.9898, 78.233))) * 43758.5453);
                float2 f = frac(uv) - 0.5;
                float tw = 0.6 + 0.4 * sin(_Time.y * 2.5 + h * 40.0);
                float star = step(0.975, h) * smoothstep(0.18, 0.0, length(f)) * saturate(d.y * 4.0) * tw;
                col += star * _Night;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
