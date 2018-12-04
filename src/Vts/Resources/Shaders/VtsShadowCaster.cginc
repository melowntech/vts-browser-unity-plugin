
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
