Shader "Custom/GrassShader"
{
    Properties
    {
        _Billboard("Billboard Enabled", Int) = 1
        
        _AOColor("Ambient Occlusion Color", Color) = (1,1,1,1)
        _DiffuseLowerColor("Lower Color", Color) = (1,1,1,1)
        _DiffuseUpperColor("Upper Color", Color) = (1,1,1,1)
        _TipColor("Tip Color", Color) = (1,1,1,1)
        
        _AOEnd("Ambient Occlusion Percent", float) = 0.1
        _DiffuseLowerEnd("Diffuse Lower End Percent", float) = 0.5
        _DiffuseUpperEnd("Diffuse Upper End Percent", float) = 0.5
        _MaxGrassHeight ("Max Grass Height", float) = 1
        
        _GrassSwayTexture("Grass Sway Render Texture", 2D) = "white" {}
        _TimeScale("Sway Speed", Float) = 1
        _SwayStrength("Sway Strength", Float) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct GrassData
            {
                float3 position;
                float height;
                float2 worldUV;
                float checkRadius;
            };

            StructuredBuffer<GrassData> _GrassData;
            float _GrassScale;
            float3 _CameraPos;
            float4 _Color;
            float4 _AOColor;
            float4 _DiffuseLowerColor;
            float4 _DiffuseUpperColor;
            float4 _TipColor;
            float _AOEnd;
            float _DiffuseLowerEnd;
            float _MaxGrassHeight;
            float _DiffuseUpperEnd;
            sampler2D _GrassSwayTexture;
            float _TimeScale;
            float _SwayStrength;
            int _Billboard;

            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 worldUV : TEXCOORD1;
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v, uint id : SV_InstanceID)
            {
                v2f o;
                GrassData g = _GrassData[id];
                
                float3 localPos = v.vertex;
                
                float height01 = saturate(localPos.y / _MaxGrassHeight);

                // Sample sway texture in world space
                float swaySample = tex2Dlod(
                    _GrassSwayTexture,
                    float4(g.worldUV, 0, 0)
                ).r;

                // Remap [0,1] → [-1,1]
                float sway = (swaySample * 2.0 - 1.0);

                // Fade sway near the base
                float swayAmount = sway * height01 * _SwayStrength;
                
                localPos.x += swayAmount;
                
                // Scale the height by the grass height parameter
                localPos.y *= g.height;

                if (_Billboard == 1)
                {
                    float3 camDir = _WorldSpaceCameraPos - g.position;
                    camDir.y = 0;
                    camDir = normalize(camDir);

                    // World up
                    float3 up = float3(0, 1, 0);

                    // Billboard basis
                    float3 right = normalize(cross(up, camDir));
                    float3 forward = cross(right, up);

                    // Rotate local XZ into camera-facing orientation
                    float3 billboardOffset =
                        right * localPos.x +
                        up * localPos.y +
                        forward * localPos.z;

                    localPos = billboardOffset;
                }
                
                // Translate to world position
                float3 worldPos = localPos + g.position;

                // Convert to clip space
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.color = _Color.rgb;
                
                float verticalUV = v.vertex.y / _MaxGrassHeight;

                o.uv = float2(0, verticalUV);
                o.worldUV = g.worldUV;
                
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float verticalPerc = i.uv.y;
                
                // Debug visualize the UV
                //return float4(i.uv.y , i.uv.y, i.uv.y , 1); 
                //return tex2D(_GrassSwayTexture, i.worldUV);
                
                float3 color;
                
                if (verticalPerc <= _AOEnd)
                {
                    // AO
                    float t = verticalPerc / _AOEnd;
                    color = lerp(_AOColor.rgb, _DiffuseLowerColor.rgb, t);
                }
                else if (verticalPerc <= _DiffuseLowerEnd)
                {
                    // Lower diffuse
                    float t = (verticalPerc - _AOEnd) / (_DiffuseLowerEnd - _AOEnd);
                    color = lerp(_DiffuseLowerColor.rgb, _DiffuseUpperColor.rgb, t);
                }
                else if (verticalPerc <= _DiffuseUpperEnd)
                {
                    // Upper diffuse
                    float t = (verticalPerc - _DiffuseLowerEnd) / (_DiffuseUpperEnd - _DiffuseLowerEnd);
                    color = lerp(_DiffuseUpperColor.rgb, _TipColor.rgb, t);
                }
                else
                {
                    // Tip
                    color = _TipColor.rgb;
                }
                
                
                return float4(color, 1);
            }
            ENDCG
        }
    }
}
