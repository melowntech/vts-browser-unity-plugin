
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"
#include "VtsCommon.cginc"
#include "VtsAtmosphere.cginc"

struct vIn
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	VTS_VIN
};

struct v2f
{
	float4 pos : SV_POSITION;
	float3 normal : NORMAL;
	VTS_V2F
	SHADOW_COORDS(3)
};

struct fOut
{
	float4 color : SV_Target;
};

VTS_UNI

v2f vert(vIn v)
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.viewPos = UnityObjectToViewPos(v.vertex);
	o.normal = UnityObjectToWorldNormal(v.normal);
	VTS_VERT(v, o)
	TRANSFER_SHADOW(o)
	return o;
}

fOut frag(v2f i)
{
	fOut o;
	VTS_FRAG(i, o.color)
	o.color.rgb *= SHADOW_ATTENUATION(i);
	float atmDensity = vtsAtmDensity(i.viewPos);
	o.color = vtsAtmColor(atmDensity, o.color);
	return o;
}
