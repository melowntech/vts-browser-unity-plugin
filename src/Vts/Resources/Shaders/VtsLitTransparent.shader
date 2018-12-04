Shader "Vts/LitTransparent"
{
	SubShader
	{
		Tags
		{
			//"Queue" = "AlphaTest+50"
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase"
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
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma multi_compile __ VTS_ATMOSPHERE
			#include "VtsLit.cginc"
			ENDCG
		}

		UsePass "Vts/UnlitOpaque/SHADOWCASTER"
	}
}

