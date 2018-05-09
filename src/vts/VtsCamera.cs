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

    protected virtual void Start()
    {
        cam = GetComponent<Camera>();
        trans = GetComponent<Transform>();
        SetupCommandBuffers();
    }

    protected virtual void SetupCommandBuffers()
    {
        opaque = new CommandBuffer();
        transparent = new CommandBuffer();
        geodata = new CommandBuffer();
        infographics = new CommandBuffer();

        cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, opaque);
        cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, transparent);
        cam.AddCommandBuffer(CameraEvent.AfterImageEffects, geodata);
        cam.AddCommandBuffer(CameraEvent.AfterEverything, infographics);
    }

    private Map.CameraOverrideHandler CamOverrideViewDel;
    private void CamOverrideView(ref double[] values)
    {
        // view matrix
        if (controlTransformation == VtsDataControl.Vts)
        {
            Matrix4x4 m = VtsUtil.V2U44(values).inverse;
            trans.localRotation = m.rotation;
            trans.localPosition = m.MultiplyPoint(new Vector3(0, 0, 0));
        }
        else
        {
            Matrix4x4 m = Matrix4x4.TRS(trans.localPosition, trans.localRotation, trans.localScale).inverse;
            values = VtsUtil.U2V44(m);
        }
    }

    private Map.CameraOverrideParamsHandler CamOverrideParametersDel;
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
        VtsMap vtsMap = mapObject.GetComponent<VtsMap>();
        Map map = vtsMap.Handle;
        map.SetWindowSize((uint)cam.scaledPixelWidth, (uint)cam.scaledPixelHeight);
        map.EventCameraView += CamOverrideViewDel;
        map.EventCameraFovAspectNearFar += CamOverrideParametersDel;
        map.RenderTickRender();
        map.EventCameraView -= CamOverrideViewDel;
        map.EventCameraFovAspectNearFar -= CamOverrideParametersDel;
        draws = map.Draws();
        RegenerateCommandBuffers();
    }

    protected virtual void RegenerateCommandBuffer(CommandBuffer buffer, List<DrawTask> tasks)
    {
        buffer.Clear();
        buffer.SetProjectionMatrix(cam.projectionMatrix);
        foreach (DrawTask t in tasks)
        {
            Matrix4x4 m = VtsUtil.V2U44(t.data.mv);
            buffer.DrawMesh(t.mesh as UnityEngine.Mesh, m, debugMaterial);
        }
    }

    protected virtual void RegenerateCommandBuffers()
    {
        RegenerateCommandBuffer(opaque, draws.opaque);
        RegenerateCommandBuffer(transparent, draws.transparent);
        RegenerateCommandBuffer(geodata, draws.geodata);
        RegenerateCommandBuffer(infographics, draws.infographics);
    }

    public GameObject mapObject;

    public VtsDataControl controlTransformation;
    public VtsDataControl controlNearFar;
    public VtsDataControl controlFov;

    public Material debugMaterial;

    protected Draws draws;
    protected Camera cam;
    protected Transform trans;

    protected CommandBuffer opaque;
    protected CommandBuffer transparent;
    protected CommandBuffer geodata;
    protected CommandBuffer infographics;
}

