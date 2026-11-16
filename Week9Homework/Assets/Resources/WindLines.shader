Shader "Wind/Lines"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Attributes { float4 position : POSITION; fixed4 color : COLOR; };
            struct Varyings { float4 position : SV_POSITION; fixed4 color : COLOR; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.color = input.color;
                return output;
            }
            fixed4 frag(Varyings input) : SV_Target { return input.color; }
            ENDHLSL
        }
    }
}
