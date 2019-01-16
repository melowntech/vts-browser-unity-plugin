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

[DisallowMultipleComponent]
[RequireComponent(typeof(VtsMap))]
public class VtsMapShiftingOrigin : MonoBehaviour
{
    public GameObject focusObject;
    public float distanceThreshold = 2000;
    public bool updateColliders = false;

    private VtsMap umap;
    private Map vmap;

    private void Start()
    {
        umap = GetComponent<VtsMap>();
        vmap = umap.GetVtsMap();
    }

    private void LateUpdate()
    {
        if (!vmap.GetMapconfigAvailable())
            return;
        Vector3 fp = focusObject.transform.position;
        if (fp.sqrMagnitude > distanceThreshold * distanceThreshold)
            PerformShift();
    }

    private void PerformShift()
    {
        // the focus object must be moved
        Debug.Assert(focusObject.GetComponentInParent<VtsObjectShiftingOriginBase>());

        // compute the transformation change
        double[] originalNavigationPoint = umap.UnityToVtsNavigation(zero3d);
        double[] targetNavigationPoint = umap.UnityToVtsNavigation(VtsUtil.U2V3(focusObject.transform.position));
        if (!VtsMapMakeLocal.MakeLocal(umap, targetNavigationPoint))
        {
            Debug.Assert(false, "failed shifting origin");
            return;
        }
        Vector3 move = -focusObject.transform.position;
        float Yrot = (float)(targetNavigationPoint[0] - originalNavigationPoint[0]) * Mathf.Sign((float)originalNavigationPoint[1]);

        // find objects that will be transformed
        var objs = new System.Collections.Generic.List<VtsObjectShiftingOriginBase>();
        foreach (VtsObjectShiftingOriginBase obj in FindObjectsOfType<VtsObjectShiftingOriginBase>())
        {
            // ask if the object allows to be transformed by this map
            if (obj.enabled && obj.OnBeforeOriginShift(this))
                objs.Add(obj);
        }

        // actually transform the objects
        foreach (VtsObjectShiftingOriginBase obj in objs)
        {
            // only transform object's topmost ancestor - its childs will inherit the change
            // an object is shifted only once even if it has multiple VtsObjectShiftingOriginBase components
            if (!obj.transform.parent || !obj.transform.parent.GetComponentInParent<VtsObjectShiftingOriginBase>()
                && obj == obj.GetComponents<VtsObjectShiftingOriginBase>()[0])
            {
                obj.transform.localPosition += move;
                obj.transform.RotateAround(Vector3.zero, Vector3.up, Yrot);
            }
        }

        // notify the object that it was transformed
        foreach (VtsObjectShiftingOriginBase obj in objs)
            obj.OnAfterOriginShift();

        // force all objects cameras to recompute positions -> improves precision
        foreach (VtsCameraBase cam in FindObjectsOfType<VtsCameraBase>())
        cam.OriginShifted();

        // force all collider probes to recompute positions -> improves precision
        // warning: this has big performance impact!
        if (updateColliders)
        {
            foreach (VtsColliderProbe col in FindObjectsOfType<VtsColliderProbe>())
                col.OriginShifted();
        }
    }

    static readonly double[] zero3d = new double[3] { 0, 0, 0 };
}
