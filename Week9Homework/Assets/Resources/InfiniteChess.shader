Shader "Chess/Infinite Board"
{
    Properties
    {
        _Color ("Board", Color) = (0.72, 0.55, 0.32, 1)
        _LineColor ("Grid", Color) = (0.22, 0.16, 0.10, 1)
        _CellSize ("Cell Size", Float) = 9
        _GridOffset ("Grid Position Offset (X/Y)", Vector) = (0,0,0,0)
        _LineWidth ("Line Width", Float) = 0.18
        _BoardZ ("Grid Projection Depth (Match Pieces)", Float) = -79
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
        Pass
        {
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color, _LineColor, _GridOffset;
            float _CellSize, _LineWidth, _BoardZ;
            struct Attributes { float4 vertex : POSITION; };
            struct Varyings { float4 position : SV_POSITION; float3 ray : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.ray = input.vertex.xyz;
                return output;
            }
            float4 frag(Varyings input) : SV_Target
            {
                // Skybox ray/plane intersection: no finite mesh and no board edges.
                float3 ray = normalize(input.ray);
                float safeZ = (ray.z < 0 ? -1 : 1) * max(abs(ray.z), 0.001);
                float2 world = _WorldSpaceCameraPos.xy + ray.xy * ((_BoardZ - _WorldSpaceCameraPos.z) / safeZ);
                float2 grid = (world - _GridOffset.xy) / max(_CellSize, 0.01);
                float2 distanceToLine = abs(frac(grid + 0.5) - 0.5) * _CellSize;
                float pixel = max(fwidth(world.x), fwidth(world.y));
                float gridLine = 1 - smoothstep(_LineWidth, _LineWidth + pixel,
                    min(distanceToLine.x, distanceToLine.y));
                // Subtle continuous grain, with no board edge, seam or outer frame.
                float grain = sin(world.y * 1.7 + sin(world.x * 0.035) * 2.0) * 0.012;
                float3 color = lerp(_Color.rgb + grain, _LineColor.rgb, gridLine * 0.65);
                return float4(color, 1);
            }
            ENDHLSL
        }
    }
}
