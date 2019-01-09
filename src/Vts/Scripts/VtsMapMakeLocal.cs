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
public class VtsMapMakeLocal : MonoBehaviour
{
    public static bool MakeLocal(VtsMap umap, double[] navPt)
    {
        Util.CheckArray(navPt, 3);
        Map map = umap.GetVtsMap();
        if (!map.GetMapconfigAvailable())
            return false;
        double[] p = map.Convert(navPt, Srs.Navigation, Srs.Physical);
        { // swap YZ
            double tmp = p[1];
            p[1] = p[2];
            p[2] = tmp;
        }
        Vector3 v = Vector3.Scale(VtsUtil.V2U3(p), umap.transform.localScale);
        if (map.GetProjected())
        {
            umap.transform.position = -v;
        }
        else
        {
            float m = v.magnitude;
            umap.transform.position = new Vector3(0, -m, 0); // altitude
            umap.transform.rotation =
                Quaternion.Euler(0, (float)navPt[0] + 90.0f, 0) // align to north
                * Quaternion.FromToRotation(-v, umap.transform.position); // latlon
        }
        return true;
    }

    private void Update()
    {
        if (MakeLocal(GetComponent<VtsMap>(), new double[3] { x, y, z }))
        {
            if (singleUse)
                Destroy(this);
        }
    }

    [Tooltip("Navigation SRS X (Longitude)")]
    public double x;

    [Tooltip("Navigation SRS Y (Latitude)")]
    public double y;

    [Tooltip("Navigation SRS Z (Altitude)")]
    public double z;

    public bool singleUse = true;
}
