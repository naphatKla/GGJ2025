// Honeycomb energy shield (Parry). Procedural - no texture needed.
// Use on a SpriteRenderer with a full-rect square sprite (UV 0..1). Vertex color = tint * alpha.
// Driven per-instance by HexShieldVFX through a MaterialPropertyBlock (_Wave, _Dissolve, _Intensity).
Shader "GGJ/VFX/HexShield"
{
    Properties
    {
        [HideInInspector] _MainTex ("Sprite (unused)", 2D) = "white" {}
        [HDR] _FillColor ("Fill Color", Color) = (0.05, 0.35, 1.0, 1)
        [HDR] _LineColor ("Hex Line Color", Color) = (0.3, 0.85, 1.6, 1)
        [HDR] _RimColor ("Rim Color", Color) = (0.8, 1.6, 2.2, 1)
        _HexScale ("Hex Count (across)", Range(2, 30)) = 9
        _LineWidth ("Hex Line Width", Range(0.01, 0.3)) = 0.08
        _FillAlpha ("Center Fill Alpha", Range(0, 1)) = 0.08
        _RimPower ("Rim Falloff (sphere look)", Range(0.5, 8)) = 3
        _RimWidth ("Outer Ring Width", Range(0.005, 0.2)) = 0.035
        _Twinkle ("Cell Twinkle", Range(0, 1)) = 0.35
        _TwinkleSpeed ("Twinkle Speed", Range(0, 20)) = 6
        _ScrollSpeed ("Scan Line Speed", Range(-3, 3)) = 0.6
        _Wave ("Wave Radius (0..1)", Range(-0.2, 1.3)) = -0.2
        _WaveWidth ("Wave Width", Range(0.02, 0.5)) = 0.12
        _WaveBoost ("Wave Brightness", Range(0, 6)) = 2.5
        _Dissolve ("Dissolve (per hex)", Range(0, 1)) = 0
        _Intensity ("Intensity", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FillColor, _LineColor, _RimColor;
                float _HexScale, _LineWidth, _FillAlpha, _RimPower, _RimWidth;
                float _Twinkle, _TwinkleSpeed, _ScrollSpeed;
                float _Wave, _WaveWidth, _WaveBoost, _Dissolve, _Intensity;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Hex grid: returns (distance to cell edge 0..0.5, cell id.xy)
            float3 hexGrid(float2 p)
            {
                const float2 s = float2(1, 1.7320508);
                float4 hc = floor(float4(p, p - float2(0.5, 1)) / s.xyxy) + 0.5;
                float4 h = float4(p - hc.xy * s, p - (hc.zw + 0.5) * s);
                float4 c = dot(h.xy, h.xy) < dot(h.zw, h.zw) ? float4(h.xy, hc.xy) : float4(h.zw, hc.zw + 9.73);
                float2 a = abs(c.xy);
                float edge = 0.5 - max(dot(a, s * 0.5), a.x);   // 0 at the border, 0.5 at the center
                return float3(edge, c.zw);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;          // -1..1
                float r = length(p);
                if (r > 1.0) discard;

                // sphere-ish look: brighter towards the rim
                float rim = pow(saturate(r), _RimPower);

                // slight bulge so the honeycomb looks wrapped on a dome
                float2 dome = p * (1.0 + 0.35 * r * r);
                float3 hx = hexGrid(dome * _HexScale * 0.5);
                float edge = hx.x;
                float cell = hash21(hx.yz);

                // per-hex dissolve (cells drop out one by one)
                if (cell < _Dissolve) discard;

                float hexLine = 1.0 - smoothstep(0.0, _LineWidth, edge);

                // twinkling cells + a scan line sweeping across
                float tw = hash21(hx.yz + floor(_Time.y * _TwinkleSpeed));
                float twinkle = 1.0 + _Twinkle * (tw * 2.0 - 1.0);
                float scan = smoothstep(0.92, 1.0, sin((p.y - _Time.y * _ScrollSpeed) * 3.14159 * 1.5) * 0.5 + 0.5);

                // expanding wave band
                float wave = exp(-pow((r - _Wave) / max(_WaveWidth, 1e-3), 2.0));

                float lineAmt = hexLine * (0.25 + 0.75 * rim) * twinkle + hexLine * wave * _WaveBoost;
                float fillAmt = _FillAlpha * (0.4 + rim) + wave * 0.35 + scan * 0.15;
                float outer = smoothstep(1.0 - _RimWidth * 2.0, 1.0 - _RimWidth * 0.5, r) * (1.0 - smoothstep(0.995, 1.0, r));

                float3 col = _FillColor.rgb * fillAmt + _LineColor.rgb * lineAmt + _RimColor.rgb * (outer * 1.5 + rim * 0.35);
                float alpha = saturate(fillAmt + lineAmt + outer + rim * 0.2);

                col *= i.color.rgb * _Intensity;
                alpha *= i.color.a;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
