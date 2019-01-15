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
using System.Text;
using UnityEngine;
using vts;

[DisallowMultipleComponent]
public class VtsColliderProbe : MonoBehaviour
{
    private void Start()
    {
        vmap = mapObject.GetComponent<VtsMap>().GetVtsMap();
        vcam = new vts.Camera(vmap);
        probTrans = GetComponent<Transform>();
        mapTrans = mapObject.GetComponent<Transform>();
    }

    private void FixedUpdate()
    {
        // update current colliders
        vcam.RenderUpdate();
        draws.Load(vmap, vcam);
        UpdateParts();

        // prepare for next frame
        vcam.SetViewportSize(1, 1);
        double[] Mu = Math.Mul44x44(VtsUtil.U2V44(mapTrans.localToWorldMatrix), VtsUtil.U2V44(VtsUtil.SwapYZ));
        double[] view = Math.Mul44x44(VtsUtil.U2V44(probTrans.localToWorldMatrix.inverse), Mu);
        vcam.SetView(view);

        // enforce fixed traversal mode
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{ \"fixedTraversalDistance\":").Append(collidersDistance).Append(", \"fixedTraversalLod\":").Append(collidersLod).Append(", \"traverseModeSurfaces\":4, \"traverseModeGeodata\":0 }");
            vcam.SetOptions(builder.ToString());
        }

        // statistics
        Statistics = vcam.GetStatistics();
    }

    public void OriginShifted()
    {
        originHasShifted = true;
    }

    private void UpdateParts()
    {
        if (originHasShifted)
        {
            originHasShifted = false;
            foreach (var p in partsCache)
                Destroy(p.Value);
            partsCache.Clear();
        }

        VtsMapShiftingOrigin shiftingOriginMap = mapObject.GetComponent<VtsMapShiftingOrigin>();
        double[] conv = Math.Mul44x44(Math.Mul44x44(VtsUtil.U2V44(mapTrans.localToWorldMatrix), VtsUtil.U2V44(VtsUtil.SwapYZ)), Math.Inverse44(draws.camera.view));

        Dictionary<VtsMesh, DrawTask> tasksByMesh = new Dictionary<VtsMesh, DrawTask>();
        foreach (DrawTask t in draws.colliders)
        {
            VtsMesh k = t.mesh as VtsMesh;
            if (!tasksByMesh.ContainsKey(k))
                tasksByMesh.Add(k, t);
        }

        HashSet<VtsMesh> partsToRemove = new HashSet<VtsMesh>(partsCache.Keys);

        foreach (KeyValuePair<VtsMesh, DrawTask> tbm in tasksByMesh)
        {
            if (!partsCache.ContainsKey(tbm.Key))
            {
                GameObject o = Instantiate(colliderPrefab);
                partsCache.Add(tbm.Key, o);
                UnityEngine.Mesh msh = (tbm.Value.mesh as VtsMesh).Get();
                o.GetComponent<MeshCollider>().sharedMesh = msh;
                o.GetComponent<VtsObjectShiftingOrigin>().map = shiftingOriginMap;
                VtsUtil.Matrix2Transform(o.transform, VtsUtil.V2U44(Math.Mul44x44(conv, System.Array.ConvertAll(tbm.Value.data.mv, System.Convert.ToDouble))));
            }
            partsToRemove.Remove(tbm.Key);
        }

        foreach (VtsMesh m in partsToRemove)
        {
            Destroy(partsCache[m]);
            partsCache.Remove(m);
        }
    }

    public GameObject mapObject;
    public GameObject colliderPrefab;

    public double collidersDistance = 200;
    public uint collidersLod = 18;

#pragma warning disable
    [SerializeField, TextArea(0, 20)] private string Statistics = "This will show statistics at play";
#pragma warning restore

    private readonly Draws draws = new Draws();
    private readonly Dictionary<VtsMesh, GameObject> partsCache = new Dictionary<VtsMesh, GameObject>();

    private Map vmap;
    private vts.Camera vcam;
    private Transform probTrans;
    private Transform mapTrans;
    private bool originHasShifted = false;
}

