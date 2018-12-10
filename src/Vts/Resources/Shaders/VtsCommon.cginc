
#if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
#	define VTSH_MANUAL_CLIP 1
#endif

// texcoord = internal uv
// texcoord1 = external uv
#define VTS_VIN_UV \
	float2 texcoord  : TEXCOORD0; \
	float2 texcoord1 : TEXCOORD1;

#define VTS_V2F_COMMON \
	float3 viewPos : TEXCOORD0; \
	float2 _uvTex : TEXCOORD1;

#ifdef VTSH_MANUAL_CLIP
#	define VTS_V2F_CLIP \
		float4 clip : TEXCOORD2;
#else
#	define VTS_V2F_CLIP \
		float4 clip : SV_ClipDistance0;
#endif

#define VTS_UNI_SAMP \
	sampler2D _MainTex; \
	sampler2D _MaskTex;

// flags: mask, monochromatic, flat shading, uv source
#define VTS_UNI_COMMON \
	float4x4 _UvMat; \
	float4 _Color; \
	float4 _Flags;

#define VTS_UNI_CLIP \
	float4 _UvClip;

#define VTS_VERT_UV(i,o) \
	o._uvTex = mul((float3x3)_UvMat, float3(_Flags.w > 0 ? i.texcoord1.xy : i.texcoord.xy, 1.0)).xy;

#define VTS_VERT_CLIP(i,o) \
	o.clip[0] = (i.texcoord1[0] - _UvClip[0]) * +1.0; \
	o.clip[1] = (i.texcoord1[1] - _UvClip[1]) * +1.0; \
	o.clip[2] = (i.texcoord1[0] - _UvClip[2]) * -1.0; \
	o.clip[3] = (i.texcoord1[1] - _UvClip[3]) * -1.0;

#define VTS_FRAG_COMMON(i,color) \
	color = tex2D(_MainTex, i._uvTex); \
	if (_Flags.x > 0) \
	{ \
		if (tex2D(_MaskTex, i._uvTex).r < 0.5) \
			clip(-1.0); \
	} \
	if (_Flags.y > 0) \
		color = color.rrra; \
	color *= _Color;

#ifdef VTSH_MANUAL_CLIP
#	define VTS_FRAG_CLIP(i) \
		if (any(i.clip <= 0.0)) \
			clip(-1.0);
#else
#	define VTS_FRAG_CLIP(i)
#endif
