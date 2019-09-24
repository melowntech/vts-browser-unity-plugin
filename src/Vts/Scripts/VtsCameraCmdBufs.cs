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

public class VtsCameraCmdBufs : VtsCameraBase
{
    protected override void Start()
    {
        base.Start();

        opaque = new CommandBuffer();
        opaque.name = "Vts Opaque";
        transparent = new CommandBuffer();
        transparent.name = "Vts Transparent";

        ucam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, opaque);
        ucam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, transparent);
    }

    private void RegenerateCommandBuffer(CommandBuffer buffer, List<DrawSurfaceTask> tasks, Material renderMaterial)
    {
        buffer.Clear();
        buffer.SetViewMatrix(Matrix4x4.identity);
        buffer.SetProjectionMatrix(ucam.projectionMatrix);
        foreach (DrawSurfaceTask t in tasks)
        {
            if (t.mesh == null)
                continue;
            InitMaterial(propertyBlock, t);
            buffer.DrawMesh((t.mesh as VtsMesh).Get(), VtsUtil.V2U44(t.data.mv), renderMaterial, 0, -1, propertyBlock);
        }
    }

    protected override void CameraDraw()
    {
        RegenerateCommandBuffer(opaque, draws.opaque, opaqueMaterial);
        RegenerateCommandBuffer(transparent, draws.transparent, transparentMaterial);
    }

    public Material opaqueMaterial;
    public Material transparentMaterial;

    private CommandBuffer opaque;
    private CommandBuffer transparent;
}

