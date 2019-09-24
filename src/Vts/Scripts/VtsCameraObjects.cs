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
        UpdateDrawsSimple(opaquePrefab, draws.opaque, opaquePartsCache);
        UpdateDrawsSimple(transparentPrefab, draws.transparent, transparentPartsCache);
    }

    public override void OriginShifted()
    {
        //originHasShifted = true;
    }

    private void UpdateDrawsSimple(GameObject prefab, List<DrawSurfaceTask> tasks, List<GameObject> partsCache)
    {
        // resize the partsCache
        int changeCount = tasks.Count - partsCache.Count;
        while (changeCount > 0)
        {
            // inflate
            partsCache.Add(Instantiate(prefab, partsGroup));
            changeCount--;
        }
        if (changeCount < 0)
        {
            // deflate
            foreach (GameObject p in partsCache.GetRange(tasks.Count, -changeCount))
                Destroy(p);
            partsCache.RemoveRange(tasks.Count, -changeCount);
        }
        Debug.Assert(tasks.Count == partsCache.Count);

        // update the parts
        int index = 0;
        foreach (DrawSurfaceTask t in tasks)
        {
            GameObject o = partsCache[index++];
            UpdatePart(o, t);
        }
    }

    private void UpdatePart(GameObject o, DrawSurfaceTask t)
    {
        o.GetComponent<MeshFilter>().mesh = (t.mesh as VtsMesh).Get();
        if (shiftingOriginMap)
            VtsUtil.GetOrAddComponent<VtsObjectShiftingOrigin>(o).map = shiftingOriginMap;
        VtsUtil.Matrix2Transform(o.transform, VtsUtil.V2U44(Math.Mul44x44(conv, System.Array.ConvertAll(t.data.mv, System.Convert.ToDouble))));
        InitMaterial(propertyBlock, t);
        o.GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);
    }

    public GameObject opaquePrefab;
    public GameObject transparentPrefab;

    //private readonly Dictionary<VtsMesh, List<GameObject>> opaquePartsCache = new Dictionary<VtsMesh, List<GameObject>>();
    private readonly List<GameObject> opaquePartsCache = new List<GameObject>();
    private readonly List<GameObject> transparentPartsCache = new List<GameObject>();
    private Transform partsGroup;

    private double[] conv;
    private VtsMapShiftingOrigin shiftingOriginMap;
    //private bool originHasShifted = false;
}

