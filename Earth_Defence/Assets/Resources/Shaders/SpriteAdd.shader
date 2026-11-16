// 11-16 1차 수정안(네온): 빛 번짐 · 불꽃 튐 · 총구 섬광 · 레이저 · 낙뢰 · 터렛 대기 효과를 "빛 더하기"로 그림 → 어두운 밤하늘에서 네온처럼 빛남.
// 2D 조명을 쓰지 않는 게임이라 조명 없는 셰이더로 충분 (Resources 에 두어 빌드에 들어감).
// 11-16 밤 고침: 처음 판은 꼭짓점 색만 곱했는데, Unity 6 은 SpriteRenderer.color · flipX/Y 를 _RendererColor · _Flip 으로 넘김
//   → 색 · 투명도(사라지기) · 뒤집기가 모두 무시돼 빛이 전부 흰색으로 그려졌음 (벌집 입구 빛 픽셀을 재서 찾음).
//   기본 스프라이트 셰이더와 같은 UnitySprites.cginc 의 SpriteVert(색 · 뒤집기 처리)를 씀.
Shader "EarthDef/SpriteAdd"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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
        Blend SrcAlpha One
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
            fixed4 frag (v2f IN) : SV_Target { return SampleSpriteTexture(IN.texcoord) * IN.color; }
            ENDCG
        }
    }
}
