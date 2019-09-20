
#define VTSH_MANUAL_CLIP 1 // surface shaders do not preserve SV_ClipDistance0
#include "VtsCommon.cginc"

struct Input
{
	float4 pos : SV_POSITION;
	VTS_V2F
};

VTS_UNI

void vert(inout appdata_full v, out Input o)
{
	UNITY_INITIALIZE_OUTPUT(Input, o);
	o.pos = UnityObjectToClipPos(v.vertex);
	o.viewPos = UnityObjectToViewPos(v.vertex);
	VTS_VERT(v, o)
}

void surf(Input i, inout SurfaceOutput o)
{
	float4 color;
	VTS_FRAG(i, color);
	o.Albedo = color.rgb;
	o.Alpha = color.a;
}
