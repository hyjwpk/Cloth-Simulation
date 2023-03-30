Shader "Custom/ClothShaderCPU" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		[Enum(tex,0,force,1,stress,2)]_Texture("Texture",Float) = 0
	}
		SubShader{
			Cull Off
			Tags { "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM
			#pragma surface surf Standard fullforwardshadows

			fixed3 HSVToRGB(fixed3 c)
			{
				float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
				fixed3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
				return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
			}

			sampler2D _MainTex;
			float _Texture;

			struct Input {
				float2 uv_MainTex;
			};

			void surf(Input IN, inout SurfaceOutputStandard o) {
				if (_Texture == 0) {
					half4 c = tex2D(_MainTex, IN.uv_MainTex);
					o.Albedo = c.rgb;
					o.Alpha = 0;
				}
				else
				{
					float force = force = IN.uv_MainTex.x;
					if (_Texture == 2)
						force = force / 1000;
					fixed4 c = fixed4(HSVToRGB(fixed3(0.5 + force, 1, 1)), 1);
					o.Albedo = c.rgb;
					o.Alpha = 0;
				}
			}
			ENDCG
		}
			FallBack "Diffuse"
}
