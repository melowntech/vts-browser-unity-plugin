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

[RequireComponent(typeof(VtsMap))]
public class VtsMapMakeLocal : MonoBehaviour
{
    public static bool MakeLocal(MonoBehaviour behavior, double[] longLatAlt)
    {
        Util.CheckArray(longLatAlt, 3);
        Map map = behavior.GetComponent<VtsMap>().map;
        if (!map.GetMapConfigAvailable())
            return false;
        double[] p = map.Convert(longLatAlt, Srs.Navigation, Srs.Physical);
        { // swap YZ
            double tmp = p[1];
            p[1] = p[2];
            p[2] = tmp;
        }
        Vector3 v = Vector3.Scale(VtsUtil.V2U3(p), behavior.transform.localScale);
        float m = v.magnitude;
        behavior.transform.position = new Vector3(0, -m, 0); // altitude
        behavior.transform.rotation =
            Quaternion.Euler(0, (float)longLatAlt[0] + 90.0f, 0) // align to north
            * Quaternion.FromToRotation(-v, behavior.transform.position); // latlon
        return true;
    }

    void Update()
    {
        if (MakeLocal(this, new double[3] { longitude, latitude, altitude }))
        {
            if (singleUse)
                Destroy(this);
        }
    }

    public double longitude;
    public double latitude;
    public double altitude;
    public bool singleUse = true;
}
