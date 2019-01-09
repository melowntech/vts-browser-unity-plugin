Shader "Vts/SurfaceTransparent"
{
	Properties
	{
		_Cutoff("Alpha Cut-Off Threshold", Range(0,1)) = 0.5
	}

	SubShader
	{
		Tags
		{
			"Queue" = "AlphaTest"
			"RenderType" = "Transparent"
			"ForceNoShadowCasting" = "True"
			"IgnoreProjector" = "True"
		}

		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off
		Offset 0, -1000

		CGPROGRAM
		#pragma surface surf Lambert fullforwardshadows vertex:vert alphatest:_Cutout
		#pragma target 4.0
		#include "VtsSurface.cginc"
		ENDCG
	}
}
