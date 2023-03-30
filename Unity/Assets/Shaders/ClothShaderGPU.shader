Shader "Custom/ClothShaderGPU"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Transparent" }
		LOD 100
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			fixed3 HSVToRGB(fixed3 c)
			{
				float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
				fixed3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
				return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
			}

			struct v2f
			{
				float3 normal : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float2 tex : TEXCOORD1;
				float3 force: TEXCOORD2;
			};

			StructuredBuffer<float4> position;
			StructuredBuffer<float4> normal;
			StructuredBuffer<float3> force;
			StructuredBuffer<float2> uv;

			sampler2D _MainTex;

			v2f vert(uint id : SV_VertexID)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(position[id]);
				o.normal = normal[id].xyz;
				o.tex = uv[id].xy;
				o.force = force[id];
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed3 color = fixed3(length(i.force) / 1000,1,1);
				color = HSVToRGB(color);
				fixed4 c = fixed4(color, 1);
				fixed4 tex = tex2D(_MainTex, i.tex);
				return abs(dot(i.normal, _WorldSpaceLightPos0.xyz) * _LightColor0) * tex;
			}

			ENDCG
		}
	}
}
