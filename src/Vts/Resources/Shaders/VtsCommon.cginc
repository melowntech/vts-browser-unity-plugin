
#if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
#	define VTSH_MANUAL_CLIP 1
#endif

#define VTS_VIN_UV \
	float2 uvInternal : TEXCOORD0; \
	float2 uvExternal : TEXCOORD1;

#define VTS_V2F_COMMON \
	float3 viewPos : TEXCOORD0; \
	float2 uvTex : TEXCOORD1;

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
	o.uvTex = mul((float3x3)_UvMat, float3(_Flags.w > 0 ? i.uvExternal : i.uvInternal, 1.0)).xy;

#define VTS_VERT_CLIP(i,o) \
	o.clip[0] = (i.uvExternal[0] - _UvClip[0]) * +1.0; \
	o.clip[1] = (i.uvExternal[1] - _UvClip[1]) * +1.0; \
	o.clip[2] = (i.uvExternal[0] - _UvClip[2]) * -1.0; \
	o.clip[3] = (i.uvExternal[1] - _UvClip[3]) * -1.0;

#define VTS_FRAG_COMMON(i,o) \
	o.color = tex2D(_MainTex, i.uvTex); \
	if (_Flags.x > 0) \
	{ \
		if (tex2D(_MaskTex, i.uvTex).r < 0.5) \
			discard; \
	} \
	if (_Flags.y > 0) \
		o.color = o.color.rrra; \
	o.color *= _Color;

#ifdef VTSH_MANUAL_CLIP
#	define VTS_FRAG_CLIP(i) \
		if (any(i.clip <= 0.0)) \
			discard;
#else
#	define VTS_FRAG_CLIP(i)
#endif
