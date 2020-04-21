Shader "Unlit/BrushAngle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AngleTex1 ("AngleTex1", 2D) = "white" {}
        _AngleTex2 ("AngleTex2", 2D) = "black" {}
        _Angle1 ("Angle1", Range(0.0, 90.0)) = 0.0 
        _Angle2 ("Angle2", Range(0.0, 90.0)) = 90.0 
        _CurrentAngle ("CurrentAngle", Range(0.0, 90.0)) = 45.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            //Variables
            uniform sampler2D _MainTex;
            fixed4 _MainTex_ST;
            uniform sampler2D _AngleTex1;
            fixed4 _AngleTex1_ST;
            uniform sampler2D _AngleTex2;
            fixed4 _AngleTex2_ST;
            uniform float _Angle1;
            uniform float _Angle2;
            uniform float _CurrentAngle;

            fixed3 mixColor(v2f i)
            {
                fixed2 offsetUVAngle1 = i.uv + _AngleTex1_ST.zw;
                fixed2 offsetUVAngle2 = i.uv + _AngleTex2_ST.zw;

                fixed3 colorAngle1 = tex2D(_AngleTex1, offsetUVAngle1).xyz;
                fixed3 colorAngle2 = tex2D(_AngleTex2, offsetUVAngle2).xyz;

                float percent = ((_CurrentAngle - _Angle1) / (_Angle2 - _Angle1));

                percent = clamp(percent, 0.0, 1.0);

                fixed3 lerpedAngle = lerp(colorAngle1, colorAngle2, percent);

                return lerpedAngle;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col;
                col = fixed4(mixColor(i), 1.f);
                //col = fixed4((_CurrentAngle / 90), 0.f, 0.f, 1.f);
                return col;
            }
            ENDCG
        }
    }
}
