Shader "Unlit/BrushPressure"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BrightnessAmount ("Brightness Amount", Range(0.5, 3.0)) = 1.0
        _ContrastAmount ("Contrast Amount", Range(0.5, 6.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"

            //Variables
            uniform sampler2D _MainTex;
            fixed4 _MainTex_ST;
            fixed _BrightnessAmount;
            fixed _ContrastAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float3 BrightnessContrastAdjustment(float3 color, float brightness, float contrast)
			{
				// luminance coefficient for getting luminance from the image
				float3 luminanceCoeff = float3(0.2125, 0.7154, 0.0721);

				// Brightness calculation
				float3 avgLum = float3(0.5, 0.5, 0.5);
				float3 brightnessColor = color * brightness;
				float intensityf = dot(brightnessColor, luminanceCoeff);
				float3 intensity = float3(intensityf, intensityf, intensityf);


                // Contrast calculation
				float3 contrastColor = lerp(avgLum, intensity, contrast);

				return contrastColor;
			}

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                col.rgb = BrightnessContrastAdjustment(col.rgb, _BrightnessAmount, _ContrastAmount);

                return col;
            }
            ENDCG
        }
    }
}
