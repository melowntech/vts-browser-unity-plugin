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
        partsGroup = new GameObject(name + " - parts").transform;
    }

    protected void OnDestroy()
    {
        Destroy(partsGroup);
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
                    Destroy(p);
            }
            opaquePartsCache.Clear();
        }

        Dictionary<VtsMesh, List<DrawSurfaceTask>> tasksByMesh = new Dictionary<VtsMesh, List<DrawSurfaceTask>>();
        foreach (DrawSurfaceTask t in draws.opaque)
        {
            VtsMesh k = t.mesh as VtsMesh;
            if (!tasksByMesh.ContainsKey(k))
                tasksByMesh.Add(k, new List<DrawSurfaceTask>());
            tasksByMesh[k].Add(t);
        }

        HashSet<VtsMesh> partsToRemove = new HashSet<VtsMesh>(opaquePartsCache.Keys);

        foreach (KeyValuePair<VtsMesh, List<DrawSurfaceTask>> tbm in tasksByMesh)
        {
            if (!opaquePartsCache.ContainsKey(tbm.Key))
                opaquePartsCache.Add(tbm.Key, new List<GameObject>(tbm.Value.Count));
            UpdateOpaqueParts(tbm.Value, opaquePartsCache[tbm.Key]);
            partsToRemove.Remove(tbm.Key);
        }

        foreach (VtsMesh m in partsToRemove)
        {
            foreach (GameObject p in opaquePartsCache[m])
                Destroy(p);
            opaquePartsCache.Remove(m);
        }

        if (shaderValueAtmEnabled)
        {
            foreach (var l in opaquePartsCache)
            {
                foreach (var p in l.Value)
                {
                    var mr = p.GetComponent<MeshRenderer>();
                    mr.GetPropertyBlock(propertyBlock);
                    UpdateAtmosphereDynamic(propertyBlock);
                    mr.SetPropertyBlock(propertyBlock);
                }
            }
        }
    }

    private void UpdateOpaqueParts(List<DrawSurfaceTask> tasks, List<GameObject> parts)
    {
        if (parts.Count == tasks.Count)
            return;
        if (parts.Count > 0)
        {
            foreach (GameObject p in parts)
                Destroy(p);
            parts.Clear();
        }
        foreach (DrawSurfaceTask t in tasks)
        {
            GameObject o = Instantiate(opaquePrefab, partsGroup);
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
            transparentPartsCache.Add(Instantiate(transparentPrefab, partsGroup));
            changeCount--;
        }
        if (changeCount < 0)
        {
            // deflate
            foreach (GameObject p in transparentPartsCache.GetRange(draws.transparent.Count, -changeCount))
                Destroy(p);
            transparentPartsCache.RemoveRange(draws.transparent.Count, -changeCount);
        }
        Debug.Assert(draws.transparent.Count == transparentPartsCache.Count);

        // update the parts
        int index = 0;
        foreach (DrawSurfaceTask t in draws.transparent)
        {
            GameObject o = transparentPartsCache[index++];
            UpdatePart(o, t);
        }
    }

    private void UpdatePart(GameObject o, DrawSurfaceTask t)
    {
        o.GetComponent<MeshFilter>().mesh = (t.mesh as VtsMesh).Get();
        if (shiftingOriginMap)
            VtsUtil.GetOrAddComponent<VtsObjectShiftingOrigin>(o).map = shiftingOriginMap;
        UpdateMaterial(propertyBlock, t);
        VtsUtil.Matrix2Transform(o.transform, VtsUtil.V2U44(Math.Mul44x44(conv, System.Array.ConvertAll(t.data.mv, System.Convert.ToDouble))));
        o.GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);
    }

    public GameObject opaquePrefab;
    public GameObject transparentPrefab;

    private readonly Dictionary<VtsMesh, List<GameObject>> opaquePartsCache = new Dictionary<VtsMesh, List<GameObject>>();
    private readonly List<GameObject> transparentPartsCache = new List<GameObject>();
    private Transform partsGroup;

    private double[] conv;
    private VtsMapShiftingOrigin shiftingOriginMap;
    private bool originHasShifted = false;
}

