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
    public VtsObjectShiftingOriginBase focusObject;
    public float distanceThreshold = 2000;

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
        Debug.Log("Performing Origin Shift");
        Debug.Assert(focusObject.GetComponent<VtsObjectShiftingOriginBase>()); // the focus object must be moved
        Vector3 fp = focusObject.transform.position;
        double[] originalNavigationPoint = umap.UnityToVtsNavigation(new double[3] { 0, 0, 0 });
        double[] targetNavigationPoint = umap.UnityToVtsNavigation(VtsUtil.U2V3(fp));
        float Yrot = (float)(targetNavigationPoint[0] - originalNavigationPoint[0]) * Mathf.Sign((float)originalNavigationPoint[1]);
        //Debug.Log("navigation coordinates of origin: " + VtsUtil.V2U3(targetNavigationPoint).ToString("F5"));
        if (!VtsMapMakeLocal.MakeLocal(umap, targetNavigationPoint))
            Debug.Assert(false, "failed shifting origin");
        foreach (VtsObjectShiftingOriginBase obj in FindObjectsOfType<VtsObjectShiftingOriginBase>())
        {
            // ask if the object allows to be transformed by this map
            if (obj.enabled && obj.OnBeforeOriginShift(this))
            {
                // only transform object's topmost ancestor
                // its childs will inherit the transformation change
                // other objects may still be interested to react to the change
                // furthermore, the object is only shifted once even if it has multiple VtsObjectShiftingOriginBase components
                if (!obj.transform.parent || !obj.transform.parent.GetComponentInParent<VtsObjectShiftingOriginBase>()
                    && obj == obj.GetComponents<VtsObjectShiftingOriginBase>()[0])
                {
                    obj.transform.localPosition -= fp;
                    obj.transform.RotateAround(new Vector3(0,0,0), new Vector3(0, 1, 0), Yrot);
                }
                // notify the object that it was transformed
                obj.OnAfterOriginShift();
            }
        }
    }
}
