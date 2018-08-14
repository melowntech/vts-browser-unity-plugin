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
using UnityEngine.Rendering;
using vts;

public enum VtsDataControl
{
    Unity,
    Vts,
}

public abstract class VtsCameraBase : MonoBehaviour
{
    public VtsCameraBase()
    {
        CamOverrideViewDel = CamOverrideView;
        CamOverrideParametersDel = CamOverrideParameters;
    }

    protected virtual void Start()
    {
        cam = GetComponent<Camera>();
        camTrans = GetComponent<Transform>();
        mapTrans = mapObject.GetComponent<Transform>();

        shaderPropertyMainTex = Shader.PropertyToID("_MainTex");
        shaderPropertyMaskTex = Shader.PropertyToID("_MaskTex");
        shaderPropertyUvMat = Shader.PropertyToID("_UvMat");
        shaderPropertyUvClip = Shader.PropertyToID("_UvClip");
        shaderPropertyColor = Shader.PropertyToID("_Color");
        shaderPropertyFlags = Shader.PropertyToID("_Flags");

        shaderPropertyAtmViewInv = Shader.PropertyToID("vtsUniAtmViewInv");
        shaderPropertyAtmColorHorizon = Shader.PropertyToID("vtsUniAtmColorHorizon");
        shaderPropertyAtmColorZenith = Shader.PropertyToID("vtsUniAtmColorZenith");
        shaderPropertyAtmSizes = Shader.PropertyToID("vtsUniAtmSizes");
        shaderPropertyAtmCoefficients = Shader.PropertyToID("vtsUniAtmCoefs");
        shaderPropertyAtmCameraPosition = Shader.PropertyToID("vtsUniAtmCameraPosition");
        shaderPropertyAtmCorners = Shader.PropertyToID("uniCorners");

        backgroundMaterial = Instantiate(backgroundMaterial);
        backgroundCmds = new CommandBuffer();
        backgroundCmds.name = "Vts Atmosphere Background";
        cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, backgroundCmds);
        cam.AddCommandBuffer(CameraEvent.BeforeLighting, backgroundCmds);
    }

    private readonly Map.DoubleArrayHandler CamOverrideViewDel;
    private void CamOverrideView(ref double[] values)
    {
        double[] Mu = Math.Mul44x44(VtsUtil.U2V44(mapTrans.localToWorldMatrix), VtsUtil.U2V44(VtsUtil.SwapYZ));
        // view matrix
        if (controlTransformation == VtsDataControl.Vts)
        {
            VtsUtil.Matrix2Transform(camTrans, VtsUtil.V2U44(Math.Mul44x44(Math.Inverse44(Math.Mul44x44(values, Mu)), VtsUtil.U2V44(VtsUtil.InvertZ))));
        }
        else
        {
            values = Math.Mul44x44(VtsUtil.U2V44(cam.worldToCameraMatrix), Mu);
        }
    }

    private readonly Map.CameraParamsHandler CamOverrideParametersDel;
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
        CameraUpdate();
        UpdateBackground();
    }

    protected abstract void CameraUpdate();

    protected void UpdateMaterial(Material mat)
    {
        VtsTexture tex = draws.celestial.atmosphere.densityTexture as VtsTexture;
        if (atmosphereEnabled && tex != null)
        {
            var cel = draws.celestial;
            var atm = cel.atmosphere;
            mat.SetVector(shaderPropertyAtmSizes, new Vector4(
                (float)(atm.boundaryThickness / cel.majorRadius),
                (float)(cel.majorRadius / cel.minorRadius),
                (float)(1.0 / cel.majorRadius),
                0));
            mat.SetVector(shaderPropertyAtmCoefficients, new Vector4(
                (float)atm.horizontalExponent,
                (float)atm.colorGradientExponent,
                0,
                0));
            mat.SetVector(shaderPropertyAtmCameraPosition, VtsUtil.V2U3(draws.camera.eye) / (float)cel.majorRadius);
            mat.SetMatrix(shaderPropertyAtmViewInv, VtsUtil.V2U44(Math.Inverse44(draws.camera.view)));
            mat.SetVector(shaderPropertyAtmColorHorizon, VtsUtil.V2U4(atm.colorHorizon));
            mat.SetVector(shaderPropertyAtmColorZenith, VtsUtil.V2U4(atm.colorZenith));
            mat.SetTexture("vtsTexAtmDensity", tex.Get());
            mat.EnableKeyword("VTS_ATMOSPHERE");
        }
        else
        {
            mat.DisableKeyword("VTS_ATMOSPHERE");
        }
    }

    private void UpdateBackground()
    {
        backgroundCmds.Clear();
        if (atmosphereEnabled)
        {
            UpdateMaterial(backgroundMaterial);
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
                backgroundMaterial.SetVectorArray(shaderPropertyAtmCorners, c);
            }
            backgroundCmds.DrawMesh(backgroundMesh, Matrix4x4.identity, backgroundMaterial, 0, -1);
        }
    }

    public GameObject mapObject;

    public VtsDataControl controlTransformation;
    public VtsDataControl controlNearFar;
    public VtsDataControl controlFov;

    public bool atmosphereEnabled = false;
    public Material backgroundMaterial;
    public UnityEngine.Mesh backgroundMesh;

    protected int shaderPropertyMainTex;
    protected int shaderPropertyMaskTex;
    protected int shaderPropertyUvMat;
    protected int shaderPropertyUvClip;
    protected int shaderPropertyColor;
    protected int shaderPropertyFlags;

    protected int shaderPropertyAtmViewInv;
    protected int shaderPropertyAtmColorHorizon;
    protected int shaderPropertyAtmColorZenith;
    protected int shaderPropertyAtmSizes;
    protected int shaderPropertyAtmCoefficients;
    protected int shaderPropertyAtmCameraPosition;
    protected int shaderPropertyAtmCorners;

    protected readonly Draws draws = new Draws();

    protected Camera cam;
    protected Transform camTrans;
    protected Transform mapTrans;

    private CommandBuffer backgroundCmds;
}

