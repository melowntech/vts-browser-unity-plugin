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

public class VtsCamera : MonoBehaviour
{
    public VtsCamera()
    {
        CamOverrideEyeDel = CamOverrideEye;
        CamOverrideTargetDel = CamOverrideTarget;
        CamOverrideUpDel = CamOverrideUp;
    }

    protected virtual void Start()
    {
        cam = GetComponent<Camera>();
        SetupCommandBuffers();
    }

    protected virtual void SetupCommandBuffers()
    {
        opaque = new CommandBuffer();
        transparent = new CommandBuffer();
        geodata = new CommandBuffer();
        infographics = new CommandBuffer();

        cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, opaque);
        //cam.AddCommandBuffer(CameraEvent.AfterGBuffer, opaque);
        cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, transparent);
        cam.AddCommandBuffer(CameraEvent.AfterImageEffects, geodata);
        cam.AddCommandBuffer(CameraEvent.AfterEverything, infographics);
    }

    private Map.CameraOverrideHandler CamOverrideEyeDel;
    private void CamOverrideEye(ref double[] values)
    {
        values = VtsUtil.U2V3(transform.position);
    }

    private Map.CameraOverrideHandler CamOverrideTargetDel;
    private void CamOverrideTarget(ref double[] values)
    {
        values = VtsUtil.U2V3(transform.position + transform.forward);
    }

    private Map.CameraOverrideHandler CamOverrideUpDel;
    private void CamOverrideUp(ref double[] values)
    {
        values = VtsUtil.U2V3(transform.up);
    }

    protected virtual void Update()
    {
        VtsMap vtsMap = mapObject.GetComponent<VtsMap>();
        Map map = vtsMap.Handle;
        map.SetWindowSize((uint)cam.scaledPixelWidth, (uint)cam.scaledPixelHeight);
        //map.EventCameraEye += CamOverrideEyeDel;
        //map.EventCameraTarget += CamOverrideTargetDel;
        //map.EventCameraUp += CamOverrideUpDel;
        map.RenderTickRender();
        //map.EventCameraEye -= CamOverrideEyeDel;
        //map.EventCameraTarget -= CamOverrideTargetDel;
        //map.EventCameraUp -= CamOverrideUpDel;
        draws = map.Draws();
        RegenerateCommandBuffers();
    }

    protected virtual void RegenerateCommandBuffer(CommandBuffer buffer, List<DrawTask> tasks)
    {
        //UnityEngine.Mesh msh = CreateCube();
        //Material mat = CreateMaterial();
        buffer.Clear();

        Matrix4x4 proj = VtsUtil.V2U44(draws.camera.proj);
        //proj = Matrix4x4.Scale(new Vector3(1, 1, -1)) * proj;

        //buffer.SetViewMatrix(Matrix4x4.Scale(new Vector3(0.5f, 0.5f, 0.5f)));
        buffer.SetViewMatrix(Matrix4x4.identity);
        buffer.SetProjectionMatrix(Matrix4x4.identity);
        foreach (DrawTask t in tasks)
        {
            Matrix4x4 m = VtsUtil.V2U44(t.data.mv);
            //m = cam.projectionMatrix * m;
            //m = m.transpose;
            m = proj * m;
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

    private UnityEngine.Mesh CreateCube()
    {
        Vector3[] vertices =
        {
            new Vector3 (-1, -1, -1),
            new Vector3 (1, -1, -1),
            new Vector3 (1, 1, -1),
            new Vector3 (-1, 1, -1),
            new Vector3 (-1, 1, 1),
            new Vector3 (1, 1, 1),
            new Vector3 (1, -1, 1),
            new Vector3 (-1, -1, 1),
        };

        int[] triangles =
        {
            0, 2, 1, //face front
            0, 3, 2,
            2, 3, 4, //face top
            2, 4, 5,
            1, 2, 5, //face right
            1, 5, 6,
            0, 7, 4, //face left
            0, 4, 3,
            5, 4, 7, //face back
            5, 7, 6,
            0, 6, 7, //face bottom
            0, 1, 6
        };

        UnityEngine.Mesh mesh = new UnityEngine.Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private Material CreateMaterial()
    {
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Material diffuse = primitive.GetComponent<MeshRenderer>().sharedMaterial;
        DestroyImmediate(primitive);
        return diffuse;
    }

    public GameObject mapObject;

    public Material debugMaterial;

    protected Draws draws;
    protected Camera cam;

    protected CommandBuffer opaque;
    protected CommandBuffer transparent;
    protected CommandBuffer geodata;
    protected CommandBuffer infographics;
}

