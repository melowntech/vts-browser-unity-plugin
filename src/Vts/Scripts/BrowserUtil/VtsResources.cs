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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using vts;

// callbacks called by the vts library to upload assets to the application
// these callbacks are called from non-main thread
// therefore, to actually upload to gpu one must wait till used in the main thread
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

    public static void Init()
    {
        lock (resourcesToDestroy)
        {
            if (resourcesUnloader == null)
            {
                resourcesUnloader = new GameObject();
                resourcesUnloader.AddComponent<VtsResourcesUnloader>();
                resourcesUnloader.name = "Vts Resources Unloader";
                resourcesUnloader.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(resourcesUnloader);
            }
        }
    }

    internal static GameObject resourcesUnloader;
    internal static List<UnityEngine.Object> resourcesToDestroy = new List<UnityEngine.Object>();
}

// it is forbidden to call unity's Destroy on non-main threads
// therefore, in resource's dispose method, we enqueue the resource to be destroyed on main thread
// this class declares a component for dequeuing and destroying the resources on the main thread
// the gameobject with this component is created automatically and hidden, indestructible
internal class VtsResourcesUnloader : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine("UnloadResources");
    }

    private IEnumerator UnloadResources()
    {
        while (true)
        {
            lock (VtsResources.resourcesToDestroy)
            {
                foreach (var o in VtsResources.resourcesToDestroy)
                {
                    UnityEngine.Object.Destroy(o);
                }
                VtsResources.resourcesToDestroy.Clear();
            }
            yield return new WaitForSeconds(.5f);
        }
    }
}

// class that represents single texture provided by the vts
public class VtsTexture : IDisposable
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

    private static UnityEngine.FilterMode ExtractFilterMode(vts.FilterMode mode)
    {
        switch (mode)
        {
            case vts.FilterMode.Nearest:
                return UnityEngine.FilterMode.Point;
            case vts.FilterMode.Linear:
                return UnityEngine.FilterMode.Bilinear;
            default:
                return UnityEngine.FilterMode.Trilinear;
        }
    }

    private static TextureWrapMode ExtractWrapMode(vts.WrapMode mode)
    {
        switch (mode)
        {
            case vts.WrapMode.Repeat:
                return TextureWrapMode.Repeat;
            case vts.WrapMode.MirroredRepeat:
                return TextureWrapMode.Mirror;
            case vts.WrapMode.MirrorClampToEdge:
                return TextureWrapMode.MirrorOnce;
            case vts.WrapMode.ClampToEdge:
                return TextureWrapMode.Clamp;
        }
        throw new VtsException(-19, "Unsupported texture wrap mode");
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
            Debug.Assert(vt != null);
            ut = new Texture2D((int)vt.width, (int)vt.height, ExtractFormat(vt), false);
            ut.LoadRawTextureData(vt.data);
            ut.filterMode = ExtractFilterMode(vt.filterMode);
            ut.wrapMode = ExtractWrapMode(vt.wrapMode);
            ut.anisoLevel = 100; // just do it!
            ut.Apply(false, true); // actually upload the texture to gpu
            vt = null;
        }
        return ut;
    }

    public void Dispose()
    {
        if (ut)
        {
            lock (VtsResources.resourcesToDestroy)
            {
                VtsResources.resourcesToDestroy.Add(ut);
            }
            ut = null;
        }
    }

    private vts.Texture vt;
    private Texture2D ut;
    public readonly bool monochromatic;
}

// class that represents single mesh provided by the vts
public class VtsMesh : IDisposable
{
    private static float ExtractFloat(vts.Mesh m, int byteOffset, GpuType type, bool normalized)
    {
        switch (type)
        {
            case GpuType.Float:
                Debug.Assert(!normalized);
                return BitConverter.ToSingle(m.vertices, byteOffset);
            case GpuType.UnsignedShort:
                Debug.Assert(normalized);
                return BitConverter.ToUInt16(m.vertices, byteOffset) / 65535.0f;
            default:
                throw new VtsException(-17, "Unsupported gpu type");
        }
    }

    private static Vector3[] ExtractBuffer3(vts.Mesh m, int attributeIndex)
    {
        var a = m.attributes[attributeIndex];
        if (!a.enable)
            return null;
        Debug.Assert(a.components == 3);
        uint typeSize = Util.GpuTypeSize(a.type);
        Vector3[] r = new Vector3[m.verticesCount];
        int stride = (int)(a.stride == 0 ? typeSize * a.components : a.stride);
        int start = (int)a.offset;
        for (int i = 0; i < m.verticesCount; i++)
        {
            r[i] = new Vector3(
                ExtractFloat(m, start + i * stride + 0 * (int)typeSize, a.type, a.normalized),
                ExtractFloat(m, start + i * stride + 1 * (int)typeSize, a.type, a.normalized),
                ExtractFloat(m, start + i * stride + 2 * (int)typeSize, a.type, a.normalized)
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
        uint typeSize = Util.GpuTypeSize(a.type);
        Vector2[] r = new Vector2[m.verticesCount];
        int stride = (int)(a.stride == 0 ? typeSize * a.components : a.stride);
        int start = (int)a.offset;
        for (int i = 0; i < m.verticesCount; i++)
        {
            r[i] = new Vector2(
                ExtractFloat(m, start + i * stride + 0 * (int)typeSize, a.type, a.normalized),
                ExtractFloat(m, start + i * stride + 1 * (int)typeSize, a.type, a.normalized)
                );
        }
        return r;
    }

    private void LoadTrianglesIndices(vts.Mesh m)
    {
        topology = MeshTopology.Triangles;
        if (m.indices != null)
        {
            indices = Array.ConvertAll(m.indices, Convert.ToInt32);
        }
        else
        {
            indices = new int[m.verticesCount];
            for (int i = 0; i < m.verticesCount; i += 3)
            {
                indices[i + 0] = i + 0;
                indices[i + 1] = i + 1;
                indices[i + 2] = i + 2;
            }
        }
    }

    private void LoadLinesIndices(vts.Mesh m)
    {
        topology = MeshTopology.Lines;
        if (m.indices != null)
        {
            indices = Array.ConvertAll(m.indices, Convert.ToInt32);
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
            Debug.Assert(vertices != null);
            um = new UnityEngine.Mesh();
            um.vertices = vertices;
            um.uv = uv0;
            um.uv2 = uv1;
            um.SetIndices(indices, topology, 0);
            um.RecalculateBounds();
            um.RecalculateNormals();
            um.UploadMeshData(false); // upload to gpu
            vertices = null;
            uv0 = uv1 = null;
            indices = null;
        }
        return um;
    }

    public void Dispose()
    {
        if (um)
        {
            lock (VtsResources.resourcesToDestroy)
            {
                VtsResources.resourcesToDestroy.Add(um);
            }
            um = null;
        }
    }

    private Vector3[] vertices;
    private Vector2[] uv0;
    private Vector2[] uv1;
    private int[] indices;
    private MeshTopology topology;
    private UnityEngine.Mesh um;
}
