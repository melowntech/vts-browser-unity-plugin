Shader "Vts/UnlitShader"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct vIn
			{
				float4 vertex : POSITION;
				float2 uvInternal : TEXCOORD0;
				float2 uvExternal : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uvTex : TEXCOORD0;
				float2 uvClip : TEXCOORD1;
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

			v2f vert (vIn i)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(i.vertex);
				o.uvTex = mul((float3x3)_UvMat, float3(_Flags.w > 0 ? i.uvExternal : i.uvInternal, 1.0)).xy;
				o.uvClip = i.uvExternal;
				return o;
			}

			fOut frag (v2f i)
			{
				fOut o;

				// texture color
				o.color = tex2D(_MainTex, i.uvTex);
				if (_Flags.y > 0)
					o.color = o.color.rrra; // monochromatic texture

				// uv clipping
				if (   i.uvClip.x < _UvClip.x
					|| i.uvClip.y < _UvClip.y
					|| i.uvClip.x > _UvClip.z
					|| i.uvClip.y > _UvClip.w)
					discard;

				// mask
				if (_Flags.x > 0)
				{
					if (tex2D(_MaskTex, i.uvTex).r < 0.5)
						discard;
				}

				// uniform tint
				o.color *= _Color;

				return o;
			}
			ENDCG
		}
	}
}
