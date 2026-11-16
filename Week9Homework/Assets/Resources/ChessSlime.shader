Shader "Chess/Glossy Slime"
{
    Properties { _Color ("Slime Color", Color) = (0.7,0.9,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float4 _Blobs[24];
            int _BlobCount;
            float _Blend, _Thickness, _Radius, _Wobble;
            struct Input { float4 vertex : POSITION; };
            struct Varyings { float4 position : SV_POSITION; float3 world : TEXCOORD0; };
            struct Output { float4 color : SV_Target; float depth : SV_Depth; };
            Varyings vert(Input input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.world = mul(unity_ObjectToWorld, input.vertex).xyz;
                return output;
            }
            float field(float3 p)
            {
                float density = 0;
                float support = 1.15 + (_Blend / max(_Radius, 0.001)) * 0.55;
                [loop] for (int i = 0; i < _BlobCount; i++)
                {
                    float3 q = p - _Blobs[i].xyz;
                    q.z /= _Thickness;
                    float ripple = sin(_Time.y * 2.5 + _Blobs[i].x * 0.17) * _Wobble;
                    float radius = _Blobs[i].w * (1 + ripple);
                    float kernel = saturate(1 - dot(q,q) / (radius * radius * support * support));
                    density += kernel * kernel * kernel;
                }
                float iso = 1 - 1 / (support * support);
                iso = iso * iso * iso;
                return (iso - density) * _Radius * 0.7;
            }
            Output frag(Varyings input)
            {
                float3 direction = normalize(input.world - _WorldSpaceCameraPos);
                if (unity_OrthoParams.w > 0.5) direction = -UNITY_MATRIX_V[2].xyz;
                float3 p = input.world;
                if (field(p) < 0) discard;
                float distance = 0;
                bool hit = false;
                [loop] for (int step = 0; step < 64; step++)
                {
                    float d = field(p);
                    if (d < _Radius * 0.001) { hit = true; break; }
                    float advance = max(d, _Radius * 0.004);
                    distance += advance;
                    if (distance > _Radius * 5) break;
                    p += direction * advance;
                }
                if (!hit) discard;
                float e = _Radius * 0.015;
                // Refine depth so overlapping proxy meshes agree on the surface.
                [unroll] for (int refine = 0; refine < 3; refine++)
                {
                    float d = field(p);
                    float slope = min(-0.05, (field(p + direction * e) - d) / e);
                    p -= direction * clamp(d / slope, -_Radius * 0.05, _Radius * 0.05);
                }
                float3 n = normalize(float3(
                    field(p + float3(e,0,0)) - field(p - float3(e,0,0)),
                    field(p + float3(0,e,0)) - field(p - float3(0,e,0)),
                    field(p + float3(0,0,e)) - field(p - float3(0,0,e))));
                float3 light = normalize(float3(-0.4,0.65,-0.8));
                float3 view = -direction;
                float diffuse = saturate(dot(n, light));
                float specular = pow(saturate(dot(n, normalize(light + view))), 72);
                float broadHighlight = pow(saturate(dot(n, normalize(float3(0.5,0.8,-1) + view))), 14);
                float rim = pow(1 - saturate(dot(n, view)), 3);
                float3 color = _Color.rgb * (0.52 + 0.48 * diffuse);
                color += float3(0.8,0.92,1) * (specular * 0.85 + broadHighlight * 0.12 + rim * 0.12);
                Output output;
                output.color = float4(color,1);
                float4 clip = mul(UNITY_MATRIX_VP, float4(p,1));
                output.depth = clip.z / clip.w;
                if (UNITY_NEAR_CLIP_VALUE < 0) output.depth = output.depth * 0.5 + 0.5;
                return output;
            }
            ENDHLSL
        }
    }
}
