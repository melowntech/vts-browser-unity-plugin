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
        draws = new Draws();
    }

    protected virtual void Start()
    {
        cam = GetComponent<Camera>();
        trans = GetComponent<Transform>();
        mapTrans = mapObject.GetComponent<Transform>();

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

        SetupCommandBuffers();
    }

    protected virtual void SetupCommandBuffers()
    {
        opaque = new CommandBuffer();
        opaque.name = "Vts Opaque";
        transparent = new CommandBuffer();
        transparent.name = "Vts Transparent";
        geodata = new CommandBuffer();
        geodata.name = "Vts Geodata";
        infographics = new CommandBuffer();
        infographics.name = "Vts Infographics";
        background = new CommandBuffer();
        background.name = "Skybox";

        cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, opaque);
        cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, transparent);
        cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, geodata);
        cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, infographics);
        cam.AddCommandBuffer(CameraEvent.AfterImageEffectsOpaque, background);
    }

    private readonly Map.CameraOverrideHandler CamOverrideViewDel;
    private void CamOverrideView(ref double[] values)
    {
        Matrix4x4 Mu = mapTrans.localToWorldMatrix * VtsUtil.UnityToVtsMatrix;
        // view matrix
        if (controlTransformation == VtsDataControl.Vts)
        {
            // todo it would be nice to actually decompose the matrix into the camera transformation
            // it would make it usable in other components or objects
            cam.worldToCameraMatrix = VtsUtil.V2U44(Math.Mul44x44(values, Math.Inverse44(VtsUtil.U2V44(Mu))));
        }
        else
            values = Math.Mul44x44(VtsUtil.U2V44(cam.worldToCameraMatrix), VtsUtil.U2V44(Mu));
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

    protected virtual void Update()
    {
        Map map = mapObject.GetComponent<VtsMap>().map;
        map.SetWindowSize((uint)cam.pixelWidth, (uint)cam.pixelHeight);
        map.EventCameraView += CamOverrideViewDel;
        map.EventCameraFovAspectNearFar += CamOverrideParametersDel;
        map.RenderTickRender();
        map.EventCameraView -= CamOverrideViewDel;
        map.EventCameraFovAspectNearFar -= CamOverrideParametersDel;
        draws.Load(map);
        RegenerateCommandBuffers();
    }

    protected virtual MaterialPropertyBlock CreatePropertyBlock()
    {
        MaterialPropertyBlock mat = new MaterialPropertyBlock();
        if (vtsAtmosphere)
        {
            var cel = draws.celestial;
            var atm = cel.atmosphere;
            mat.SetVector(shaderPropertyAtmParams, new Vector4((float)(atm.thickness / cel.majorRadius), (float)atm.horizontalExponent, (float)(cel.minorRadius / cel.majorRadius), (float)cel.majorRadius));
            mat.SetVector(shaderPropertyAtmCameraPosition, VtsUtil.V2U3(draws.camera.eye) / (float)cel.majorRadius);
            mat.SetMatrix(shaderPropertyAtmViewInv, VtsUtil.V2U44(Math.Inverse44(draws.camera.view)));
            mat.SetVector(shaderPropertyAtmColorLow, VtsUtil.V2U4(atm.colorLow));
            mat.SetVector(shaderPropertyAtmColorHigh, VtsUtil.V2U4(atm.colorHigh));
        }
        return mat;
    }

    static readonly Matrix4x4 InvertDepthMatrix = Matrix4x4.Scale(new Vector3(1, 1, -1));

    protected virtual void RegenerateCommandBuffer(CommandBuffer buffer, List<DrawTask> tasks)
    {
        buffer.Clear();
        buffer.SetProjectionMatrix(InvertDepthMatrix * GL.GetGPUProjectionMatrix(cam.projectionMatrix, false));
        foreach (DrawTask t in tasks)
        {
            if (t.mesh == null)
                continue;
            MaterialPropertyBlock mat = CreatePropertyBlock();
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
            buffer.DrawMesh((t.mesh as VtsMesh).Get(), VtsUtil.V2U44(t.data.mv), mapMaterial, 0, -1, mat);
        }
    }

    protected virtual void RegenerateBackground()
    {
        background.Clear();
        if (vtsAtmosphere)
        {
            MaterialPropertyBlock mat = CreatePropertyBlock();
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
                mat.SetVectorArray(shaderPropertyAtmCorners, c);
            }
            background.DrawMesh(backgroundMesh, Matrix4x4.identity, backgroundMaterial, 0, -1, mat);
        }
    }

    protected virtual void RegenerateCommandBuffers()
    {
        RegenerateCommandBuffer(opaque, draws.opaque);
        RegenerateCommandBuffer(transparent, draws.transparent);
        RegenerateCommandBuffer(geodata, draws.geodata);
        RegenerateCommandBuffer(infographics, draws.infographics);
        RegenerateBackground();
    }

    public GameObject mapObject;

    public VtsDataControl controlTransformation;
    public VtsDataControl controlNearFar;
    public VtsDataControl controlFov;

    public Material mapMaterial;
    public Material backgroundMaterial;
    public UnityEngine.Mesh backgroundMesh;

    public bool vtsAtmosphere;

    protected int shaderPropertyMainTex;
    protected int shaderPropertyMaskTex;
    protected int shaderPropertyUvMat;
    protected int shaderPropertyUvClip;
    protected int shaderPropertyColor;
    protected int shaderPropertyFlags;

    protected int shaderPropertyAtmViewInv;
    protected int shaderPropertyAtmColorLow;
    protected int shaderPropertyAtmColorHigh;
    protected int shaderPropertyAtmParams;
    protected int shaderPropertyAtmCameraPosition;
    protected int shaderPropertyAtmCorners;

    protected readonly Draws draws;
    protected Camera cam;
    protected Transform trans;
    protected Transform mapTrans;

    protected CommandBuffer opaque;
    protected CommandBuffer transparent;
    protected CommandBuffer geodata;
    protected CommandBuffer infographics;
    protected CommandBuffer background;
}

