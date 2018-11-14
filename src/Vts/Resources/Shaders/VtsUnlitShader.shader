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

			#include "VtsCommon.cginc"
			#pragma multi_compile __ VTS_ATMOSPHERE
			#include "VtsAtmosphere.cginc"

			struct vIn
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				VTS_VIN_UV
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				VTS_V2F_COMMON
				VTS_V2F_CLIP
			};

			struct fOut
			{
				float4 color : SV_Target;
			};

			VTS_UNI_SAMP
			VTS_UNI_COMMON
			VTS_UNI_CLIP

			v2f vert(vIn v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.viewPos = UnityObjectToViewPos(v.vertex);
				VTS_VERT_UV(v,o)
				VTS_VERT_CLIP(v,o)
				return o;
			}

			fOut frag(v2f i)
			{
				VTS_FRAG_CLIP(i)

				fOut o;
				VTS_FRAG_COMMON(i,o)

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

			#include "VtsCommon.cginc"

			struct vIn
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				VTS_VIN_UV
			};

			struct v2f
			{
				VTS_V2F_CLIP
				V2F_SHADOW_CASTER;
			};

			VTS_UNI_CLIP

			v2f vert(vIn v)
			{
				v2f o;
				VTS_VERT_CLIP(v,o)
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				VTS_FRAG_CLIP(i)
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}
