Shader "Vts/UnlitOpaque"
{
	SubShader
	{
		Tags
		{
			"Queue" = "Geometry"
			"RenderType" = "Opaque"
		}

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile __ VTS_ATMOSPHERE
			#include "VtsUnlit.cginc"
			ENDCG
		}

		Pass
		{
			Name "ShadowCaster"

			Tags { "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#include "VtsShadowCaster.cginc"
			ENDCG
		}
	}
}
