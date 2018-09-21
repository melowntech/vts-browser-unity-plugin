Shader "Vts/UnlitShader"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			#pragma multi_compile __ VTS_ATMOSPHERE
			#include "VtsAtmosphereShader.cginc"

			struct vIn
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uvInternal : TEXCOORD0;
				float2 uvExternal : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 viewPos : TEXCOORD0;
				float2 uvTex : TEXCOORD1;
				float4 clip : SV_ClipDistance0;
			};

			struct fOut
			{
				float4 color : SV_Target;
			};

			sampler2D _MainTex;
			sampler2D _MaskTex;

			float4x4 _UvMat;
			float4 _UvClip;
			float4 _Color;
			float4 _Flags; // mask, monochromatic, flat shading, uv source

			v2f vert(vIn i)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(i.vertex);
				o.viewPos = UnityObjectToViewPos(i.vertex);
				o.uvTex = mul((float3x3)_UvMat, float3(_Flags.w > 0 ? i.uvExternal : i.uvInternal, 1.0)).xy;
				o.clip[0] = (i.uvExternal[0] - _UvClip[0]) * +1.0;
				o.clip[1] = (i.uvExternal[1] - _UvClip[1]) * +1.0;
				o.clip[2] = (i.uvExternal[0] - _UvClip[2]) * -1.0;
				o.clip[3] = (i.uvExternal[1] - _UvClip[3]) * -1.0;
				return o;
			}

			fOut frag(v2f i)
			{
				fOut o;

				// texture color
				o.color = tex2D(_MainTex, i.uvTex);

				// mask
				if (_Flags.x > 0)
				{
					if (tex2D(_MaskTex, i.uvTex).r < 0.5)
						discard;
				}

				// monochromatic texture
				if (_Flags.y > 0)
					o.color = o.color.rrra;

				// uniform tint
				o.color *= _Color;

				// atmosphere
				float atmDensity = vtsAtmDensity(i.viewPos);
				o.color = vtsAtmColor(atmDensity, o.color);

				return o;
			}
			ENDCG
		}

		Pass
		{
			Name "SHADOWCASTER"

			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"

			struct vIn
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uvInternal : TEXCOORD0;
				float2 uvExternal : TEXCOORD1;
			};

			struct v2f
			{
				float4 clip : SV_ClipDistance0;
				V2F_SHADOW_CASTER;
			};

			float4 _UvClip;

			v2f vert(vIn v)
			{
				v2f o;
				o.clip[0] = (v.uvExternal[0] - _UvClip[0]) * +1.0;
				o.clip[1] = (v.uvExternal[1] - _UvClip[1]) * +1.0;
				o.clip[2] = (v.uvExternal[0] - _UvClip[2]) * -1.0;
				o.clip[3] = (v.uvExternal[1] - _UvClip[3]) * -1.0;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}
