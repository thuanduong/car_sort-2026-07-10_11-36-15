Shader "Custom/URPSpriteOutline_Multi"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [Toggle] _OutlineEnabled ("Outline Enabled", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1,1,0,1)
        _OutlineSize ("Outline Size", Range(0, 0.05)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // Unity tự cung cấp kích thước pixel
            float _OutlineEnabled;
            float4 _OutlineColor;
            float _OutlineSize;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 c = tex2D(_MainTex, IN.uv);

                if (_OutlineEnabled > 0.5)
                {
                    // Tận dụng _MainTex_TexelSize để tính toán pixel chính xác
                    // Cách này không dùng Scale, không làm hỏng tọa độ Atlas
                    float2 uv_x = float2(_MainTex_TexelSize.x * _OutlineSize * 100, 0);
                    float2 uv_y = float2(0, _MainTex_TexelSize.y * _OutlineSize * 100);

                    float alpha = tex2D(_MainTex, IN.uv + uv_x).a + 
                                  tex2D(_MainTex, IN.uv - uv_x).a + 
                                  tex2D(_MainTex, IN.uv + uv_y).a + 
                                  tex2D(_MainTex, IN.uv - uv_y).a;

                    if (c.a < 0.1 && alpha > 0.1)
                    {
                        return half4(_OutlineColor.rgb, 1.0);
                    }
                }
                return c * c.a;
            }
            ENDHLSL
        }
    }
}