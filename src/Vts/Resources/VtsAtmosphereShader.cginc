#if VTS_ATMOSPHERE

sampler2D vtsTexAtmDensity;
float4 vtsTexAtmDensity_TexelSize;

float4x4 vtsUniAtmViewInv;
float4 vtsUniAtmColorHorizon;
float4 vtsUniAtmColorZenith;
float4 vtsUniAtmSizes; // atmosphere thickness (divided by major axis), major / minor axes ratio, inverze major axis
float4 vtsUniAtmCoefs; // horizontal exponent, colorGradientExponent
float3 vtsUniAtmCameraPosition; // world position of camera (divided by major axis)

float vtsAtmDecodeFloat(float4 rgba)
{
	return dot(rgba, float4(1.0, 1.0 / 256.0, 1.0 / (256.0*256.0), 0.0));
}

float4 vtsAtmTexelFetch(int2 iuv, int2 off)
{
	return tex2Dlod(vtsTexAtmDensity, float4((iuv + off) * vtsTexAtmDensity_TexelSize.xy, 0, 0));
}

float vtsAtmSampleDensity(float2 uv)
{
	// since some color channels of the density texture are not continuous
	//   it is important to first decode the float from rgba and only after
	//   that to filter the values
	int2 res = vtsTexAtmDensity_TexelSize.zw;
	float2 uvp = uv * float2(res - 1);
	int2 iuv = int2(uvp); // upper-left texel fetch coordinates
	float4 s;
	s.x = vtsAtmDecodeFloat(vtsAtmTexelFetch(iuv, int2(0, 0)));
	s.y = vtsAtmDecodeFloat(vtsAtmTexelFetch(iuv, int2(1, 0)));
	s.z = vtsAtmDecodeFloat(vtsAtmTexelFetch(iuv, int2(0, 1)));
	s.w = vtsAtmDecodeFloat(vtsAtmTexelFetch(iuv, int2(1, 1)));
	float2 f = frac(uvp); // interpolation factors
	float2 a = lerp(s.xz, s.yw, f.x);
	return lerp(a.x, a.y, f.y);
}

// fragDir is in model space
float vtsAtmDensityDir(float3 fragDir, float fragDist)
{
	if (vtsUniAtmSizes[0] == 0.0) // no atmosphere
		return 0.0;

	// convert from ellipsoidal into spherical space
	float3 camPos = vtsUniAtmCameraPosition;
	camPos.z *= vtsUniAtmSizes[1];
	float3 camNormal = normalize(camPos);
	fragDir.z *= vtsUniAtmSizes[1];
	fragDir = normalize(fragDir);
	if (fragDist < 1000.0)
	{
		float3 T = vtsUniAtmCameraPosition + fragDist * fragDir;
		T.z *= vtsUniAtmSizes[1];
		fragDist = length(T - camPos);
	}

	// ray parameters
	float ts[2];
	ts[1] = fragDist; // max ray length
	float l = length(camPos); // distance of camera center from world origin
	float x = dot(fragDir, -camNormal) * l; // distance from camera to a point called "x", which is on the ray and closest to world origin
	float y2 = l * l - x * x;
	float y = sqrt(y2); // distance of the ray from world origin

	float atmThickness = vtsUniAtmSizes[0]; // atmosphere height (excluding planet radius)
	float invAtmThickness = 1.0 / atmThickness;
	float atmRad = 1.0 + atmThickness; // atmosphere height including planet radius
	float atmRad2 = atmRad * atmRad;

	if (y > atmRad)
		return 0.0; // the ray does not cross the atmosphere

	float t1e = x - sqrt(1.0 - y2); // t1 at ellipse

	// fill holes in terrain if the ray passes through the planet
	if (y < 0.998 && x >= 0.0 && ts[1] > 1000.0)
		ts[1] = t1e;

	// approximate the planet by the ellipsoid if the mesh is too rough
	if (y <= 1.0)
		ts[1] = lerp(ts[1], t1e, clamp((l - 1.4) / 0.1, 0.0, 1.0));

	// to improve accuracy, swap direction of the ray to point out of the terrain
	bool swapDirection = ts[1] < 1000.0 && x >= 0.0;

	// distance of atmosphere boundary from "x"
	float a = sqrt(atmRad2 - y2);

	// clamp t0 and t1 to atmosphere boundaries
	// ts is line segment that encloses the unobstructed portion of the ray and is inside atmosphere
	ts[0] = max(0.0, x - a);
	ts[1] = min(ts[1], x + a);

	// sample the density texture
	float ds[2];
	for (int i = 0; i < 2; i++)
	{
		float t = x - ts[i];
		float r = sqrt(t * t + y2);
		float2 uv = float2(0.5 - 0.5 * t / r, 0.5 + 0.5 * (r - 1.0) * invAtmThickness);
		if (swapDirection)
			uv.x = 1.0 - uv.x;
		ds[i] = vtsAtmSampleDensity(uv);
	}

	// final optical transmittance
	float density = ds[0] - ds[1];
	if (swapDirection)
		density *= -1.0;
	float transmittance = exp(-vtsUniAtmCoefs[0] * density);
	return 1.0 - transmittance;
}

// fragVect is view-space fragment position
float vtsAtmDensity(float3 fragVect)
{
	// convert fragVect to world-space and divide by major radius
	fragVect = mul(vtsUniAtmViewInv, float4(fragVect, 1.0)).xyz;
	fragVect = fragVect * vtsUniAtmSizes[2];
	float3 f = fragVect - vtsUniAtmCameraPosition;
	return vtsAtmDensityDir(f, length(f));
}

float4 vtsAtmColor(float density, float4 color)
{
	density = clamp(density, 0.0, 1.0);
	float3 a = lerp(vtsUniAtmColorHorizon.rgb, vtsUniAtmColorZenith.rgb, pow(1.0 - density, vtsUniAtmCoefs[1]));
	return float4(lerp(color.rgb, a, density), color.a);
}

#else

float vtsAtmDensity(float3 fragVect)
{
	return 0;
}

float4 vtsAtmColor(float density, float4 color)
{
	return color;
}

#endif

