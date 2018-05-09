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

public class VtsMap : MonoBehaviour
{
    void OnEnable()
    {
        VtsLog.Dummy();
        Debug.Assert(map == null);
        map = new Map("");
        map.DataInitialize();
        map.RenderInitialize();
        map.SetMapConfigPath("https://cdn.melown.com/mario/store/melown2015/map-config/melown/Melown-Earth-Intergeo-2017/mapConfig.json");

        map.EventLoadTexture += LoadTexture;
        map.EventLoadMesh += LoadMesh;
    }

    void Update()
    {
        Util.Log(LogLevel.info2, "Unity update frame index: " + frameIndex++);
        Debug.Assert(map != null);

        double[] pan = new double[3];
        pan[0] = 1;
        map.Pan(pan);

        map.DataTick();
        map.RenderTickPrepare(Time.deltaTime);
    }

    System.Object LoadTexture(vts.Texture t)
    {
        Debug.Assert(map != null);
        // todo
        return new System.Object();
    }

    static float ExtractFloat(vts.Mesh m, int byteOffset)
    {
        return System.BitConverter.ToSingle(m.vertices, byteOffset);
    }

    static Vector3[] ExtractBuffer3(vts.Mesh m, int attributeIndex)
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

    static Vector2[] ExtractBuffer2(vts.Mesh m, int attributeIndex)
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

    System.Object LoadMesh(vts.Mesh m)
    {
        Debug.Assert(map != null);
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

    void OnDisable()
    {
        Debug.Assert(map != null);
        map.DataDeinitialize();
        map.RenderDeinitialize();
        map = null;
    }

    private uint frameIndex;
    private Map map;

    public Map Handle { get { return map; } }
}

