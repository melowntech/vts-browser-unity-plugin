Shader "Vts/LitOpaque"
{
	SubShader
	{
		Tags
		{
			"Queue" = "Geometry"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
		}

		Pass
		{
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma multi_compile __ VTS_ATMOSPHERE
			#include "VtsLit.cginc"
			ENDCG
		}

		UsePass "Vts/UnlitOpaque/SHADOWCASTER"
	}
}


