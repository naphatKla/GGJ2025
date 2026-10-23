// Break Point (Bright2 Breaking state). Procedural, two looks in one shader:
//  - Spikes (concept 1): white shatter spikes bursting from the center (_SpikeAlpha / _SpikeLen)
//  - Shield (concept 2): shield outline that cracks along a zigzag and splits in half (_ShieldAlpha / _Crack / _Split)
// Use on a SpriteRenderer with a full-rect square sprite (UV 0..1). Driven per instance by BreakingVFX (MaterialPropertyBlock).
Shader "GGJ/VFX/BreakCrack"
{
    Properties
    {
        [HideInInspector] _MainTex ("Sprite (unused)", 2D) = "white" {}
        [HDR] _Color ("Line Color", Color) = (1.5, 1.5, 1.6, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (0.55, 0.42, 1.1, 1)
        _GlowWidth ("Glow Width", Range(0.005, 0.3)) = 0.045
        _GlowAmount ("Glow Amount", Range(0, 2)) = 0.45
        _EdgeMask ("Edge Mask Start (radius)", Range(0.3, 0.98)) = 0.8

        [Header(Spikes Concept 1)]
        _SpikeAlpha ("Spike Alpha", Range(0, 1)) = 0
        _SpikeLen ("Spike Length (0..1)", Range(0, 1)) = 1
        _SpikeWidth ("Spike Width", Range(0.005, 0.15)) = 0.05
        _SpikeCount ("Spike Count", Range(3, 16)) = 11
        _SpikeSeed ("Spike Seed", Float) = 3.7
        _CoreFlash ("Center Flash", Range(0, 2)) = 0.6
        _RingRadius ("Shock Ring Radius (0..1)", Range(0, 1)) = 0
        _RingAlpha ("Shock Ring Alpha", Range(0, 1)) = 0

        [Header(Shield Concept 2)]
        _ShieldAlpha ("Shield Alpha", Range(0, 1)) = 0
        _ShieldLine ("Outline Width", Range(0.005, 0.1)) = 0.035
        _ShieldFill ("Fill Alpha", Range(0, 1)) = 0.07
        _Crack ("Crack Progress (top -> bottom)", Range(0, 1)) = 0
        _Split ("Split Distance", Range(0, 1)) = 0
        _SplitTilt ("Split Tilt (radians)", Range(0, 1)) = 0.25

        _Intensity ("Intensity", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Blend One One
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
                float4 _Color, _GlowColor;
                float _GlowWidth, _GlowAmount, _EdgeMask;
                float _SpikeAlpha, _SpikeLen, _SpikeWidth, _SpikeCount, _SpikeSeed, _CoreFlash, _RingRadius, _RingAlpha;
                float _ShieldAlpha, _ShieldLine, _ShieldFill, _Crack, _Split, _SplitTilt;
                float _Intensity;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash11(float n) { return frac(sin(n * 127.1) * 43758.5453); }

            float2 rot(float2 p, float a) { float c = cos(a), s = sin(a); return float2(c * p.x - s * p.y, s * p.x + c * p.y); }

            // ── concept 1: thin triangle spikes from the center (signed distance, <0 inside) ──
            float spikes(float2 p)
            {
                float d = 1e3;
                int n = (int)_SpikeCount;
                for (int i = 0; i < 16; i++)
                {
                    if (i >= n) break;
                    float fi = i + _SpikeSeed * 7.0;
                    float a = (i + hash11(fi) * 0.8) / n * 6.28318;
                    float len = lerp(0.35, 0.95, hash11(fi + 1.3)) * _SpikeLen;
                    float w = _SpikeWidth * lerp(0.5, 1.3, hash11(fi + 2.1));
                    float2 q = rot(p, -a);              // spike along +x
                    float u = q.x / max(len, 1e-3);
                    float taper = w * (1.0 - saturate(u));
                    float ds = abs(q.y) - taper;
                    ds = max(ds, max(-q.x, q.x - len));
                    d = min(d, ds);
                    // short side branch (jagged shatter look)
                    if (hash11(fi + 4.7) > 0.45)
                    {
                        float bu = lerp(0.3, 0.6, hash11(fi + 5.9)) * len;
                        float ba = (hash11(fi + 6.3) > 0.5 ? 1.0 : -1.0) * 0.6;
                        float2 b = rot(q - float2(bu, 0), -ba);
                        float bl = len * 0.35;
                        float bt = w * 0.6 * (1.0 - saturate(b.x / max(bl, 1e-3)));
                        float db = max(abs(b.y) - bt, max(-b.x, b.x - bl));
                        d = min(d, db);
                    }
                }
                return d;
            }

            // ── concept 2: shield shape (signed distance, <0 inside), fits in about -0.85..0.75 ──
            float shieldSdf(float2 p)
            {
                float2 bq = abs(p - float2(0, 0.31)) - float2(0.62, 0.41);
                float box = length(max(bq, 0)) + min(max(bq.x, bq.y), 0);
                float cl = length(p - float2(0.1437, -0.1)) - 0.7637;
                float cr = length(p - float2(-0.1437, -0.1)) - 0.7637;
                float lens = max(cl, cr);                        // pointed bottom (overlaps the box, no seam)
                float s = min(box, lens);
                float dip = length(p - float2(0, 1.26)) - 0.6;   // dip in the top edge
                return max(s, -dip);
            }

            // zigzag crack: signed horizontal distance (<0 = left side)
            float crackSide(float2 p)
            {
                float2 P0 = float2(-0.04, 0.95), P1 = float2(0.12, 0.45), P2 = float2(-0.13, 0.12);
                float2 P3 = float2(0.09, -0.22), P4 = float2(-0.06, -0.52), P5 = float2(0.03, -1.0);
                float x;
                if (p.y > P1.y)      x = lerp(P1.x, P0.x, saturate((p.y - P1.y) / (P0.y - P1.y)));
                else if (p.y > P2.y) x = lerp(P2.x, P1.x, (p.y - P2.y) / (P1.y - P2.y));
                else if (p.y > P3.y) x = lerp(P3.x, P2.x, (p.y - P3.y) / (P2.y - P3.y));
                else if (p.y > P4.y) x = lerp(P4.x, P3.x, (p.y - P4.y) / (P3.y - P4.y));
                else                 x = lerp(P5.x, P4.x, saturate((p.y - P5.y) / (P4.y - P5.y)));
                return (p.x - x) * 0.85;   // ~cos of the zigzag slope
            }

            #define SHIELD_SCALE 1.6

            void shieldHalf(float2 p, float side, float aa, inout float lineA, inout float fillA, inout float glow)
            {
                // move this half outward + tilt it away from the crack
                // shield drawn smaller than the quad (x SHIELD_SCALE) so the split halves never reach the edge mask
                aa *= SHIELD_SCALE;
                float2 q = p * SHIELD_SCALE - float2(side * _Split * 0.6, -_Split * 0.15);
                q = rot(q, side * _Split * _SplitTilt);
                float s = shieldSdf(q);
                float c = crackSide(q) * side;           // >0 = this half's side
                float h = (_Split > 0.001) ? max(s, -c) : s;
                float ln = 1.0 - smoothstep(_ShieldLine, _ShieldLine + aa, abs(h));
                float inner = 1.0 - smoothstep(_ShieldLine * 0.35, _ShieldLine * 0.35 + aa, abs(h + 0.09));
                inner *= step(h, 0);
                // crack line drawn while cracking (before the halves separate)
                float crackLine = 0;
                if (_Split <= 0.001)
                {
                    float front = 1.0 - _Crack * 2.1;                     // top -> bottom
                    float reveal = smoothstep(front - 0.04, front + 0.04, q.y);
                    float inside = 1.0 - smoothstep(-aa, aa, s);
                    crackLine = (1.0 - smoothstep(_ShieldLine * 0.6, _ShieldLine * 0.6 + aa, abs(c))) * inside * reveal;
                    // bright spark at the crack tip while it travels
                    crackLine += exp(-length(float2(c, q.y - front)) / 0.05) * inside * step(0.001, _Crack) * step(_Crack, 0.99) * 1.5;
                }
                lineA = max(lineA, max(ln, max(inner * 0.5, crackLine)));
                // energy fill: soft, stronger near the rim, no hard edge
                float fill = (1.0 - smoothstep(-aa, aa, h)) * lerp(0.35, 1.0, smoothstep(-0.35, 0.0, h));
                fillA = max(fillA, fill);
                // glow hugs the outline and reaches exactly 0 a short distance away (no visible box)
                float g = exp(-abs(h) / max(_GlowWidth, 1e-3)) * (1.0 - smoothstep(_GlowWidth * 2.0, _GlowWidth * 5.0, abs(h)));
                glow = max(glow, g);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float r = length(p);
                float aa = fwidth(r) * 1.5 + 1e-5;

                float3 col = 0;

                if (_SpikeAlpha > 0.001)
                {
                    float d = spikes(p);
                    float ln = 1.0 - smoothstep(0, aa, d);
                    float dd = max(d, 0);
                    float gl = exp(-dd / max(_GlowWidth, 1e-3)) * (1.0 - smoothstep(_GlowWidth * 2.0, _GlowWidth * 5.0, dd));
                    gl *= _GlowAmount * (1.0 - ln);
                    // spikes are brightest at the center and thin out towards the tips
                    float tip = lerp(1.0, 0.45, smoothstep(0.1, max(_SpikeLen, 0.11), r));
                    float core = exp(-r / 0.06) * _CoreFlash;
                    col += (_Color.rgb * (ln * tip + core) + _GlowColor.rgb * gl) * _SpikeAlpha;
                }

                if (_RingAlpha > 0.001)
                {
                    float dr = abs(r - _RingRadius);
                    float ring = 1.0 - smoothstep(0.004, 0.004 + aa, dr);
                    float ringGlow = exp(-dr / 0.035) * (1.0 - smoothstep(0.07, 0.15, dr)) * 0.6;
                    col += (_Color.rgb * ring + _GlowColor.rgb * ringGlow) * _RingAlpha;
                }

                if (_ShieldAlpha > 0.001)
                {
                    float lineA = 0, fillA = 0, glow = 0;
                    shieldHalf(p, -1.0, aa, lineA, fillA, glow);
                    if (_Split > 0.001) shieldHalf(p, 1.0, aa, lineA, fillA, glow);
                    glow *= _GlowAmount * (1.0 - lineA);
                    float fill = fillA * _ShieldFill * (1.0 - lineA);
                    col += (_Color.rgb * lineA + _GlowColor.rgb * (glow + fill)) * _ShieldAlpha;
                }

                // additive: colour is already weighted by each layer's alpha
                // soft round mask: nothing ever reaches the quad edge
                float mask = 1.0 - smoothstep(_EdgeMask, 0.99, r);
                col *= i.color.rgb * _Intensity * i.color.a * mask;
                return half4(col, 0);
            }
            ENDHLSL
        }
    }
}
