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

using System.Collections.Generic;
using UnityEngine;
using vts;

public class VtsCameraObjects : VtsCameraBase
{
    protected override void Start()
    {
        base.Start();
        var cam = GetComponent<Camera>();
        cam.cullingMask |= 1 << renderLayer;
    }

    protected override void CameraUpdate()
    {
        double[] conv = Math.Mul44x44(Math.Mul44x44(VtsUtil.U2V44(mapTrans.localToWorldMatrix), VtsUtil.U2V44(VtsUtil.SwapYZ)), Math.Inverse44(draws.camera.view));

        Dictionary<VtsMesh, List<DrawTask>> tasksByMesh = new Dictionary<VtsMesh, List<DrawTask>>();
        foreach (DrawTask t in draws.opaque)
        {
            VtsMesh k = t.mesh as VtsMesh;
            if (!tasksByMesh.ContainsKey(k))
                tasksByMesh.Add(k, new List<DrawTask>());
            tasksByMesh[k].Add(t);
        }

        HashSet<VtsMesh> partsToRemove = new HashSet<VtsMesh>(partsCache.Keys);

        foreach (KeyValuePair<VtsMesh, List<DrawTask>> tbm in tasksByMesh)
        {
            if (!partsCache.ContainsKey(tbm.Key))
                partsCache.Add(tbm.Key, new List<GameObject>(tbm.Value.Count));
            UpdateParts(tbm.Value, partsCache[tbm.Key], conv);
            partsToRemove.Remove(tbm.Key);
        }

        foreach (VtsMesh m in partsToRemove)
        {
            foreach (GameObject o in partsCache[m])
                Destroy(o);
            partsCache.Remove(m);
        }
    }

    private void UpdateParts(List<DrawTask> tasks, List<GameObject> parts, double[] conv)
    {
        if (parts.Count == tasks.Count)
            return;
        if (parts.Count > 0)
        {
            foreach (GameObject p in parts)
                Destroy(p);
            parts.Clear();
        }
        foreach (DrawTask t in tasks)
        {
            GameObject o = Instantiate(renderPrefab);
            parts.Add(o);
            o.layer = renderLayer;
            UnityEngine.Mesh msh = (tasks[0].mesh as VtsMesh).Get();
            o.GetComponent<MeshFilter>().mesh = msh;
            Material mat = o.GetComponent<MeshRenderer>().material;
            UpdateMaterial(mat);
            bool monochromatic = false;
            if (t.texColor != null)
            {
                var tt = t.texColor as VtsTexture;
                mat.SetTexture(shaderPropertyMainTex, tt.Get());
                monochromatic = tt.monochromatic;
            }
            if (t.texMask != null)
            {
                var tt = t.texMask as VtsTexture;
                mat.SetTexture(shaderPropertyMaskTex, tt.Get());
            }
            mat.SetMatrix(shaderPropertyUvMat, VtsUtil.V2U33(t.data.uvm));
            mat.SetVector(shaderPropertyUvClip, VtsUtil.V2U4(t.data.uvClip));
            mat.SetVector(shaderPropertyColor, VtsUtil.V2U4(t.data.color));
            // flags: mask, monochromatic, flat shading, uv source
            mat.SetVector(shaderPropertyFlags, new Vector4(t.texMask == null ? 0 : 1, monochromatic ? 1 : 0, 0, t.data.externalUv ? 1 : 0));
            VtsUtil.Matrix2Transform(o.transform, VtsUtil.V2U44(Math.Mul44x44(conv, System.Array.ConvertAll(t.data.mv, System.Convert.ToDouble))));
        }
    }

    public GameObject renderPrefab;
    public int renderLayer = 31;

    private readonly Dictionary<VtsMesh, List<GameObject>> partsCache = new Dictionary<VtsMesh, List<GameObject>>();
}

