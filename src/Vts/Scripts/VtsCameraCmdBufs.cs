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
        //geodata = new CommandBuffer();
        //geodata.name = "Vts Geodata";
        //infographics = new CommandBuffer();
        //infographics.name = "Vts Infographics";

        ucam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, opaque);
        ucam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, transparent);
        //ucam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, geodata);
        //ucam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, infographics);
    }

    private void RegenerateCommandBuffer(CommandBuffer buffer, List<DrawTask> tasks, Material renderMaterial)
    {
        buffer.Clear();
        if (atmosphere && draws.celestial.atmosphere.densityTexture as VtsTexture != null)
            buffer.EnableShaderKeyword("VTS_ATMOSPHERE");
        else
            buffer.DisableShaderKeyword("VTS_ATMOSPHERE");
        buffer.SetViewMatrix(Matrix4x4.identity);
        buffer.SetProjectionMatrix(ucam.projectionMatrix);
        foreach (DrawTask t in tasks)
        {
            if (t.mesh == null)
                continue;
            MaterialPropertyBlock mat = new MaterialPropertyBlock();
            VtsTexture atmTex = draws.celestial.atmosphere.densityTexture as VtsTexture;
            if (atmosphere && atmTex != null)
            {
                mat.SetVector(shaderPropertyAtmSizes, shaderValueAtmSizes);
                mat.SetVector(shaderPropertyAtmCoefficients, shaderValueAtmCoefficients);
                mat.SetVector(shaderPropertyAtmCameraPosition, shaderValueAtmCameraPosition);
                mat.SetMatrix(shaderPropertyAtmViewInv, shaderValueAtmViewInv);
                mat.SetVector(shaderPropertyAtmColorHorizon, shaderValueAtmColorHorizon);
                mat.SetVector(shaderPropertyAtmColorZenith, shaderValueAtmColorZenith);
                mat.SetTexture("vtsTexAtmDensity", atmTex.Get());
            }
            bool monochromatic = false;
            if (t.texColor != null)
            {
                var tt = t.texColor as VtsTexture;
                mat.SetTexture(shaderPropertyMainTex, tt.Get());
                monochromatic = tt.monochromatic;
            }
            if (t.texMask != null)
                mat.SetTexture(shaderPropertyMaskTex, (t.texMask as VtsTexture).Get());
            mat.SetMatrix(shaderPropertyUvMat, VtsUtil.V2U33(t.data.uvm));
            mat.SetVector(shaderPropertyUvClip, VtsUtil.V2U4(t.data.uvClip));
            mat.SetVector(shaderPropertyColor, VtsUtil.V2U4(t.data.color));
            // flags: mask, monochromatic, flat shading, uv source
            mat.SetVector(shaderPropertyFlags, new Vector4(t.texMask == null ? 0 : 1, monochromatic ? 1 : 0, 0, t.data.externalUv ? 1 : 0));
            buffer.DrawMesh((t.mesh as VtsMesh).Get(), VtsUtil.V2U44(t.data.mv), renderMaterial, 0, -1, mat);
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
    //private CommandBuffer geodata;
    //private CommandBuffer infographics;
}

