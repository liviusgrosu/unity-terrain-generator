Shader "Custom/Terrain" {
	Properties{
		testTexture("Texture", 2D) = "white" {}
		testScale("Scale", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		const static int maxLayerCount = 8;
		const static float epsilon = 1E-4;

		int layerCount;
		float3 baseColours[maxLayerCount];
		float baseStartHeights[maxLayerCount];
		float baseBlends[maxLayerCount];
		float baseColourStrength[maxLayerCount];
		float baseTextureScales[maxLayerCount];

		float minHeight;
		float maxHeight;

		sampler2D testTexture;
		float testScale;

		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		float inverseLerp(float a, float b, float value) {
			return saturate((value-a)/(b-a));
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
			for (int i = 0; i < layerCount; i ++) {
				float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);
				o.Albedo = o.Albedo * (1-drawStrength) + baseColours[i] * drawStrength;
			}

			float3 scaledWorldPos = IN.worldPos / testScale;
			float3 blendAxis = abs(IN.worldNormal);
			blendAxis /= blendAxis.x + blendAxis.y + blendAxis.z;
			float3 xProjection = tex2D(testTexture, scaledWorldPos.yz) * blendAxis.x;
			float3 yProjection = tex2D(testTexture, scaledWorldPos.xz) * blendAxis.y;
			float3 zProjection = tex2D(testTexture, scaledWorldPos.xy) * blendAxis.z;
			//o.Albedo = xProjection + yProjection + zProjection;
		}

		ENDCG
	}
	FallBack "Diffuse"
}
