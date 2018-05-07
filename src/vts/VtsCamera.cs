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
            buffer.DrawMesh(debugMesh, m, debugMaterial);
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

    public UnityEngine.Mesh debugMesh;
    public Material debugMaterial;

    protected Draws draws;
    protected Camera cam;

    protected CommandBuffer opaque;
    protected CommandBuffer transparent;
    protected CommandBuffer geodata;
    protected CommandBuffer infographics;
}

