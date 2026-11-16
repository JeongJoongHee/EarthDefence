// 3차 터렛 스킨 (11-16 밤): 스프라이트 색을 돌림(Hue) · 채도 x(Sat). 선(진갈색) · 흰색처럼 채도가 낮거나 어두운 곳은 그대로 → 낙서풍 선이 유지됨.
// Linear 색 공간이라 감마로 바꿔 돌리고 다시 선형으로 (tools/skin_preview.py 가 UI 용 그림을 같은 식으로 만듦).
// SpriteRenderer 색 · 뒤집기는 UnitySprites.cginc SpriteVert (SpriteAdd 와 같음). 값은 MaterialPropertyBlock 으로 (_Hue 0~1 = 0~360도).
Shader "EarthDef/SpriteHue"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Hue ("Hue (0~1)", Float) = 0
        _Sat ("Saturation x", Float) = 1
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _Hue, _Sat;
            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y); float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }
            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }
            fixed4 frag (v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                float3 g = LinearToGammaSpace(c.rgb / max(c.a, 0.001));
                float3 h = rgb2hsv(g);
                float k = smoothstep(0.12, 0.3, h.y) * smoothstep(0.18, 0.38, h.z);
                float3 r = hsv2rgb(float3(frac(h.x + _Hue), saturate(h.y * _Sat), h.z));
                g = lerp(g, r, k);
                c.rgb = GammaToLinearSpace(g) * c.a;
                return c;
            }
            ENDCG
        }
    }
}
