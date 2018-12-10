
#define VTSH_MANUAL_CLIP 1 // surface shaders do not preserve SV_ClipDistance0
#include "VtsCommon.cginc"

struct Input
{
	VTS_V2F_COMMON
	VTS_V2F_CLIP
};

VTS_UNI_SAMP
VTS_UNI_COMMON
VTS_UNI_CLIP

void vert(inout appdata_full v, out Input o)
{
	UNITY_INITIALIZE_OUTPUT(Input, o);
	o.viewPos = UnityObjectToViewPos(v.vertex);
	VTS_VERT_UV(v, o)
	VTS_VERT_CLIP(v, o)
}

void surf(Input i, inout SurfaceOutput o)
{
	// VTS_FRAG_COMMON
	float4 color = tex2D(_MainTex, i._uvTex);
	if (_Flags.x > 0)
	{
		if (tex2D(_MaskTex, i._uvTex).r < 0.5)
			clip(-1.0);
	}
	if (_Flags.y > 0)
		color = color.rrra;
	color *= _Color;

	VTS_FRAG_CLIP(i)

	o.Albedo = color.rgb;
	o.Alpha = color.a;
}
