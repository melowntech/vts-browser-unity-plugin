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
        var cam = GetComponent<UnityEngine.Camera>();
        cam.cullingMask |= 1 << renderLayer;
    }

    protected override void CameraDraw()
    {
        shiftingOriginMap = mapObject.GetComponent<VtsMapShiftingOrigin>();
        conv = Math.Mul44x44(Math.Mul44x44(VtsUtil.U2V44(mapTrans.localToWorldMatrix), VtsUtil.U2V44(VtsUtil.SwapYZ)), Math.Inverse44(draws.camera.view));
        UpdateOpaqueDraws();
        UpdateTransparentDraws();
    }

    public override void OriginShifted()
    {
        originHasShifted = true;
    }

    private void UpdateOpaqueDraws()
    {
        if (originHasShifted)
        {
            originHasShifted = false;
            foreach (var l in opaquePartsCache)
            {
                foreach (var p in l.Value)
                    DestroyWithMaterial(p);
            }
            opaquePartsCache.Clear();
        }

        Dictionary<VtsMesh, List<DrawTask>> tasksByMesh = new Dictionary<VtsMesh, List<DrawTask>>();
        foreach (DrawTask t in draws.opaque)
        {
            VtsMesh k = t.mesh as VtsMesh;
            if (!tasksByMesh.ContainsKey(k))
                tasksByMesh.Add(k, new List<DrawTask>());
            tasksByMesh[k].Add(t);
        }

        HashSet<VtsMesh> partsToRemove = new HashSet<VtsMesh>(opaquePartsCache.Keys);

        foreach (KeyValuePair<VtsMesh, List<DrawTask>> tbm in tasksByMesh)
        {
            if (!opaquePartsCache.ContainsKey(tbm.Key))
                opaquePartsCache.Add(tbm.Key, new List<GameObject>(tbm.Value.Count));
            UpdateOpaqueParts(tbm.Value, opaquePartsCache[tbm.Key]);
            partsToRemove.Remove(tbm.Key);
        }

        foreach (VtsMesh m in partsToRemove)
        {
            foreach (GameObject p in opaquePartsCache[m])
                DestroyWithMaterial(p);
            opaquePartsCache.Remove(m);
        }

        foreach (var l in opaquePartsCache)
        {
            foreach (var p in l.Value)
                UpdateMaterialDynamic(p.GetComponent<MeshRenderer>().material);
        }
    }

    private void UpdateOpaqueParts(List<DrawTask> tasks, List<GameObject> parts)
    {
        if (parts.Count == tasks.Count)
            return;
        if (parts.Count > 0)
        {
            foreach (GameObject p in parts)
                DestroyWithMaterial(p);
            parts.Clear();
        }
        foreach (DrawTask t in tasks)
        {
            GameObject o = Instantiate(opaquePrefab);
            parts.Add(o);
            UpdatePart(o, t);
        }
    }

    private void UpdateTransparentDraws()
    {
        // resize the transparentPartsCache
        int changeCount = draws.transparent.Count - transparentPartsCache.Count;
        while (changeCount > 0)
        {
            // inflate
            transparentPartsCache.Add(Instantiate(transparentPrefab));
            changeCount--;
        }
        if (changeCount < 0)
        {
            // deflate
            foreach (GameObject p in transparentPartsCache.GetRange(draws.transparent.Count, -changeCount))
                DestroyWithMaterial(p);
            transparentPartsCache.RemoveRange(draws.transparent.Count, -changeCount);
        }
        Debug.Assert(draws.transparent.Count == transparentPartsCache.Count);

        // update the parts
        int index = 0;
        foreach (DrawTask t in draws.transparent)
        {
            GameObject o = transparentPartsCache[index++];
            UpdatePart(o, t);
        }
    }

    private void UpdatePart(GameObject o, DrawTask t)
    {
        o.layer = renderLayer;
        o.GetComponent<MeshFilter>().mesh = (t.mesh as VtsMesh).Get();
        o.GetComponent<VtsObjectShiftingOrigin>().map = shiftingOriginMap;
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

    private void DestroyWithMaterial(GameObject p)
    {
        Destroy(p.GetComponent<MeshRenderer>().material);
        Destroy(p);
    }

    public GameObject opaquePrefab;
    public GameObject transparentPrefab;
    public int renderLayer = 31;

    private readonly Dictionary<VtsMesh, List<GameObject>> opaquePartsCache = new Dictionary<VtsMesh, List<GameObject>>();
    private readonly List<GameObject> transparentPartsCache = new List<GameObject>();

    private double[] conv;
    private VtsMapShiftingOrigin shiftingOriginMap;
    private bool originHasShifted = false;
}

