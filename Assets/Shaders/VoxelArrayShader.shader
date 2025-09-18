Shader "Custom/VoxelArrayShader"
{
    Properties
    {
        _MainTex ("Texture Array", 2DArray) = "white" {}
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
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv3 : TEXCOORD2;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float texIndex : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            UNITY_DECLARE_TEX2DARRAY(_MainTex);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.texIndex = v.uv3.x;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Debug: показуємо індекс як колір (закоментовано)
                // float index = i.texIndex;
                // return fixed4(index/50.0, index/50.0, index/50.0, 1.0);
                
                // Реальне текстурування з Texture2DArray
                return UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(i.uv, i.texIndex));
            }
            ENDCG
        }
    }
} 