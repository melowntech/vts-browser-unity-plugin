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
    Unity, // the property is assigned from the unity camera component to the vts camera
    Vts, // the property is computed by vts plugin
}

// this class is common functionality for both vts cameras
// it handles view and proj matrices, shader properties, rendering background
[RequireComponent(typeof(UnityEngine.Camera))]
public abstract class VtsCameraBase : MonoBehaviour
{
    protected virtual void Start()
    {
        vmap = mapObject.GetComponent<VtsMap>().Map;
        Debug.Assert(vmap != null);
        vcam = new vts.Camera(vmap);
        ucam = GetComponent<UnityEngine.Camera>();
        camTrans = GetComponent<Transform>();
        mapTrans = mapObject.GetComponent<Transform>();

        shaderPropertyMainTex = Shader.PropertyToID("_MainTex");
        shaderPropertyMaskTex = Shader.PropertyToID("_MaskTex");
        shaderPropertyUvMat = Shader.PropertyToID("_UvMat");
        shaderPropertyUvClip = Shader.PropertyToID("_UvClip");
        shaderPropertyColor = Shader.PropertyToID("_Color");
        shaderPropertyBlendingCoverage = Shader.PropertyToID("_BlendingCoverage");
        shaderPropertyFlags = Shader.PropertyToID("_Flags");
        shaderPropertyFrameIndex = Shader.PropertyToID("_FrameIndex");
        shaderPropertyTexBlueNoise = Shader.PropertyToID("_BlueNoiseTex");

        shaderPropertyAtmViewInv = Shader.PropertyToID("vtsUniAtmViewInv");
        shaderPropertyAtmColorHorizon = Shader.PropertyToID("vtsUniAtmColorHorizon");
        shaderPropertyAtmColorZenith = Shader.PropertyToID("vtsUniAtmColorZenith");
        shaderPropertyAtmSizes = Shader.PropertyToID("vtsUniAtmSizes");
        shaderPropertyAtmCoefficients = Shader.PropertyToID("vtsUniAtmCoefs");
        shaderPropertyAtmCameraPosition = Shader.PropertyToID("vtsUniAtmCameraPosition");
        shaderPropertyAtmCorners = Shader.PropertyToID("uniCorners");
        shaderPropertyAtmTexDensity = Shader.PropertyToID("vtsTexAtmDensity");

        backgroundMaterial = Instantiate(backgroundMaterial);
        backgroundCmds = new CommandBuffer();
        backgroundCmds.name = "Vts Atmosphere Background";
        ucam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, backgroundCmds);
        ucam.AddCommandBuffer(CameraEvent.BeforeLighting, backgroundCmds);
        propertyBlock = new MaterialPropertyBlock();

        if (Config.Length > 0)
            vcam.SetOptions(Config);
    }

    private void Update()
    {
        // sync transformation etc.
        vcam.SetViewportSize((uint)ucam.pixelWidth, (uint)ucam.pixelHeight);
        double[] Mu = Math.Mul44x44(VtsUtil.U2V44(mapTrans.localToWorldMatrix), VtsUtil.U2V44(VtsUtil.SwapYZ));
        if (controlTransformation == VtsDataControl.Vts)
        {
            double[] view = vcam.GetView();
            VtsUtil.Matrix2Transform(camTrans, VtsUtil.V2U44(Math.Mul44x44(Math.Inverse44(Math.Mul44x44(view, Mu)), VtsUtil.U2V44(VtsUtil.InvertZ))));
        }
        else
        {
            double[] view = Math.Mul44x44(VtsUtil.U2V44(ucam.worldToCameraMatrix), Mu);
            vcam.SetView(view);
        }
        if (controlNearFar == VtsDataControl.Vts)
        {
            double n, f;
            vcam.SuggestedNearFar(out n, out f);
            ucam.nearClipPlane = (float)n;
            ucam.farClipPlane = (float)f;
        }
        vcam.SetProj(ucam.fieldOfView, ucam.nearClipPlane, ucam.farClipPlane);
        Matrix4x4 proj = VtsUtil.V2U44(vcam.GetProj());
        if (proj[0] == proj[0] && proj[0] != 0)
            ucam.projectionMatrix = proj;

        // draw
        vcam.RenderUpdate();
        draws.Load(vmap, vcam);
        PrepareShaderData();
        CameraDraw();
        UpdateBackground();

        // statistics
        Statistics = vcam.GetStatistics();
    }

    protected abstract void CameraDraw();

    public virtual void OriginShifted()
    {}

    private void PrepareShaderData()
    {
        var cel = draws.celestial;
        var atm = cel.atmosphere;
        shaderValueAtmSizes = new Vector4(
                (float)(atm.boundaryThickness / cel.majorRadius),
                (float)(cel.majorRadius / cel.minorRadius),
                (float)(1.0 / cel.majorRadius),
                0);
        shaderValueAtmCoefficients = new Vector4(
                (float)atm.horizontalExponent,
                (float)atm.colorGradientExponent,
                0,
                0);
        shaderValueAtmCameraPosition = VtsUtil.V2U3(draws.camera.eye) / (float)cel.majorRadius;
        shaderValueAtmViewInv = VtsUtil.V2U44(Math.Inverse44(draws.camera.view));
        shaderValueAtmColorHorizon = VtsUtil.V2U4(atm.colorHorizon);
        shaderValueAtmColorZenith = VtsUtil.V2U4(atm.colorZenith);
        shaderValueAtmEnabled = Shader.IsKeywordEnabled("VTS_ATMOSPHERE") && draws.celestial.atmosphere.densityTexture != null;
        shaderValueFrameIndex++;
    }

    private void UpdateAtmosphere(MaterialPropertyBlock mat)
    {
        mat.SetVector(shaderPropertyAtmCameraPosition, shaderValueAtmCameraPosition);
        mat.SetMatrix(shaderPropertyAtmViewInv, shaderValueAtmViewInv);
    }

    private void InitAtmosphere(MaterialPropertyBlock mat)
    {
        mat.SetVector(shaderPropertyAtmSizes, shaderValueAtmSizes);
        mat.SetVector(shaderPropertyAtmCoefficients, shaderValueAtmCoefficients);
        mat.SetVector(shaderPropertyAtmColorHorizon, shaderValueAtmColorHorizon);
        mat.SetVector(shaderPropertyAtmColorZenith, shaderValueAtmColorZenith);
        mat.SetTexture(shaderPropertyAtmTexDensity, (draws.celestial.atmosphere.densityTexture as VtsTexture).Get());
    }

    protected void UpdateMaterial(MaterialPropertyBlock mat, DrawSurfaceTask t)
    {
        if (shaderValueAtmEnabled)
            UpdateAtmosphere(mat);
        mat.SetVector(shaderPropertyUvClip, VtsUtil.V2U4(t.data.uvClip));
        mat.SetVector(shaderPropertyColor, VtsUtil.V2U4(t.data.color));
        mat.SetFloat(shaderPropertyBlendingCoverage, float.IsNaN(t.data.blendingCoverage) ? -1 : t.data.blendingCoverage);
        mat.SetInt(shaderPropertyFrameIndex, shaderValueFrameIndex);
    }

    protected void InitMaterial(MaterialPropertyBlock mat, DrawSurfaceTask t)
    {
        if (shaderValueAtmEnabled)
            InitAtmosphere(mat);
        UpdateMaterial(mat, t);
        mat.SetMatrix(shaderPropertyUvMat, VtsUtil.V2U33(t.data.uvm));
        int flags = 0; // _Flags: mask, monochromatic, flat shading, uv source
        if (t.texMask != null)
        {
            flags |= 1 << 0;
            mat.SetTexture(shaderPropertyMaskTex, (t.texMask as VtsTexture).Get());
        }
        if ((t.texColor as VtsTexture).monochromatic)
            flags |= 1 << 1;
        if (t.data.externalUv)
            flags |= 1 << 3;
        mat.SetInt(shaderPropertyFlags, flags);
        mat.SetTexture(shaderPropertyMainTex, (t.texColor as VtsTexture).Get());
        mat.SetTexture(shaderPropertyTexBlueNoise, blueNoiseTexture);
    }

    private void UpdateBackground()
    {
        backgroundCmds.Clear();
        propertyBlock.Clear();
        if (!shaderValueAtmEnabled)
            return;

        InitAtmosphere(propertyBlock);
        UpdateAtmosphere(propertyBlock);
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
        propertyBlock.SetVectorArray(shaderPropertyAtmCorners, c);
        backgroundCmds.DrawMesh(backgroundMesh, Matrix4x4.identity, backgroundMaterial, 0, -1, propertyBlock);
    }

    public GameObject mapObject;

    public VtsDataControl controlTransformation;
    public VtsDataControl controlNearFar;

#pragma warning disable
    [SerializeField, TextArea] private string Config = "{ \"traverseModeSurfaces\":\"balanced\", \"traverseModeGeodata\":\"none\", \"lodBlending\":0 }";
    [SerializeField, TextArea(0, 20)] private string Statistics = "This will show statistics at play";
#pragma warning restore

    public Material backgroundMaterial;
    public UnityEngine.Mesh backgroundMesh;
    private CommandBuffer backgroundCmds;

    public Texture2DArray blueNoiseTexture;

    private int shaderPropertyMainTex;
    private int shaderPropertyMaskTex;
    private int shaderPropertyUvMat;
    private int shaderPropertyUvClip;
    private int shaderPropertyColor;
    private int shaderPropertyBlendingCoverage;
    private int shaderPropertyFlags;
    private int shaderPropertyFrameIndex;
    private int shaderPropertyTexBlueNoise;

    private int shaderPropertyAtmViewInv;
    private int shaderPropertyAtmColorHorizon;
    private int shaderPropertyAtmColorZenith;
    private int shaderPropertyAtmSizes;
    private int shaderPropertyAtmCoefficients;
    private int shaderPropertyAtmCameraPosition;
    private int shaderPropertyAtmCorners;
    private int shaderPropertyAtmTexDensity;

    private Vector4 shaderValueAtmSizes;
    private Vector4 shaderValueAtmCoefficients;
    private Vector3 shaderValueAtmCameraPosition;
    private Matrix4x4 shaderValueAtmViewInv;
    private Vector4 shaderValueAtmColorHorizon;
    private Vector4 shaderValueAtmColorZenith;
    private bool shaderValueAtmEnabled;
    private int shaderValueFrameIndex;

    protected readonly Draws draws = new Draws();

    protected Map vmap;
    protected vts.Camera vcam;
    protected UnityEngine.Camera ucam;
    protected Transform camTrans;
    protected Transform mapTrans;
    protected MaterialPropertyBlock propertyBlock;

    public vts.Camera Camera { get { return vcam; } }
}

