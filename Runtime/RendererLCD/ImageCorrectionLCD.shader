Shader "Hidden/Illumetry/ImageCorrectionLCD" {
    Properties {
        CurrentFrame ("CurrentFrame", 2D) = "white" {}
        PreviousOverdriveFrame ("PreviousOverdriveFrame", 2D) = "white" {}
        UseLimitsCorrection("UseLimitsCorrection", Int) = 0
        UseGammaCorrection("UseGammaCorrection", Int) = 0
        UseOverdriveCorrection("UseOverdriveCorrection", Int) = 0
        //Unit: half of vertical size of the screen
        CameraPositionRelativeToFrame ("CameraPositionRelativeToFrame", Vector) = (0, 0, -2, 0)
        Attenuation("Attenuation", Vector) = (1, 1, 1)
        CommonGamma("CommonGamma", Float) = 1
    }
    SubShader {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // #pragma enable_d3d11_debug_symbols

            #include "UnityCG.cginc"

            struct VertexInput {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput {
                float4 pixelPosition : SV_POSITION;
                float2 screenPosition : TEXCOORD0;
                
            };

            VertexOutput vert (VertexInput input) {
                VertexOutput output;
                output.pixelPosition = input.position;
                #if UNITY_UV_STARTS_AT_TOP
                    output.pixelPosition.y *= -1.0;
                #endif
                output.screenPosition = input.position;
                return output;
            }
 
            float3 Illumetry_GammaFunction_LinearX;
            float3 Illumetry_GammaFunction_LinearY;
            float3 Illumetry_GammaFunction_LinearZ;
            float3 Illumetry_GammaFunction_SquaredX;
            float3 Illumetry_GammaFunction_SquaredY;
            float3 Illumetry_GammaFunction_SquaredZ;
            float3 Illumetry_GammaFunction_CrossX;
            float3 Illumetry_GammaFunction_CrossY;
            float3 Illumetry_GammaFunction_CrossZ;

            float2 Illumetry_GammaLimits_LinearX;
            float2 Illumetry_GammaLimits_LinearY;
            float2 Illumetry_GammaLimits_LinearZ;
            float2 Illumetry_GammaLimits_SquaredX;
            float2 Illumetry_GammaLimits_SquaredY;
            float2 Illumetry_GammaLimits_SquaredZ;
            float2 Illumetry_GammaLimits_CrossX;
            float2 Illumetry_GammaLimits_CrossY;
            float2 Illumetry_GammaLimits_CrossZ;

            float3 GrayPosition(float3 pixelToCamera) {
                float3 squared = pixelToCamera*pixelToCamera;
                float3 cross = pixelToCamera*pixelToCamera.yzx;

                return 
                    +Illumetry_GammaFunction_LinearX*pixelToCamera.x
                    +Illumetry_GammaFunction_LinearY*pixelToCamera.y
                    +Illumetry_GammaFunction_LinearZ*pixelToCamera.z

                    +Illumetry_GammaFunction_SquaredX*squared.x
                    +Illumetry_GammaFunction_SquaredY*squared.y
                    +Illumetry_GammaFunction_SquaredZ*squared.z

                    +Illumetry_GammaFunction_CrossX*cross.x
                    +Illumetry_GammaFunction_CrossY*cross.y
                    +Illumetry_GammaFunction_CrossZ*cross.z;
            }

            float2 Limits(float3 pixelToCamera) {
                float3 squared = pixelToCamera * pixelToCamera;
                float3 cross = pixelToCamera * pixelToCamera.yzx;

                float2 result =
                    + Illumetry_GammaLimits_LinearX * pixelToCamera.x
                    + Illumetry_GammaLimits_LinearY * pixelToCamera.y
                    + Illumetry_GammaLimits_LinearZ * pixelToCamera.z

                    + Illumetry_GammaLimits_SquaredX * squared.x
                    + Illumetry_GammaLimits_SquaredY * squared.y
                    + Illumetry_GammaLimits_SquaredZ * squared.z

                    + Illumetry_GammaLimits_CrossX * cross.x
                    + Illumetry_GammaLimits_CrossY * cross.y
                    + Illumetry_GammaLimits_CrossZ * cross.z;
                return clamp(result, 0, 1);
            }

            sampler3D Illumetry_OverdriveMapR, Illumetry_OverdriveMapG, Illumetry_OverdriveMapB;

            float3 OverdriveRGB(float3 Current, float3 PreviousOverdrive, float screenY) {
                return float3(
                    tex3D(Illumetry_OverdriveMapR, float3(PreviousOverdrive.x, Current.x, screenY)).x,
                    tex3D(Illumetry_OverdriveMapG, float3(PreviousOverdrive.y, Current.y, screenY)).x,
                    tex3D(Illumetry_OverdriveMapB, float3(PreviousOverdrive.z, Current.z, screenY)).x
                );
            }           

            sampler2D CurrentFrame;
            sampler2D PreviousOverdriveFrame;
            float3 CameraPositionRelativeToFrame;
            int UseLimitsCorrection;
            int UseGammaCorrection;
            int UseOverdriveCorrection;

            float3 Attenuation;
            float CommonGamma;

            fixed4 frag(VertexOutput input) : SV_Target{
                const float rangeMultiplier = 1.0;

                float aspect = ddy(input.screenPosition.y) / ddx(input.screenPosition.x);
                float2 uv = 0.5*input.screenPosition + 0.5; 
                float2 pixelInFrame = input.screenPosition*float2(aspect, 1);
                float3 pixelToCamera = CameraPositionRelativeToFrame - float3(pixelInFrame, 0);
                
                float3 currentFrameColor = Attenuation*tex2D(CurrentFrame, uv).xyz;
                currentFrameColor = pow(currentFrameColor, CommonGamma)*rangeMultiplier;
                float3 previousOverdriveFrameColor = tex2D(PreviousOverdriveFrame, uv).xyz;
                
                float3 output;
                if (UseLimitsCorrection == 1) {
                    float3 gammaFuncArgs = normalize(pixelToCamera);
                    float2 limits = Limits(gammaFuncArgs);
                    float3 limitsCorrectedColor = limits.x + (limits.y - limits.x) * currentFrameColor.xyz;
                    if(UseGammaCorrection == 1) {
                        float3 grayPositionInsideLimits = (GrayPosition(gammaFuncArgs) - limits.x) / (limits.y - limits.x);
                        float3 gammaCorrection = -log2(grayPositionInsideLimits);
                        float3 gammaCorrectedColor = pow(limitsCorrectedColor, gammaCorrection) * (limits.y - limits.x) + limits.x;

                        // CHECKER
                        //float x = input.pixelPosition.x;
                        //float y = input.pixelPosition.y;
                        //if ((round(x / 10) + round(y / 10)) % 2 == 0) {
                        //    float intensity = lerp(limits.x, limits.y, (x + y) % 2);
                        //    return float4(intensity, intensity, intensity, 1);
                        //}
                        //return fixed4(pow(0.5, gammaCorrection) * (limits.y - limits.x) + limits.x, 1); // test gamma correction
                        if (UseOverdriveCorrection == 1) {
                            output = OverdriveRGB(gammaCorrectedColor.xyz, previousOverdriveFrameColor.xyz, uv.y);
                        }
                        else {
                            output = gammaCorrectedColor.xyz;
                        }
                    } else {
                        if (UseOverdriveCorrection == 1) {
                            output = OverdriveRGB(limitsCorrectedColor.xyz, previousOverdriveFrameColor.xyz, uv.y);
                        }
                        else {
                            output = limitsCorrectedColor.xyz;
                        }
                    }
                }
                else {
                    if (UseOverdriveCorrection == 1) {
                        output = OverdriveRGB(currentFrameColor.xyz, previousOverdriveFrameColor.xyz, uv.y);
                    }
                    else {
                        output = currentFrameColor;
                    }
                }
                return float4(output, 1);
            }
            ENDCG
        }
    }
}
