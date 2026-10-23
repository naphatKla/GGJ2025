// Singularity (Bright2): thin white ring + dot field pulled towards the center. Procedural.
// Use on a SpriteRenderer with a full-rect square sprite (UV 0..1). Vertex color = tint * alpha.
// _RingRadius / _DotAlpha / _Intensity are driven per instance by SingularityVFX (MaterialPropertyBlock).
Shader "GGJ/VFX/SingularityRing"
{
    Properties
    {
        [HideInInspector] _MainTex ("Sprite (unused)", 2D) = "white" {}
        [HDR] _RingColor ("Ring Color", Color) = (2.2, 2.2, 2.4, 1)
        [HDR] _GlowColor ("Ring Glow Color", Color) = (0.8, 0.8, 1.0, 1)
        _RingRadius ("Ring Radius (0..1)", Range(0, 1)) = 0.95
        _RingWidth ("Ring Width", Range(0.001, 0.1)) = 0.008
        _GlowWidth ("Glow Width", Range(0.005, 0.5)) = 0.06
        _GlowAmount ("Glow Amount", Range(0, 2)) = 0.6
        _InnerGlow ("Inner Glow (corona)", Range(0, 2)) = 0
        _InnerGlowWidth ("Inner Glow Width", Range(0.01, 1)) = 0.25
        [HDR] _DotColor ("Dot Color", Color) = (1, 1, 1.1, 1)
        _DotAlpha ("Dot Field Alpha", Range(0, 1)) = 0
        _DotDensity ("Dot Density", Range(4, 120)) = 48
        _DotSize ("Dot Size", Range(0.02, 0.5)) = 0.16
        _DotPullSpeed ("Dot Pull Speed", Range(0, 3)) = 0.35
        _RingAlpha ("Ring Alpha", Range(0, 1)) = 1
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
                float4 _RingColor, _GlowColor, _DotColor;
                float _RingRadius, _RingWidth, _GlowWidth, _GlowAmount, _InnerGlow, _InnerGlowWidth;
                float _DotAlpha, _DotDensity, _DotSize, _DotPullSpeed, _Intensity, _RingAlpha;
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

            half4 frag (Varyings i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float r = length(p);
                float aa = fwidth(r) * 1.5 + 1e-5;

                // thin crisp ring
                float d = abs(r - _RingRadius);
                float ring = 1.0 - smoothstep(_RingWidth, _RingWidth + aa, d);
                // soft glow both sides, stronger outside
                float glow = exp(-d / max(_GlowWidth, 1e-3)) * _GlowAmount;
                // corona: light bleeding inward from the ring (eclipse look)
                float inner = (r < _RingRadius) ? exp(-(_RingRadius - r) / max(_InnerGlowWidth, 1e-3)) * _InnerGlow : 0.0;

                // dot field (halftone) inside the ring, drifting towards the center
                float dots = 0.0;
                if (_DotAlpha > 0.001 && r < _RingRadius)
                {
                    float logR = log(max(r, 1e-3));
                    float ang = atan2(p.y, p.x) / 6.28318 + 0.5;
                    // polar grid: rows march inwards over time
                    float rows = _DotDensity * 0.25;
                    float2 g = float2(ang * _DotDensity, logR * rows + _Time.y * _DotPullSpeed * rows);
                    float2 cell = floor(g);
                    float2 f = frac(g) - 0.5;
                    float rnd = hash21(cell);
                    float size = _DotSize * (0.6 + 0.8 * rnd) * smoothstep(0.0, 0.35, r);   // smaller near the center
                    dots = 1.0 - smoothstep(size, size + 0.08, length(f));
                    dots *= smoothstep(_RingRadius, _RingRadius - 0.15, r);                   // fade at the ring
                    dots *= 0.35 + 0.65 * (0.5 + 0.5 * sin(_Time.y * 4.0 + rnd * 30.0));    // twinkle
                    dots *= _DotAlpha;
                }

                ring *= _RingAlpha; glow *= _RingAlpha; inner *= _RingAlpha;
                float3 col = _RingColor.rgb * ring + _GlowColor.rgb * (glow + inner) + _DotColor.rgb * dots;
                float alpha = saturate(ring + glow + inner + dots * 0.8);
                // never reach the quad edge
                alpha *= 1.0 - smoothstep(0.96, 1.0, r);

                col *= i.color.rgb * _Intensity;
                return half4(col, alpha * i.color.a);
            }
            ENDHLSL
        }
    }
}
