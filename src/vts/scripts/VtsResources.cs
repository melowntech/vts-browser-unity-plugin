/**
 * Copyright (c) 2017 Melown Technologies SE
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * *  Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 *
 * *  Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using UnityEngine;
using vts;

public static class VtsResources
{
    public static System.Object LoadTexture(vts.Texture t)
    {
        return new VtsTexture(t);
    }

    public static System.Object LoadMesh(vts.Mesh m)
    {
        return new VtsMesh(m);
    }
}

public class VtsTexture
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

    public VtsTexture(vts.Texture t)
    {
        vt = t;
        monochromatic = t.components == 1;
    }

    public Texture2D Get()
    {
        if (ut == null)
        {
            ut = new Texture2D((int)vt.width, (int)vt.height, ExtractFormat(vt), false);
            ut.LoadRawTextureData(vt.data);
            ut.filterMode = FilterMode.Bilinear;
            ut.anisoLevel = 100; // just do it!
            ut.Apply(false, true);
            vt = null;
        }
        return ut;
    }

    private vts.Texture vt;
    private Texture2D ut;
    public readonly bool monochromatic;
}

public class VtsMesh
{
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

    private void LoadTrianglesIndices(vts.Mesh m)
    {
        topology = MeshTopology.Triangles;
        // triangle winding is reversed due to different handedness of unity coordinate system
        if (m.indices != null)
        {
            for (int i = 0; i < m.indicesCount; i += 3)
            {
                ushort tmp = m.indices[i + 1];
                m.indices[i + 2] = m.indices[i + 1];
                m.indices[i + 1] = tmp;
            }
            indices = System.Array.ConvertAll(m.indices, System.Convert.ToInt32);
        }
        else
        {
            indices = new int[m.verticesCount];
            for (int i = 0; i < m.verticesCount; i += 3)
            {
                indices[i + 0] = i + 0;
                indices[i + 1] = i + 2;
                indices[i + 2] = i + 1;
            }
        }
    }

    private void LoadLinesIndices(vts.Mesh m)
    {
        topology = MeshTopology.Lines;
        if (m.indices != null)
        {
            indices = System.Array.ConvertAll(m.indices, System.Convert.ToInt32);
        }
        else
        {
            indices = new int[m.verticesCount];
            for (int i = 0; i < m.verticesCount; i++)
                indices[i] = i;
        }
    }

    public VtsMesh(vts.Mesh m)
    {
        // assume that attribute 0 is vertex positions
        vertices = ExtractBuffer3(m, 0);
        // assume that attribute 1 is internal texture coordinates (used with textures that are packed with the mesh)
        uv0 = ExtractBuffer2(m, 1);
        // assume that attribute 2 is external texture coordinates (used with textures that come from bound layers)
        uv1 = ExtractBuffer2(m, 2);
        // indices
        switch (m.faceMode)
        {
            case FaceMode.Triangles:
                LoadTrianglesIndices(m);
                break;
            case FaceMode.Lines:
                LoadLinesIndices(m);
                break;
            default:
                throw new VtsException(-19, "Unsupported mesh face mode");
        }
    }

    public UnityEngine.Mesh Get()
    {
        if (um == null)
        {
            um = new UnityEngine.Mesh();
            um.vertices = vertices;
            um.uv = uv0;
            um.uv2 = uv1;
            um.SetIndices(indices, topology, 0);
            um.RecalculateBounds();
            //um.RecalculateNormals();
            um.UploadMeshData(true);
            vertices = null;
            uv0 = uv1 = null;
            indices = null;
        }
        return um;
    }

    private Vector3[] vertices;
    private Vector2[] uv0;
    private Vector2[] uv1;
    private int[] indices;
    private MeshTopology topology;
    private UnityEngine.Mesh um;
}
