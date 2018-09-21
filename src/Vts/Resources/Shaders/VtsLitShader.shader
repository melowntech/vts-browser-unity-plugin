Shader "Vts/LitShader"
{
	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
		}
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
			#include "Lighting.cginc"

			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#include "AutoLight.cginc"

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
				float4 pos : SV_POSITION;
				float3 normal : NORMAL;
				float3 viewPos : TEXCOORD0;
				float2 uvTex : TEXCOORD1;
				float4 clip : SV_ClipDistance0;
				SHADOW_COORDS(2)
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
				o.pos = UnityObjectToClipPos(i.vertex);
				o.viewPos = UnityObjectToViewPos(i.vertex);
				o.normal = UnityObjectToWorldNormal(i.normal);
				o.uvTex = mul((float3x3)_UvMat, float3(_Flags.w > 0 ? i.uvExternal : i.uvInternal, 1.0)).xy;
				o.clip[0] = (i.uvExternal[0] - _UvClip[0]) * +1.0;
				o.clip[1] = (i.uvExternal[1] - _UvClip[1]) * +1.0;
				o.clip[2] = (i.uvExternal[0] - _UvClip[2]) * -1.0;
				o.clip[3] = (i.uvExternal[1] - _UvClip[3]) * -1.0;
				TRANSFER_SHADOW(o)
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

				// shadow
				o.color.rgb *= SHADOW_ATTENUATION(i);

				// atmosphere
				float atmDensity = vtsAtmDensity(i.viewPos);
				o.color = vtsAtmColor(atmDensity, o.color);

				return o;
			}
			ENDCG
		}

		UsePass "Vts/UnlitShader/SHADOWCASTER"
	}
}


