
#if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
#	define VTSH_MANUAL_CLIP 1
#endif

#ifdef VTSH_MANUAL_CLIP
#	define VTSH_V2F_CLIPPING \
		float4 clip : TEXCOORD2;
#	define VTS_FRAG_CLIPPING(i) \
		if (any(i.clip <= 0.0)) \
			clip(-1.0);
#else
#	define VTSH_V2F_CLIPPING \
		float4 clip : SV_ClipDistance0;
#	define VTS_FRAG_CLIPPING(i)
#endif

// texcoord = internal uv
// texcoord1 = external uv
#define VTS_VIN \
	float2 texcoord  : TEXCOORD0; \
	float2 texcoord1 : TEXCOORD1;

#define VTS_V2F \
	float3 viewPos : TEXCOORD0; \
	float2 _uvTex : TEXCOORD1; \
	VTSH_V2F_CLIPPING

// _Flags: mask, monochromatic, flat shading, uv source

#define VTS_UNI \
	sampler2D _MainTex; \
	sampler2D _MaskTex; \
	UNITY_DECLARE_TEX2DARRAY(_BlueNoiseTex); \
	float4x4 _UvMat; \
	float4 _Color; \
	float4 _UvClip; \
	float _BlendingCoverage; \
	int _Flags; \
	int _FrameIndex; \
	bool getFlag(int i) { return (_Flags & (1 << i)) != 0; }

#define VTS_VERT(i,o) \
	o._uvTex = mul((float3x3)_UvMat, float3(getFlag(3) ? i.texcoord1.xy : i.texcoord.xy, 1.0)).xy; \
	o.clip[0] = (i.texcoord1[0] - _UvClip[0]) * +1.0; \
	o.clip[1] = (i.texcoord1[1] - _UvClip[1]) * +1.0; \
	o.clip[2] = (i.texcoord1[0] - _UvClip[2]) * -1.0; \
	o.clip[3] = (i.texcoord1[1] - _UvClip[3]) * -1.0;

#define VTS_FRAG(i,color) \
	VTS_FRAG_CLIPPING(i) \
	color = tex2D(_MainTex, i._uvTex); \
	if (getFlag(0)) \
	{ \
		if (tex2D(_MaskTex, i._uvTex).r < 0.5) \
			clip(-1.0); \
	} \
	if (_BlendingCoverage > -0.5) \
	{ \
		float4 cp = ComputeScreenPos(i.pos); \
		float3 uv = float3(cp.xy, _FrameIndex % 16); \
		float smpl = UNITY_SAMPLE_TEX2DARRAY_LOD(_BlueNoiseTex, uv, 0); \
		if (_BlendingCoverage < smpl) \
			clip(-1.0); \
	} \
	if (getFlag(1)) \
		color = color.rrra; \
	color *= _Color;

