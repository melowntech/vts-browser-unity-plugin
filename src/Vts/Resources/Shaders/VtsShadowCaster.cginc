
#include "UnityCG.cginc"
#include "VtsCommon.cginc"

struct vIn
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	VTS_VIN
};

struct v2f
{
	VTS_V2F
	V2F_SHADOW_CASTER;
};

VTS_UNI

v2f vert(vIn v)
{
	v2f o;
	VTS_VERT(v, o)
	TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
	return o;
}

float4 frag(v2f i) : SV_Target
{
	float4 color;
	VTS_FRAG(i, color)
	SHADOW_CASTER_FRAGMENT(i)
}
