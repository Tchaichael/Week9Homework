Shader "Wind/Lines"
{
    Properties
    {
        _ClipWind ("Clip To Wind Zone", Float) = 0
        _WindBoundsMin ("Wind Bounds Min", Vector) = (0,0,0,0)
        _WindBoundsMax ("Wind Bounds Max", Vector) = (0,0,0,0)
        _WindFeather ("Wind Edge Fade", Vector) = (1,1,1,0)
    }
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
            float _ClipWind;
            float4x4 _WindWorldToLocal;
            float4 _WindBoundsMin, _WindBoundsMax, _WindFeather;
            struct Varyings { float4 position : SV_POSITION; fixed4 color : COLOR; float3 world : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.color = input.color;
                output.world = mul(unity_ObjectToWorld, input.position).xyz;
                return output;
            }
            fixed4 frag(Varyings input) : SV_Target
            {
                fixed4 color = input.color;
                if (_ClipWind > .5)
                {
                    float3 local = mul(_WindWorldToLocal, float4(input.world, 1)).xyz;
                    float3 edge = min(local - _WindBoundsMin.xyz, _WindBoundsMax.xyz - local);
                    clip(min(edge.x, min(edge.y, edge.z)));
                    float3 fade = smoothstep(0, max(_WindFeather.xyz, .0001), edge);
                    color.a *= min(fade.x, min(fade.y, fade.z));
                }
                return color;
            }
            ENDHLSL
        }
    }
}
