Shader "Custom/InteractiveGrass"
{
    Properties
    {
        _BaseColor ("Base Color (Bottom)", Color) = (0.1, 0.3, 0.1, 1)
        _TipColor ("Tip Color (Top)", Color) = (0.3, 0.6, 0.2, 1)
        _MainTex ("Texture", 2D) = "white" {}
        
        [Header(Wind Settings)]
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.0
        _WindStrength ("Wind Strength", Range(0, 1)) = 0.3
        _WindFrequency ("Wind Frequency", Range(0, 5)) = 1.0
        
        [Header(Interaction Settings)]
        _BendRadius ("Bend Radius", Range(0, 5)) = 1.5
        _BendStrength ("Bend Strength", Range(0, 2)) = 0.8
        _BendFalloff ("Bend Falloff", Range(0.1, 3)) = 1.0
        
        [Header(Grass Settings)]
        _GrassHeight ("Grass Height Reference", Range(0, 2)) = 0.5
        [Toggle] _UseUV ("Use UV for Height (recommended)", Float) = 1
        [Toggle] _InvertHeight ("Invert Height", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        
        Cull Off
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float heightFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _TipColor;
                float _WindSpeed;
                float _WindStrength;
                float _WindFrequency;
                float _BendRadius;
                float _BendStrength;
                float _BendFalloff;
                float _GrassHeight;
                float _UseUV;
                float _InvertHeight;
                float _Cutoff;
            CBUFFER_END
            
            // Global properties set by GrassInteractionManager
            float4 _GrassInteractorPositions[10];
            int _GrassInteractorCount;
            
            float3 ApplyWind(float3 positionWS, float heightFactor, float interactionStrength)
            {
                // Reduce wind effect when grass is being bent by character
                float windMultiplier = 1.0 - saturate(interactionStrength * 2.0);
                
                float windTime = _Time.y * _WindSpeed;
                float windX = sin(windTime + positionWS.x * _WindFrequency) * _WindStrength * windMultiplier;
                float windZ = cos(windTime * 0.7 + positionWS.z * _WindFrequency * 0.8) * _WindStrength * 0.5 * windMultiplier;
                
                positionWS.x += windX * heightFactor;
                positionWS.z += windZ * heightFactor;
                
                return positionWS;
            }
            
            void ApplyInteraction(inout float3 positionWS, float heightFactor, out float interactionStrength)
            {
                float3 totalBend = float3(0, 0, 0);
                interactionStrength = 0;
                
                for (int i = 0; i < _GrassInteractorCount; i++)
                {
                    float3 interactorPos = _GrassInteractorPositions[i].xyz;
                    float interactorRadius = _GrassInteractorPositions[i].w;
                    
                    if (interactorRadius <= 0) continue;
                    
                    float2 diff = positionWS.xz - interactorPos.xz;
                    float dist = length(diff);
                    float effectRadius = _BendRadius * interactorRadius;
                    
                    if (dist < effectRadius)
                    {
                        float bendFactor = 1.0 - saturate(pow(dist / effectRadius, _BendFalloff));
                        float2 bendDir = normalize(diff);
                        
                        totalBend.xz += bendDir * bendFactor * _BendStrength * heightFactor;
                        totalBend.y -= bendFactor * _BendStrength * heightFactor * 0.3;
                        
                        interactionStrength = max(interactionStrength, bendFactor);
                    }
                }
                
                positionWS += totalBend;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                // Calculate height factor - use UV.y or object Y position
                float heightFactor;
                if (_UseUV > 0.5)
                {
                    heightFactor = input.uv.y;
                }
                else
                {
                    heightFactor = saturate(input.positionOS.y / _GrassHeight);
                }
                
                // Optionally invert the height factor
                if (_InvertHeight > 0.5)
                {
                    heightFactor = 1.0 - heightFactor;
                }
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // First calculate interaction to get the strength
                float interactionStrength;
                ApplyInteraction(positionWS, heightFactor, interactionStrength);
                
                // Apply wind with reduced strength based on interaction
                positionWS = ApplyWind(positionWS, heightFactor, interactionStrength);
                
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.heightFactor = heightFactor;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Alpha cutoff
                clip(texColor.a - _Cutoff);
                
                // Gradient color based on height
                half4 grassColor = lerp(_BaseColor, _TipColor, input.heightFactor);
                half4 finalColor = texColor * grassColor;
                
                // Simple lighting
                Light mainLight = GetMainLight();
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, mainLight.direction) * 0.5 + 0.5);
                
                finalColor.rgb *= NdotL * mainLight.color;
                
                // Add ambient
                float3 ambient = SampleSH(normalWS);
                finalColor.rgb += ambient * 0.3;
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _TipColor;
                float _WindSpeed;
                float _WindStrength;
                float _WindFrequency;
                float _BendRadius;
                float _BendStrength;
                float _BendFalloff;
                float _GrassHeight;
                float _UseUV;
                float _InvertHeight;
                float _Cutoff;
            CBUFFER_END
            
            float4 _GrassInteractorPositions[10];
            int _GrassInteractorCount;
            
            float3 ApplyWindShadow(float3 positionWS, float heightFactor)
            {
                float windTime = _Time.y * _WindSpeed;
                float windX = sin(windTime + positionWS.x * _WindFrequency) * _WindStrength;
                float windZ = cos(windTime * 0.7 + positionWS.z * _WindFrequency * 0.8) * _WindStrength * 0.5;
                
                positionWS.x += windX * heightFactor;
                positionWS.z += windZ * heightFactor;
                
                return positionWS;
            }
            
            float3 ApplyInteractionShadow(float3 positionWS, float heightFactor)
            {
                float3 totalBend = float3(0, 0, 0);
                
                for (int i = 0; i < _GrassInteractorCount; i++)
                {
                    float3 interactorPos = _GrassInteractorPositions[i].xyz;
                    float interactorRadius = _GrassInteractorPositions[i].w;
                    
                    if (interactorRadius <= 0) continue;
                    
                    float2 diff = positionWS.xz - interactorPos.xz;
                    float dist = length(diff);
                    float effectRadius = _BendRadius * interactorRadius;
                    
                    if (dist < effectRadius)
                    {
                        float bendFactor = 1.0 - saturate(pow(dist / effectRadius, _BendFalloff));
                        float2 bendDir = normalize(diff);
                        
                        totalBend.xz += bendDir * bendFactor * _BendStrength * heightFactor;
                        totalBend.y -= bendFactor * _BendStrength * heightFactor * 0.3;
                    }
                }
                
                return positionWS + totalBend;
            }
            
            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float heightFactor = saturate(input.positionOS.y / _GrassHeight);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                positionWS = ApplyWindShadow(positionWS, heightFactor);
                positionWS = ApplyInteractionShadow(positionWS, heightFactor);
                
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Simple shadow bias without GetMainLight
                float3 lightDir = normalize(float3(0.5, 1, 0.3));
                output.positionCS = TransformWorldToHClip(positionWS + normalWS * 0.001);
                
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }
            
            half4 ShadowFrag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(texColor.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
