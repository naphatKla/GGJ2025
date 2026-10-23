// Energy arrow-blade (Nova Cutter). Procedural - no texture needed.
// Quad UV: x = across (0..1), y = along the blade (0 = base, 1 = tip).
// Vertex color (SpriteRenderer color) tints the outer glow, so each skill level keeps its own color.
Shader "GGJ/VFX/EnergyBlade"
{
    Properties
    {
        [HideInInspector] _MainTex ("Sprite (unused)", 2D) = "white" {}
        [HDR] _CoreColor ("Core Color", Color) = (2.0, 2.0, 2.2, 1)
        [HDR] _BaseColor ("Base Glow Color", Color) = (2.2, 0.25, 1.4, 1)
        _GlowStrength ("Glow Strength", Range(0, 4)) = 1.3
        _BaseWidth ("Width at Base", Range(0.1, 1)) = 0.72
        _TipSharpness ("Tip Sharpness", Range(0.5, 4)) = 1.6
        _NotchDepth ("Back Notch Depth", Range(0, 0.6)) = 0.32
        _CoreWidth ("Core Width", Range(0.02, 0.8)) = 0.4
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.3)) = 0.012
        _FlowSpeed ("Energy Flow Speed", Range(0, 10)) = 5
        _FlowDensity ("Energy Flow Density", Range(1, 30)) = 16
        _FlowStrength ("Energy Flow Strength", Range(0, 1)) = 0.22
        _Flicker ("Flicker", Range(0, 1)) = 0.12
        _Intensity ("Intensity", Range(0, 4)) = 1
        _Padding ("Glow Padding (quad edge)", Range(0, 0.3)) = 0.14
        _HalfSide ("Half Blade (0 = full, 1 = right half, -1 = left half)", Range(-1, 1)) = 0
        _HalfCut ("Half Cut Line (0 = center line, 1 = outer corner)", Range(0, 0.95)) = 0
        _TipFadeY ("Tip Fade (1.1 = off, lower = cut tip shorter)", Range(0.5, 1.1)) = 1.1

        [Header(Shiny)]
        [HDR] _ShineColor ("Shine Color", Color) = (3, 3, 3.2, 1)
        _EdgeShine ("Edge Highlight", Range(0, 3)) = 1.4
        _EdgeShineWidth ("Edge Highlight Width", Range(0.005, 0.15)) = 0.035
        _BevelContrast ("Bevel (two-tone facets)", Range(0, 1)) = 0.55
        _SweepSpeed ("Shine Sweep Speed", Range(0, 4)) = 1.4
        _SweepWidth ("Shine Sweep Width", Range(0.02, 0.4)) = 0.09
        _SweepBoost ("Shine Sweep Brightness", Range(0, 6)) = 3
        _SweepAngle ("Shine Sweep Slant", Range(-1, 1)) = 0.35
        _GlintDensity ("Glint Density", Range(4, 60)) = 22
        _GlintAmount ("Glint Amount", Range(0, 1)) = 0.12
        _GlintSpeed ("Glint Twinkle Speed", Range(0, 20)) = 9
        _TipFlare ("Tip Flare", Range(0, 4)) = 0.25
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
                float4 _CoreColor, _BaseColor;
                float _GlowStrength, _BaseWidth, _TipSharpness, _NotchDepth, _CoreWidth, _EdgeSoftness;
                float _FlowSpeed, _FlowDensity, _FlowStrength, _Flicker, _Intensity;
                float4 _ShineColor;
                float _EdgeShine, _EdgeShineWidth, _BevelContrast, _SweepSpeed, _SweepWidth, _SweepBoost, _SweepAngle;
                float _GlintDensity, _GlintAmount, _GlintSpeed, _TipFlare, _Padding, _HalfSide, _HalfCut, _TipFadeY;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(233.34, 851.73));
                p += dot(p, p + 23.45);
                return frac(p.x * p.y);
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // ใบมีดอยู่ในพื้นที่ด้านในของ quad (เว้นขอบ _Padding) ให้แสงฟุ้งจางหมดก่อนถึงขอบ quad - ไม่โดนตัดเป็นเส้นตรง
                float2 uvRaw = i.uv;
                i.uv = (uvRaw - _Padding) / max(1.0 - 2.0 * _Padding, 1e-3);
                float2 toEdge = min(uvRaw, 1.0 - uvRaw);
                float edgeFade = smoothstep(0.0, max(_Padding * 0.9, 1e-3), min(toEdge.x, toEdge.y));
                float x = abs(i.uv.x - 0.5) * 2.0;     // 0 center .. 1 side
                float y = i.uv.y;                       // 0 base .. 1 tip

                // arrow outline: wide at the base, sharp at the tip, with a notch cut into the back
                float halfW = _BaseWidth * pow(saturate(1.0 - y), _TipSharpness);
                float notch = _NotchDepth * (1.0 - x);                  // V-shaped back
                // ใบมีดครึ่งซีก: ตัดตามแนวกลาง เหลือฝั่งเดียว (ขอบตัดเป็นขอบเรืองแสงเหมือนขอบอื่น)
                float halfSign = _HalfSide > 0.5 ? 1.0 : (_HalfSide < -0.5 ? -1.0 : 0.0);
                // เส้นตัด: จากจุดบนขอบหลัง (A) ไปปลายใบมีด (T) - _HalfCut = 0 คือแนวกลาง, มากขึ้น = เหลือเป็นเสี้ยวเรียวด้านนอก
                float dHalf = 1e3;
                if (halfSign != 0.0)
                {
                    float2 hp = float2((i.uv.x - 0.5) * 2.0 * halfSign, i.uv.y);   // + = ฝั่งที่เก็บไว้
                    float ax = _HalfCut * _BaseWidth;
                    float2 A = float2(ax, _NotchDepth * (1.0 - ax));
                    float2 T = float2(0.0, 1.0);
                    float2 dir = normalize(T - A);
                    float2 nrm = float2(dir.y, -dir.x);                               // ชี้ออกไปทางมุมนอก (x+)
                    dHalf = dot(hp - A, nrm);
                }
                float inside = (x <= halfW) && (y >= notch) && (dHalf >= 0.0) ? 1.0 : 0.0;

                // soft distance to the outline (for glow)
                float dSide = (halfW - x);
                float dBack = (y - notch);
                float d = min(min(dSide, dBack), dHalf);
                // ขอบคม: ความนุ่มขอบตามขนาดพิกเซล (ไม่ฟุ้งจนปลายมน)
                float aa = max(fwidth(d) * 1.2, _EdgeSoftness * saturate(1.0 - y) );
                float shape = smoothstep(-aa, aa, d);
                // halo รอบขอบ: ลดลงที่ปลาย ไม่ให้ปลายเป็นก้อนกลม
                float tipFade = 1.0 - smoothstep(0.6, 0.95, y);
                float glow = exp(-max(-d, 0.0) * 18.0) * (1.0 - inside) * 0.6 * tipFade;

                // bright core line along the center, strongest towards the tip
                float coreW = _CoreWidth * halfW + 1e-3;
                float core = (1.0 - smoothstep(0.0, coreW, x)) * smoothstep(notch, notch + 0.25, y);
                core *= smoothstep(-aa, aa, dHalf);   // ใบมีดครึ่งซีก: ตัดแกนแสงฝั่งที่ถูกตัดออกด้วย

                // energy streaming towards the tip
                float flow = sin((y * _FlowDensity - _Time.y * _FlowSpeed) * 6.28318) * 0.5 + 0.5;
                flow = lerp(1.0, flow, _FlowStrength);

                float flick = 1.0 + _Flicker * sin(_Time.y * 37.0) * sin(_Time.y * 23.0);

                // color: base glow (pink) -> level tint along the blade, white core
                float3 tint = i.color.rgb;
                float3 bodyCol = lerp(_BaseColor.rgb, tint * _GlowStrength, smoothstep(0.0, 0.85, y));
                float3 col = bodyCol * shape * flow + _CoreColor.rgb * core + bodyCol * glow;

                // ───── Shiny ─────
                float sideSign = sign(i.uv.x - 0.5);   // -1 left facet, +1 right facet

                // two-tone bevel: light from the left -> left facet bright, right facet darker (metal ridge look)
                float bevel = lerp(1.0, sideSign < 0 ? 1.35 : 0.55, _BevelContrast);
                col *= lerp(1.0, bevel, shape);

                // thin bright highlight right on the blade edges
                float edgeLine = (1.0 - smoothstep(0.0, _EdgeShineWidth * saturate(1.2 - y), max(d, 0.0))) * shape * smoothstep(0.02, 0.2, y); // ด้านในขอบเท่านั้น บางลงที่ปลาย
                float edgeLit = sideSign < 0 ? 1.0 : 0.45;
                col += _ShineColor.rgb * edgeLine * _EdgeShine * edgeLit;

                // specular band sweeping base -> tip, slanted across the blade, with a pause between sweeps
                float sweepPos = frac(_Time.y * _SweepSpeed * 0.5) * 2.2 - 0.4;
                float band = y + (i.uv.x - 0.5) * _SweepAngle - sweepPos;
                float sweep = exp(-pow(band / _SweepWidth, 2.0)) * shape;
                col += _ShineColor.rgb * sweep * _SweepBoost;

                // twinkling glints scattered inside the blade
                float2 gCell = floor(i.uv * float2(_GlintDensity * 0.5, _GlintDensity));
                float2 gLocal = frac(i.uv * float2(_GlintDensity * 0.5, _GlintDensity)) - 0.5;
                float gRnd = hash21(gCell);
                float gOn = step(1.0 - _GlintAmount, gRnd) * pow(saturate(sin(_Time.y * _GlintSpeed + gRnd * 40.0)), 8.0);
                float star = saturate(0.06 / (abs(gLocal.x) * abs(gLocal.y) * 40.0 + 0.02) - 0.4) * (1.0 - smoothstep(0.0, 0.45, length(gLocal)));
                col += _ShineColor.rgb * star * gOn * shape * 1.5;

                // star flare at the tip
                float2 tipP = float2((i.uv.x - 0.5) * 2.0, y - 0.995);
                float flarePulse = 0.75 + 0.25 * sin(_Time.y * 11.0);
                float flare = (0.004 / (tipP.x * tipP.x * 6.0 + 0.004) * exp(-abs(tipP.y) * 30.0)
                             + 0.004 / (tipP.y * tipP.y * 6.0 + 0.004) * exp(-abs(tipP.x) * 30.0)) * 0.5
                             ;
                col += _ShineColor.rgb * flare * _TipFlare * flarePulse;

                float alpha = saturate(shape * (0.55 + 0.45 * flow) + core + glow + edgeLine * 0.5 + sweep * 0.5 + flare * _TipFlare * 0.5) * i.color.a;
                // ตัดปลายเรียวที่บางกว่าพิกเซล (ไม่ให้เป็นเส้นประยาวๆ) - ใช้กับใบมีดเล็ก
                edgeFade *= 1.0 - smoothstep(_TipFadeY - 0.07, _TipFadeY, y);
                alpha *= edgeFade;
                return half4(col * _Intensity * flick * edgeFade, alpha);
            }
            ENDHLSL
        }
    }
}
