Shader "Hidden/Illumetry/CopyShader"
{
    Properties
    {
        Source ("Source", 2D) = "white" {}
    }
    SubShader
    {

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct VertexInput {
                float4 position : POSITION;
            };

            struct VertexOutput {
                float4 pixelPosition : SV_POSITION;
                float2 screenPosition : TEXCOORD0;
                
            };

            VertexOutput vert (VertexInput input) {
                VertexOutput output;
                output.pixelPosition = input.position;
                output.screenPosition = input.position;
                return output;
            }


            sampler2D Source;
            sampler2D PreviousOverdriveFrame;
            float3 CameraPositionRelativeToFrame;


            fixed4 frag (VertexOutput input) : SV_Target {
                float2 uv = 0.5*input.screenPosition+0.5;
                return tex2D(Source, uv);
            }
            ENDCG
        }
    }
}
