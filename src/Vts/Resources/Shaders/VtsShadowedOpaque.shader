Shader "Vts/LitOpaque"
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
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma multi_compile __ VTS_ATMOSPHERE
			#include "VtsShadowed.cginc"
			ENDCG
		}

		UsePass "Vts/UnlitOpaque/SHADOWCASTER"
	}
}


