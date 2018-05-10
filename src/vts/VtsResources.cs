using UnityEngine;
using vts;

public static class VtsResources
{
    private static TextureFormat ExtractFormat(vts.Texture t)
    {
        switch (t.type)
        {
            case GpuType.Byte:
            case GpuType.UnsignedByte:
                switch (t.components)
                {
                    case 1: return TextureFormat.R8;
                    case 2: return TextureFormat.RG16;
                    case 3: return TextureFormat.RGB24;
                    case 4: return TextureFormat.RGBA32;
                }
                break;
            case GpuType.Short:
            case GpuType.UnsignedShort:
                switch (t.components)
                {
                    case 1: return TextureFormat.R16;
                }
                break;
            case GpuType.Float:
                switch (t.components)
                {
                    case 1: return TextureFormat.RFloat;
                    case 2: return TextureFormat.RGFloat;
                    case 4: return TextureFormat.RGBAFloat;
                }
                break;
        }
        throw new VtsException(-19, "Unsupported texture format");
    }

    public static System.Object LoadTexture(vts.Texture t)
    {
        Texture2D tex = new Texture2D((int)t.width, (int)t.height, ExtractFormat(t), false);
        tex.LoadRawTextureData(t.data);
        tex.Apply();
        VtsTexture vt = new VtsTexture();
        vt.texture = tex;
        vt.monochromatic = t.components == 1;
        return vt;
    }

    private static float ExtractFloat(vts.Mesh m, int byteOffset)
    {
        return System.BitConverter.ToSingle(m.vertices, byteOffset);
    }

    private static Vector3[] ExtractBuffer3(vts.Mesh m, int attributeIndex)
    {
        var a = m.attributes[attributeIndex];
        if (!a.enable)
            return null;
        Debug.Assert(a.components == 3);
        Debug.Assert(a.type == GpuType.Float);
        Vector3[] r = new Vector3[m.verticesCount];
        int stride = (int)(a.stride == 0 ? 12 : a.stride);
        int start = (int)a.offset;
        for (int i = 0; i < m.verticesCount; i++)
        {
            r[i] = new Vector3(
                ExtractFloat(m, start + i * stride + 0),
                ExtractFloat(m, start + i * stride + 4),
                ExtractFloat(m, start + i * stride + 8)
                );
        }
        return r;
    }

    private static Vector2[] ExtractBuffer2(vts.Mesh m, int attributeIndex)
    {
        var a = m.attributes[attributeIndex];
        if (!a.enable)
            return null;
        Debug.Assert(a.components == 2);
        Debug.Assert(a.type == GpuType.Float);
        Vector2[] r = new Vector2[m.verticesCount];
        int stride = (int)(a.stride == 0 ? 8 : a.stride);
        int start = (int)a.offset;
        for (int i = 0; i < m.verticesCount; i++)
        {
            r[i] = new Vector2(
                ExtractFloat(m, start + i * stride + 0),
                ExtractFloat(m, start + i * stride + 4)
                );
        }
        return r;
    }

    public static System.Object LoadMesh(vts.Mesh m)
    {
        UnityEngine.Mesh u = new UnityEngine.Mesh();
        // assume that attribute 0 is vertex positions
        u.vertices = ExtractBuffer3(m, 0);
        // assume that attribute 1 is internal texture coordinates (used with textures that are packed with the mesh)
        u.uv = ExtractBuffer2(m, 1);
        // assume that attribute 2 is external texture coordinates (used with textures that come from bound layers)
        u.uv2 = ExtractBuffer2(m, 2);
        // indices
        // I do NOT know why flipping the winding order is required. There may be some mistake in projection matrices.
        if (m.indices != null)
        {
            for (int i = 0; i < m.indicesCount; i += 3)
            {
                ushort tmp = m.indices[i + 1];
                m.indices[i + 2] = m.indices[i + 1];
                m.indices[i + 1] = tmp;
            }
            u.triangles = System.Array.ConvertAll(m.indices, System.Convert.ToInt32);
        }
        else
        {
            var t = new int[m.verticesCount];
            for (int i = 0; i < m.verticesCount; i += 3)
            {
                t[i + 0] = i + 0;
                t[i + 1] = i + 2;
                t[i + 2] = i + 1;
            }
            u.triangles = t;
        }
        // finalize
        u.RecalculateBounds();
        u.RecalculateNormals();
        return u;
    }
}

public class VtsTexture
{
    public Texture2D texture;
    public bool monochromatic;
}
