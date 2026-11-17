Shader "Cat/Four Point Star Glow"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float2 p = abs(i.uv * 2 - 1);
                // Four narrow tips and curved, concave sides, with a soft halo.
                float d = sqrt(p.x) + sqrt(p.y);
                float aa = max(fwidth(d), .015);
                float star = 1 - smoothstep(.93 - aa, .93 + aa, d);
                float halo = exp(-dot(p,p) * 8) * .38;
                float glow = star * 2.8 + halo;
                return half4(i.color.rgb * glow, i.color.a);
            }
            ENDHLSL
        }
    }
}
