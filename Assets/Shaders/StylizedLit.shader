Shader "Custom/StylizedLit"
{
    Properties
    {
        [Header(Base)]
        _BaseMap ("Albedo (RGB)", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        
        [Header(Stylized Lighting)]
        _ShadowColor ("Shadow Color", Color) = (0.55, 0.5, 0.6, 1)
        _ShadowSoftness ("Shadow Softness", Range(0.01, 1)) = 0.3
        _ShadowOffset ("Shadow Offset", Range(-0.5, 0.5)) = 0
        _ShadowBrightness ("Shadow Brightness", Range(0, 1)) = 0.4
        
        [Header(Rim Light)]
        [Toggle(_RIMLIGHT)] _EnableRim ("Enable Rim Light", Float) = 1
        _RimColor ("Rim Color", Color) = (1, 0.9, 0.8, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.5
        _RimSoftness ("Rim Softness", Range(0.01, 1)) = 0.3
        
        [Header(Ambient)]
        _AmbientColor ("Ambient Color", Color) = (0.7, 0.65, 0.6, 1)
        _AmbientIntensity ("Ambient Intensity", Range(0, 1)) = 0.4
        
        [Header(Fog Blend)]
        [Toggle(_FOGBLEND)] _EnableFogBlend ("Enable Distance Fog Blend", Float) = 1
        _FogColor ("Fog Color", Color) = (0.75, 0.65, 0.55, 1)
        _FogStart ("Fog Start Distance", Float) = 20
        _FogEnd ("Fog End Distance", Float) = 80
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "StylizedForward"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma shader_feature_local _RIMLIGHT
            #pragma shader_feature_local _FOGBLEND
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
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
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float fogCoord : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ShadowColor;
                float _ShadowSoftness;
                float _ShadowOffset;
                float _ShadowBrightness;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _RimSoftness;
                float4 _AmbientColor;
                float _AmbientIntensity;
                float4 _FogColor;
                float _FogStart;
                float _FogEnd;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.shadowCoord = GetShadowCoord(posInputs);
                output.fogCoord = ComputeFogFactor(posInputs.positionCS.z);
                
                return output;
            }
            
            // Soft step function for stylized lighting
            float SoftStep(float value, float edge, float softness)
            {
                return smoothstep(edge - softness, edge + softness, value);
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // Sample base texture
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Get main light
                Light mainLight = GetMainLight(input.shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                
                // Calculate NdotL with offset for stylized control
                float NdotL = dot(normalWS, lightDir);
                float adjustedNdotL = NdotL * 0.5 + 0.5 + _ShadowOffset;
                
                // Stylized shadow with soft edge
                float shadow = SoftStep(adjustedNdotL, 0.5, _ShadowSoftness);
                shadow *= mainLight.shadowAttenuation;
                
                // Blend between shadow color and lit color
                float3 litColor = albedo.rgb * mainLight.color;
                // Shadow color şimdi brightness ile kontrol ediliyor
                float3 shadowColor = albedo.rgb * _ShadowColor.rgb * (0.5 + _ShadowBrightness);
                float3 diffuse = lerp(shadowColor, litColor, shadow);
                
                // Ambient contribution
                float3 ambient = albedo.rgb * _AmbientColor.rgb * _AmbientIntensity;
                
                // Rim lighting
                float3 rim = float3(0, 0, 0);
                #ifdef _RIMLIGHT
                {
                    float NdotV = 1.0 - saturate(dot(normalWS, viewDirWS));
                    float rimRaw = pow(NdotV, _RimPower);
                    // Apply softness using smoothstep
                    float rimFactor = smoothstep(0.5 - _RimSoftness, 0.5 + _RimSoftness, rimRaw);
                    // Rim is stronger on lit side
                    rimFactor *= saturate(NdotL + 0.5);
                    rim = _RimColor.rgb * rimFactor * _RimIntensity;
                }
                #endif
                
                // Combine lighting
                float3 finalColor = diffuse + ambient + rim;
                
                // Distance fog blend (custom fog - shader için)
                #ifdef _FOGBLEND
                {
                    float dist = distance(input.positionWS, _WorldSpaceCameraPos);
                    float fogFactor = saturate((dist - _FogStart) / (_FogEnd - _FogStart));
                    finalColor = lerp(finalColor, _FogColor.rgb, fogFactor);
                }
                #endif
                
                // Unity evrensel fog (Lighting > Environment > Fog)
                finalColor = MixFog(finalColor, input.fogCoord);
                
                return half4(finalColor, albedo.a);
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            float3 _LightDirection;
            
            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                
                output.positionCS = positionCS;
                return output;
            }
            
            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
        
        // Depth pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
        
        // DepthNormals pass - required for proper DoF
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            
            ZWrite On
            
            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            
            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                return half4(normalWS * 0.5 + 0.5, 0);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
