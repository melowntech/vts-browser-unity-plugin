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
using UnityEngine.Rendering;
using vts;

public enum VtsDataControl
{
    Unity,
    Vts,
}

public class VtsCamera : MonoBehaviour
{
    public VtsCamera()
    {
        CamOverrideViewDel = CamOverrideView;
        CamOverrideParametersDel = CamOverrideParameters;
    }

    private void Start()
    {
        camTrans = GetComponent<Transform>();
        mapTrans = mapObject.GetComponent<Transform>();

        cam = GetComponent<Camera>();
        cam.cullingMask |= 1 << partLayer;

        shaderPropertyMainTex = Shader.PropertyToID("_MainTex");
        shaderPropertyMaskTex = Shader.PropertyToID("_MaskTex");
        shaderPropertyUvMat = Shader.PropertyToID("_UvMat");
        shaderPropertyUvClip = Shader.PropertyToID("_UvClip");
        shaderPropertyColor = Shader.PropertyToID("_Color");
        shaderPropertyFlags = Shader.PropertyToID("_Flags");

        shaderPropertyAtmViewInv = Shader.PropertyToID("vtsUniAtmViewInv");
        shaderPropertyAtmColorLow = Shader.PropertyToID("vtsUniAtmColorLow");
        shaderPropertyAtmColorHigh = Shader.PropertyToID("vtsUniAtmColorHigh");
        shaderPropertyAtmParams = Shader.PropertyToID("vtsUniAtmParams");
        shaderPropertyAtmCameraPosition = Shader.PropertyToID("vtsUniAtmCameraPosition");
        shaderPropertyAtmCorners = Shader.PropertyToID("uniCorners");

        atmosphereMaterial = Instantiate(atmosphereMaterial);
        backgroundCmds = new CommandBuffer();
        backgroundCmds.name = "Vts Atmosphere";
        cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, backgroundCmds);
    }

    private readonly Map.CameraOverrideHandler CamOverrideViewDel;
    private void CamOverrideView(ref double[] values)
    {
        double[] Mu = Math.Mul44x44(VtsUtil.U2V44(mapTrans.localToWorldMatrix), VtsUtil.U2V44(SwapYZ));
        // view matrix
        if (controlTransformation == VtsDataControl.Vts)
        {
            VtsUtil.Matrix2Transform(camTrans, VtsUtil.V2U44(Math.Mul44x44(Math.Inverse44(Math.Mul44x44(values, Mu)), VtsUtil.U2V44(InvertZ))));
        }
        else
        {
            values = Math.Mul44x44(VtsUtil.U2V44(cam.worldToCameraMatrix), Mu);
        }
    }

    private readonly Map.CameraOverrideParamsHandler CamOverrideParametersDel;
    private void CamOverrideParameters(ref double fov, ref double aspect, ref double near, ref double far)
    {
        // fov
        if (controlFov == VtsDataControl.Vts)
            cam.fieldOfView = (float)fov;
        else
            fov = cam.fieldOfView;
        // near & far
        if (controlNearFar == VtsDataControl.Vts)
        {
            cam.nearClipPlane = (float)near;
            cam.farClipPlane = (float)far;
        }
        else
        {
            near = cam.nearClipPlane;
            far = cam.farClipPlane;
        }
    }

    private void Update()
    {
        Map map = mapObject.GetComponent<VtsMap>().map;
        map.SetWindowSize((uint)cam.pixelWidth, (uint)cam.pixelHeight);
        map.EventCameraView += CamOverrideViewDel;
        map.EventCameraFovAspectNearFar += CamOverrideParametersDel;
        map.RenderTickRender();
        map.EventCameraView -= CamOverrideViewDel;
        map.EventCameraFovAspectNearFar -= CamOverrideParametersDel;
        draws.Load(map);
        UpdateParts();
        UpdateBackground();
        if (atmosphereEnabled)
            Shader.EnableKeyword("VTS_ATMOSPHERE");
        else
            Shader.DisableKeyword("VTS_ATMOSPHERE");
    }

    private void UpdateMaterial(Material mat)
    {
        if (atmosphereEnabled)
        {
            var cel = draws.celestial;
            var atm = cel.atmosphere;
            mat.SetVector(shaderPropertyAtmParams, new Vector4((float)(atm.thickness / cel.majorRadius), (float)atm.horizontalExponent, (float)(cel.minorRadius / cel.majorRadius), (float)cel.majorRadius));
            mat.SetVector(shaderPropertyAtmCameraPosition, VtsUtil.V2U3(draws.camera.eye) / (float)cel.majorRadius);
            mat.SetMatrix(shaderPropertyAtmViewInv, VtsUtil.V2U44(Math.Inverse44(draws.camera.view)));
            mat.SetVector(shaderPropertyAtmColorLow, VtsUtil.V2U4(atm.colorLow));
            mat.SetVector(shaderPropertyAtmColorHigh, VtsUtil.V2U4(atm.colorHigh));
        }
        else
        {
            mat.SetVector(shaderPropertyAtmParams, new Vector4(0,0,0,0));
        }
    }

    private void UpdateParts()
    {
        UpdateParts(draws.opaque);
        //UpdateParts(draws.transparent);
        //UpdateParts(draws.geodata);
        //UpdateParts(draws.infographics);
    }

    private void UpdateParts(List<DrawTask> allTasks)
    {
        double[] conv = Math.Mul44x44(Math.Mul44x44(VtsUtil.U2V44(mapTrans.localToWorldMatrix), VtsUtil.U2V44(SwapYZ)), Math.Inverse44(draws.camera.view));

        Dictionary<VtsMesh, List<DrawTask>> tasksByMesh = new Dictionary<VtsMesh, List<DrawTask>>();
        foreach (DrawTask t in allTasks)
        {
            VtsMesh k = t.mesh as VtsMesh;
            if (!tasksByMesh.ContainsKey(k))
                tasksByMesh.Add(k, new List<DrawTask>());
            tasksByMesh[k].Add(t);
        }

        HashSet<VtsMesh> partsToRemove = new HashSet<VtsMesh>(partsCache.Keys);

        foreach (KeyValuePair<VtsMesh, List<DrawTask>> tbm in tasksByMesh)
        {
            if (!partsCache.ContainsKey(tbm.Key))
                partsCache.Add(tbm.Key, new List<GameObject>(tbm.Value.Count));
            UpdateParts(tbm.Value, partsCache[tbm.Key], conv);
            partsToRemove.Remove(tbm.Key);
        }

        foreach (VtsMesh m in partsToRemove)
        {
            foreach (GameObject o in partsCache[m])
                Destroy(o);
            partsCache.Remove(m);
        }
    }

    private void UpdateParts(List<DrawTask> tasks, List<GameObject> parts, double[] conv)
    {
        if (parts.Count == tasks.Count)
            return;
        if (parts.Count > 0)
        {
            foreach (GameObject p in parts)
                Destroy(p);
            parts.Clear();
        }
        bool first = true;
        foreach (DrawTask t in tasks)
        {
            GameObject o = Instantiate(partPrefab);
            parts.Add(o);
            o.layer = partLayer;
            UnityEngine.Mesh msh = (tasks[0].mesh as VtsMesh).Get();
            o.GetComponent<MeshFilter>().mesh = msh;
            if (first && generateColliders)
                o.GetComponent<MeshCollider>().sharedMesh = msh;
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
            first = false;
        }
    }

    private void UpdateBackground()
    {
        backgroundCmds.Clear();
        if (atmosphereEnabled)
        {
            UpdateMaterial(atmosphereMaterial);
            {
                double mr = draws.celestial.majorRadius;
                double[] camPos = draws.camera.eye;
                for (int i = 0; i < 3; i++)
                    camPos[i] /= mr;
                double[] scaleMat = new double[16] { mr, 0,0,0,0, mr, 0,0,0,0, mr, 0,0,0,0, 1 };
                double[] viewProj = Math.Mul44x44(draws.camera.proj, draws.camera.view);
                double[] inv = Math.Inverse44(Math.Mul44x44(viewProj, scaleMat));
                double[][] cornersD = new double[4][]
                {
                    Math.Mul44x4(inv, new double[4] { -1, -1, 0, 1 }),
                    Math.Mul44x4(inv, new double[4] { +1, -1, 0, 1 }),
                    Math.Mul44x4(inv, new double[4] { -1, +1, 0, 1 }),
                    Math.Mul44x4(inv, new double[4] { +1, +1, 0, 1 })
                };
                Vector4[] c = new Vector4[4];
                for (int i = 0; i < 4; i++)
                {
                    double[] corner = new double[3];
                    for (int j = 0; j < 3; j++)
                        corner[j] = cornersD[i][j] / cornersD[i][3] - camPos[j];
                    corner = Math.Normalize3(corner);
                    c[i] = new Vector4((float)corner[0], (float)corner[1], (float)corner[2], 0.0f);
                }
                atmosphereMaterial.SetVectorArray(shaderPropertyAtmCorners, c);
            }
            backgroundCmds.DrawMesh(atmosphereMesh, Matrix4x4.identity, atmosphereMaterial, 0, -1);
        }
    }

    public GameObject mapObject;

    public VtsDataControl controlTransformation;
    public VtsDataControl controlNearFar;
    public VtsDataControl controlFov;

    public GameObject partPrefab;
    public int partLayer = 31;

    public bool atmosphereEnabled = false;
    public Material atmosphereMaterial;
    public UnityEngine.Mesh atmosphereMesh;

    public bool generateColliders = false;

    private int shaderPropertyMainTex;
    private int shaderPropertyMaskTex;
    private int shaderPropertyUvMat;
    private int shaderPropertyUvClip;
    private int shaderPropertyColor;
    private int shaderPropertyFlags;

    private int shaderPropertyAtmViewInv;
    private int shaderPropertyAtmColorLow;
    private int shaderPropertyAtmColorHigh;
    private int shaderPropertyAtmParams;
    private int shaderPropertyAtmCameraPosition;
    private int shaderPropertyAtmCorners;

    private readonly Draws draws = new Draws();
    private readonly Dictionary<VtsMesh, List<GameObject>> partsCache = new Dictionary<VtsMesh, List<GameObject>>();

    private Camera cam;
    private Transform camTrans;
    private Transform mapTrans;

    private CommandBuffer backgroundCmds;

    private static readonly Matrix4x4 SwapYZ = new Matrix4x4(
        new Vector4(1, 0, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 1, 0, 0),
        new Vector4(0, 0, 0, 1)
        );

    private static readonly Matrix4x4 InvertZ = Matrix4x4.Scale(new Vector3(1, 1, -1));
}

