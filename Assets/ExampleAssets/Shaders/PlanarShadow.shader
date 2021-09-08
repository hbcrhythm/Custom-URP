Shader "Custom/PlanarShadow"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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

			//CGPROGRAM
			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#pragma vertex vert
			#pragma fragment frag

			CBUFFER_START(UnityPerFrame)
	        float4x4 unity_MatrixVP;
            CBUFFER_END

            CBUFFER_START(UnityPerDraw)
	        float4x4 unity_ObjectToWorld;
            CBUFFER_END
            #define UNITY_MATRIX_M unity_ObjectToWorld

            CBUFFER_START(UnityPerMaterial)
            
			float4 _ShadowPlane;
			float4 _ShadowProjDir;
			float4 _WorldPos;
			float _ShadowInvLen;
			float4 _ShadowFadeParams;
			float _ShadowFalloff;

			CBUFFER_END

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 xlv_TEXCOORD0 : TEXCOORD0;
				float3 xlv_TEXCOORD1 : TEXCOORD1;
			};

			v2f vert(appdata v)
			{
				v2f o;
				float3 lightdir = normalize(_ShadowProjDir);
				//float3 worldpos = mul(unity_ObjectToWorld, v.vertex).xyz;
				//float3 worldpos = TransformObjectToWorld(v.vertex.xyz);
				float4 worldPos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0));
				_ShadowPlane = float4(0,10,0,0);

				// _ShadowPlane.w = p0 * n  // 平面的w分量就是p0 * n
				float distance = (_ShadowPlane.w - dot(_ShadowPlane.xyz, worldPos.xyz)) / dot(_ShadowPlane.xyz, lightdir.xyz);
				worldPos = worldPos + distance * float4(lightdir.xyz, 0.0);
				//o.vertex = mul(unity_MatrixVP, float4(worldpos, 1.0));
				//o.vertex = TransformWorldToHClip(float4(worldpos, 1.0));
				o.vertex = mul(unity_MatrixVP, worldPos);
				o.xlv_TEXCOORD0 = _WorldPos.xyz;
				o.xlv_TEXCOORD1 = worldPos;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float3 posToPlane_2 = (i.xlv_TEXCOORD0 - i.xlv_TEXCOORD1);
				float4 color;
				color.xyz = float3(0.0, 0.0, 0.0);

				// 下面两种阴影衰减公式都可以使用(当然也可以自己写衰减公式)
				// 王者荣耀的衰减公式
				// color.w = (pow((1.0 - clamp(((sqrt(dot(posToPlane_2, posToPlane_2)) * _ShadowInvLen) - _ShadowFadeParams.x), 0.0, 1.0)), _ShadowFadeParams.y) * _ShadowFadeParams.z);

				// 另外的阴影衰减公式
				color.w = 1.0 - saturate(distance(i.xlv_TEXCOORD0, i.xlv_TEXCOORD1) * _ShadowFalloff);
				// color.w = 1.0;

				return color;
			}
			//ENDCG
			ENDHLSL
		}
	}
}
