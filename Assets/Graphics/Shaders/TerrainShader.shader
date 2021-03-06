﻿Shader "Custom/TerrainShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Level0 ("Albedo (RGB)", 2D) = "white" {}
        _Level1 ("Albedo (RGB)", 2D) = "white" {}
        _Level2 ("Albedo (RGB)", 2D) = "white" {}
        _Level3 ("Albedo (RGB)", 2D) = "white" {}

        _Ocean ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _Level0;
        sampler2D _Level1;
        sampler2D _Level2;
        sampler2D _Level3;
        sampler2D _Ocean;

        struct Input
        {
            float4 xyze;
            float2 biomePos;
			float3 worldNormal;
        };

        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
        
		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.xyze = float4(v.texcoord.x, v.texcoord.y, v.texcoord1.x, v.texcoord1.y);
			o.biomePos = float2(v.texcoord2.x, v.texcoord2.y);
            //o.norm = abs(v.normal);
		}

		float blend(float val, float min, float max, float blendBot, float blendTop) {
			return saturate((val - min + blendBot/2) / blendBot) * saturate((-val + max + blendTop/2) / blendTop);
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float4 xyze = IN.xyze;
            float2 biomePos = IN.biomePos;
			float3 norm = abs(IN.worldNormal);

            float4 xProject;
            float4 yProject;
            float4 zProject;

            xProject = tex2D (_Level0, xyze.yz) * norm.x;
            yProject = tex2D (_Level0, xyze.xz) * norm.y;
            zProject = tex2D (_Level0, xyze.xy) * norm.z;
            fixed4 c0 = (xProject + yProject + zProject) * blend(xyze.w, 0.01, 0.1, 0.05, 0.2);
            xProject = tex2D (_Level1, xyze.yz) * norm.x;
            yProject = tex2D (_Level1, xyze.xz) * norm.y;
            zProject = tex2D (_Level1, xyze.xy) * norm.z;
            fixed4 c1 = (xProject + yProject + zProject) * blend(xyze.w, 0.1, 0.4, 0.2, 0.2);
            xProject = tex2D (_Level2, xyze.yz) * norm.x;
            yProject = tex2D (_Level2, xyze.xz) * norm.y;
            zProject = tex2D (_Level2, xyze.xy) * norm.z;
            fixed4 c2 = (xProject + yProject + zProject) * blend(xyze.w, 0.4, 0.8, 0.2, 0.2);
            xProject = tex2D (_Level3, xyze.yz) * norm.x;
            yProject = tex2D (_Level3, xyze.xz) * norm.y;
            zProject = tex2D (_Level3, xyze.xy) * norm.z;
            fixed4 c3 = (xProject + yProject + zProject) * blend(xyze.w, 0.8, 1, 0.2, 0.01);

            xProject = tex2D (_Ocean, xyze.yz) * norm.x;
            yProject = tex2D (_Ocean, xyze.xz) * norm.y;
            zProject = tex2D (_Ocean, xyze.xy) * norm.z;
            fixed4 cOcean = (xProject + yProject + zProject) * blend(xyze.w, -1, 0, 0, 0.01);

            o.Albedo = c0.rgb + c1.rgb + c2.rgb + c3.rgb + cOcean.rgb / 2;

        }
        ENDCG
    }
    FallBack "Diffuse"
}
