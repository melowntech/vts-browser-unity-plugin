Shader "Vts/LitTransparent"
{
	SubShader
	{
		Tags
		{
			"Queue" = "AlphaTest"
			"RenderType" = "Transparent"
			"ForceNoShadowCasting" = "True"
			"IgnoreProjector" = "True"
		}

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }

			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			Offset 0, -1000

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma multi_compile __ VTS_ATMOSPHERE
			#include "VtsShadowed.cginc"
			ENDCG
		}
	}
}


