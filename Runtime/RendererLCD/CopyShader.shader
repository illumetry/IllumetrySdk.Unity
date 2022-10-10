Shader "Hidden/Illumetry/CopyShader"
{
    Properties
    {
        Source ("Source", 2D) = "white" {}
    }

    CGINCLUDE
    #pragma vertex vert
    #pragma fragment frag

    #include "UnityCG.cginc"


    sampler2D Source;
    sampler2D PreviousOverdriveFrame;
    float3 CameraPositionRelativeToFrame;

    float3 gamma_sRGB(float3 inp) {
        return 1.055f*pow(inp, 1/2.4) - 0.055;
    }

    float3 gamma_sRGB_inv(float3 inp) {
        return pow((inp + 0.055)/1.055, 2.4);
    }

    struct VertexInput {
        float4 position : POSITION;
    };

    struct VertexOutput {
        float4 pixelPosition : SV_POSITION;
        float2 screenPosition : TEXCOORD0;
    };

    VertexOutput vert(VertexInput input) {
        VertexOutput output;
        output.pixelPosition = input.position;
        output.screenPosition = input.position;
        return output;
    }
    ENDCG

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        // for gamma color space
        Pass
        {
            CGPROGRAM
            fixed4 frag (VertexOutput input) : SV_Target {
                float2 uv = 0.5*input.screenPosition+0.5;
                return float4(tex2D(Source, uv).xyz, 1.0);
            }
            ENDCG
        }

        // for linear color space
        Pass
        {
            CGPROGRAM
            fixed4 frag(VertexOutput input) : SV_Target {
                float2 uv = 0.5*input.screenPosition+0.5;
                return float4(gamma_sRGB_inv(tex2D(Source, uv).xyz), 1.0);
            }
            ENDCG
        }
    }
}
