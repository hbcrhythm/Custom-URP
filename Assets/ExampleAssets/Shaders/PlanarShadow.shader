Shader "Custom/PlanarShadow"
{
	Properties
	{
		_ShadowColor("ShadowColor",Color) = (0,0,0,1)
		_ShadowPlane("ShadowPlane", float) = 0
		[Toggle(_FADE)] _Fade("Fade", Float) = 0
		_ShadowFadeParams("ShadowFadeParams", Vector) = (0.0, 1.5, 0.7, 0.0)
		_ShadowInvLen("ShadowInvLen", float) = 0.22
		_ShadowFalloff("ShadowFalloff", Range(0,1)) = 0.5
	}
	
	SubShader
	{
		Pass
		{
			Tags{"LightMode" = "SRPDefaultUnlit"}
			Blend SrcAlpha  OneMinusSrcAlpha
			ZWrite Off
			Cull Back
			ColorMask RGB

			offset -1,0

			Stencil
			{
				Ref 0
				Comp Equal
				WriteMask 255
				ReadMask 255
				Pass Invert
				Fail Keep
				ZFail Keep
			}

			HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			#pragma shader_feature _FADE
			#pragma vertex vert
			#pragma fragment frag

            CBUFFER_START(UnityPerMaterial)
			
			float4 _ShadowColor;
			float _ShadowFalloff;
			float _ShadowPlane;
			float4 _ShadowFadeParams;
			float _ShadowInvLen;

			CBUFFER_END

			struct Attributes {
				float4 positionOS	: POSITION;
			};

			struct Varyings {
				float4 positionCS	: SV_POSITION;
				float4 color : COLOR;
			};

			float3 ShadowProjectPos(float4 positionOS, float3 lightDir)
			{
				float3 shadowPos;

				float3 worldPos = TransformObjectToWorld(positionOS.xyz);

				shadowPos.y = min(worldPos.y, _ShadowPlane);
				shadowPos.xz = worldPos.xz - lightDir.xz * max(0, worldPos.y - _ShadowPlane) / lightDir.y;

				return shadowPos;
			}

			Varyings vert(Attributes IN) {
				Varyings OUT;

				Light light = GetMainLight();

				float3 shadowPos = ShadowProjectPos(IN.positionOS, light.direction);

				OUT.positionCS = TransformWorldToHClip(shadowPos);

				//得到中心点世界坐标
				float3 center = float3(unity_ObjectToWorld[0].w, _ShadowPlane, unity_ObjectToWorld[2].w);

				OUT.color = _ShadowColor;
				
				#if defined(_FADE)
					float3 dis = distance(shadowPos, center);
					OUT.color.a = pow((1.0 - clamp(((sqrt(dot(dis, dis)) * _ShadowInvLen) - _ShadowFadeParams.x), 0.0, 1.0)), _ShadowFadeParams.y) * _ShadowFadeParams.z;			
				#else
					float falloff = 1 - saturate(distance(shadowPos, center) * _ShadowFalloff);
					OUT.color.a *= falloff;
				#endif

				return OUT;

			}

			half4 frag(Varyings IN) : SV_Target{
				return IN.color;
			}

			ENDHLSL
		}
	}
}