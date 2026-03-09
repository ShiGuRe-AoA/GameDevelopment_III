Shader "Game2D/GhostPlacementGrayscaleTint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _GhostColor ("Ghost Color", Color) = (0.2, 0.8, 1.0, 1.0)
        _Opacity ("Opacity", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _GhostColor;
            float _Opacity;

            struct a2v
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;      // SpriteRenderer 的颜色会进这里
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            v2f vert (a2v v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // 原纹理亮度（灰度信息）
                fixed lum = dot(tex.rgb, fixed3(0.299, 0.587, 0.114));

                // 颜色：指定纯色 * 灰度明暗 * 顶点色(可选)
                fixed3 rgb = _GhostColor.rgb * lum * i.color.rgb;

                // 透明度：纹理 alpha * 颜色 alpha * 顶点 alpha * 全局 Opacity
                fixed a = tex.a * _GhostColor.a * i.color.a * _Opacity;

                return fixed4(rgb, a);
            }
            ENDHLSL
        }
    }
}