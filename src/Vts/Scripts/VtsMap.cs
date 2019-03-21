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

using System.Threading;
using UnityEngine;
using vts;

[DisallowMultipleComponent]
public class VtsMap : MonoBehaviour
{
    private void Awake()
    {
        VtsLog.Init();
        VtsResources.Init();
        Debug.Assert(map == null);
        map = new Map(CreateConfig);
        map.EventLoadTexture += VtsResources.LoadTexture;
        map.EventLoadMesh += VtsResources.LoadMesh;
        dataThread = new Thread(new ThreadStart(DataEntry));
        dataThread.Start();
        map.RenderInitialize();
    }

    private void Start()
    {
        map.SetMapconfigPath(ConfigUrl, AuthUrl);
        if (RunConfig.Length > 0)
            map.SetOptions(RunConfig);
    }

    private void OnDestroy()
    {
        Debug.Assert(map != null);
        map.RenderDeinitialize();
        dataThread.Join();
        map.Dispose();
        map = null;
    }

    private void Update()
    {
        Util.Log(LogLevel.debug, "Unity update frame index: " + frameIndex++);
        Debug.Assert(map != null);
        map.RenderUpdate(Time.deltaTime);

        // statistics
        Statistics = map.GetStatistics();
    }

    private void DataEntry()
    {
        map.DataAllRun();
    }

    public double[] UnityToVtsNavigation(double[] point)
    {
        Util.CheckArray(point, 3);
        { // convert from unity world to (local) vts physical
            double[] point4 = new double[4] { point[0], point[1], point[2], 1 };
            //point4 = Math.Mul44x4(VtsUtil.U2V44(transform.worldToLocalMatrix), point4);
            point4 = Math.Mul44x4(Math.Inverse44(VtsUtil.U2V44(transform.localToWorldMatrix)), point4);
            point = new double[3] { point4[0], point4[1], point4[2] };
        }
        { // swap YZ
            double tmp = point[1];
            point[1] = point[2];
            point[2] = tmp;
        }
        point = map.Convert(point, Srs.Physical, Srs.Navigation);
        return point;
    }

    public double[] VtsNavigationToUnity(double[] point)
    {
        Util.CheckArray(point, 3);
        point = map.Convert(point, Srs.Navigation, Srs.Physical);
        { // swap YZ
            double tmp = point[1];
            point[1] = point[2];
            point[2] = tmp;
        }
        { // convert from (local) vts physical to unity world
            double[] point4 = new double[4] { point[0], point[1], point[2], 1 };
            point4 = Math.Mul44x4(VtsUtil.U2V44(transform.localToWorldMatrix), point4);
            point = new double[3] { point4[0], point4[1], point4[2] };
        }
        return point;
    }

    private Thread dataThread;
    private uint frameIndex;

    [SerializeField] private string ConfigUrl = "https://cdn.melown.com/vts/melown2015/unity/world/mapConfig.json";
    [SerializeField] private string AuthUrl = "";
    [SerializeField, TextArea] private string CreateConfig;
    [SerializeField, TextArea] private string RunConfig = "{ \"targetResourcesMemoryKB\":500000 }";

#pragma warning disable
    [SerializeField, TextArea(0,20)] private string Statistics = "This will show statistics at play";
#pragma warning restore

    private Map map;

    public Map GetVtsMap()
    {
        return map;
    }
}
