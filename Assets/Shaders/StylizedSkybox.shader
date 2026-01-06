Shader "Custom/StylizedSkybox"
{
    Properties
    {
        [Header(Sky Colors)]
        _TopColor ("Top Color", Color) = (0.4, 0.7, 1.0, 1)
        _HorizonColor ("Horizon Color", Color) = (0.9, 0.8, 0.7, 1)
        _BottomColor ("Bottom Color", Color) = (0.3, 0.4, 0.5, 1)
        
        [Header(Gradient)]
        _HorizonHeight ("Horizon Height", Range(-1, 1)) = 0.0
        _TopBlend ("Top Blend", Range(0.1, 2)) = 0.5
        _BottomBlend ("Bottom Blend", Range(0.1, 2)) = 0.5
        
        [Header(Sun)]
        _SunColor ("Sun Color", Color) = (1.0, 0.9, 0.7, 1)
        _SunSize ("Sun Size", Range(0, 0.2)) = 0.05
        _SunIntensity ("Sun Intensity", Range(0, 5)) = 2
        
        [Header(Clouds)]
        _EnableClouds ("Enable Clouds", Float) = 0
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudScale ("Cloud Scale", Range(1, 30)) = 8
        _CloudSpeed ("Cloud Speed", Range(0, 0.2)) = 0.02
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.5
        _CloudSoftness ("Cloud Softness", Range(0.01, 0.5)) = 0.2
        _CloudBrightness ("Cloud Brightness", Range(0.5, 2)) = 1
        _CloudMinHeight ("Cloud Min Height", Range(-0.2, 0.5)) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Background" 
            "RenderType" = "Background" 
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Cull Off
        ZWrite Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _HorizonColor;
                float4 _BottomColor;
                float _HorizonHeight;
                float _TopBlend;
                float _BottomBlend;
                float4 _SunColor;
                float _SunSize;
                float _SunIntensity;
                float _EnableClouds;
                float4 _CloudColor;
                float _CloudScale;
                float _CloudSpeed;
                float _CloudCoverage;
                float _CloudSoftness;
                float _CloudBrightness;
                float _CloudMinHeight;
            CBUFFER_END
            
            // Simple noise for clouds
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(
                    lerp(hash(i), hash(i + float2(1.0, 0.0)), u.x),
                    lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), u.x),
                    u.y
                );
            }
            
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.viewDir = input.positionOS.xyz;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.viewDir);
                float y = dir.y;
                
                // Gradient calculation
                float horizonY = y - _HorizonHeight;
                
                // Top gradient (horizon to sky)
                float topGradient = saturate(horizonY / _TopBlend);
                // Bottom gradient (horizon to ground)
                float bottomGradient = saturate(-horizonY / _BottomBlend);
                
                // Mix colors
                half3 skyColor = _HorizonColor.rgb;
                skyColor = lerp(skyColor, _TopColor.rgb, topGradient);
                skyColor = lerp(skyColor, _BottomColor.rgb, bottomGradient);
                
                // Sun direction (bulutlardan önce hesapla)
                Light mainLight = GetMainLight();
                float3 sunDir = mainLight.direction;
                float sunDot = dot(dir, sunDir);
                
                // Clouds (optional) - güneşten ÖNCE render et
                float cloudMask = 0;
                if (_EnableClouds > 0.5 && y > _CloudMinHeight)
                {
                    // Spherical projection
                    float2 cloudUV = dir.xz / (dir.y + 0.3) * _CloudScale;
                    cloudUV += _Time.y * _CloudSpeed * float2(1, 0.3);
                    
                    // Multi-octave noise
                    float cloudNoise = fbm(cloudUV);
                    
                    // Coverage ve softness kontrolu
                    float threshold = 1.0 - _CloudCoverage;
                    float clouds = smoothstep(threshold, threshold + _CloudSoftness, cloudNoise);
                    
                    // Horizon fade
                    float horizonFade = smoothstep(_CloudMinHeight, _CloudMinHeight + 0.3, y);
                    cloudMask = clouds * horizonFade;
                    
                    // Bulut rengi - gökyüzü rengini miras al (daha doğal)
                    half3 cloudCol = lerp(skyColor, _CloudColor.rgb * _CloudBrightness, 0.7);
                    
                    // Güneşe yakın bulutlar parlasın
                    float sunLit = saturate(sunDot * 0.5 + 0.5);
                    cloudCol += _SunColor.rgb * sunLit * 0.2;
                    
                    skyColor = lerp(skyColor, cloudCol, cloudMask * 0.85);
                }
                
                // Sun - bulutların ÜZERİNDE render et
                float sun = smoothstep(1.0 - _SunSize, 1.0, sunDot);
                // Bulut varsa güneşi hafifçe dim yap ama tamamen kapatma
                float sunIntensity = _SunIntensity * (1.0 - cloudMask * 0.5);
                skyColor += _SunColor.rgb * sun * sunIntensity;
                
                // Sun glow - bulutsuz alanlarda daha belirgin
                float sunGlow = smoothstep(0.7, 1.0, sunDot) * 0.3 * (1.0 - cloudMask * 0.3);
                skyColor += _SunColor.rgb * sunGlow;
                
                return half4(skyColor, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack Off
}
