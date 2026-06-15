Shader "Custom/OutlineComposite"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color", Color) = (0, 1, 1, 1)
        _Thickness ("Thickness", Float) = 2
        _UseDiagonal ("Use Diagonal", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Composite"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_OutlineMask);
            SAMPLER(sampler_OutlineMask);

            float4 _OutlineColor;
            float4 _OutlineTexelSize;
            float _Thickness;
            float _UseDiagonal;

            float SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_OutlineMask, sampler_OutlineMask, uv).r;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                half4 sceneColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );

                float center = SampleMask(uv);

                float2 offset = _OutlineTexelSize.xy * _Thickness;

                float around = 0;

                around = max(around, SampleMask(uv + float2( offset.x, 0)));
                around = max(around, SampleMask(uv + float2(-offset.x, 0)));
                around = max(around, SampleMask(uv + float2(0,  offset.y)));
                around = max(around, SampleMask(uv + float2(0, -offset.y)));

                if (_UseDiagonal > 0.5)
                {
                    around = max(around, SampleMask(uv + float2( offset.x,  offset.y)));
                    around = max(around, SampleMask(uv + float2(-offset.x,  offset.y)));
                    around = max(around, SampleMask(uv + float2( offset.x, -offset.y)));
                    around = max(around, SampleMask(uv + float2(-offset.x, -offset.y)));
                }

                // 只画物体外侧描边
                float outline = saturate(around - center);

                half4 finalColor = sceneColor;
                finalColor.rgb = lerp(
                    finalColor.rgb,
                    _OutlineColor.rgb,
                    outline * _OutlineColor.a
                );

                return finalColor;
            }
            ENDHLSL
        }
    }
}