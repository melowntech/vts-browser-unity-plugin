Shader "Vts/SurfaceOpaque"
{
	SubShader
	{
		Tags
		{
			"Queue" = "Geometry"
			"RenderType" = "Opaque"
		}

		UsePass "Vts/UnlitOpaque/SHADOWCASTER"

		Cull Off

		CGPROGRAM
		#pragma surface surf Lambert fullforwardshadows vertex:vert
		#pragma target 4.0
		#include "VtsSurface.cginc"
		ENDCG
	}
}
