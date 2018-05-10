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

        map.EventLoadTexture += VtsResources.LoadTexture;
        map.EventLoadMesh += VtsResources.LoadMesh;
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
