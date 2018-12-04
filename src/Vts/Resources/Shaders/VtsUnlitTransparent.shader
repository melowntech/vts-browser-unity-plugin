Shader "Vts/UnlitTransparent"
{
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			Offset 0, -10

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile __ VTS_ATMOSPHERE
			#include "VtsUnlit.cginc"
			ENDCG
		}

		UsePass "Vts/UnlitOpaque/SHADOWCASTER"
	}
}
