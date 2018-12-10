
#include "UnityCG.cginc"
#include "VtsCommon.cginc"
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
	VTS_VERT_UV(v, o)
	VTS_VERT_CLIP(v, o)
	return o;
}

fOut frag(v2f i)
{
	fOut o;
	VTS_FRAG_COMMON(i, o.color)

	VTS_FRAG_CLIP(i)

	// atmosphere
	float atmDensity = vtsAtmDensity(i.viewPos);
	o.color = vtsAtmColor(atmDensity, o.color);

	return o;
}
