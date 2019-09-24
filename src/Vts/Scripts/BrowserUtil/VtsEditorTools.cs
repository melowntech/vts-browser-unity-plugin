using UnityEngine;
using UnityEditor;

#if (UNITY_EDITOR)
public static class VtsBlueNoise
{
    [MenuItem("Vts/Convert Blue Noise Texture")]
    public static void ConvertBlueNoiseTexturesToArray()
    {
        Texture2DArray bt = new Texture2DArray(64, 64, 16, TextureFormat.R8, false);
        for (int i = 0; i < 16; i++)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Vts/Resources/Textures/BlueNoise/" + i + ".png");
            bt.SetPixels32(t.GetPixels32(), i);
        }
        bt.wrapMode = TextureWrapMode.Repeat;
        AssetDatabase.CreateAsset(bt, "Assets/Vts/Resources/Textures/BlueNoise.asset");
    }

    // validation
    [MenuItem("Vts/Convert Blue Noise Texture", true)]
    public static bool ValidateBlueNoiseTexturesAvailable()
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Vts/Resources/Textures/BlueNoise/0.png") != null;
    }
}
#endif
