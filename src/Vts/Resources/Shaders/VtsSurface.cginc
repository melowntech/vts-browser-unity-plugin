
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
	float4 color;
	VTS_FRAG_COMMON(i, color);

	VTS_FRAG_CLIP(i)

	o.Albedo = color.rgb;
	o.Alpha = color.a;
}
