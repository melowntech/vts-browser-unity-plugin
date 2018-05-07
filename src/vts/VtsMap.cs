using UnityEngine;
using vts;

public class VtsMap : MonoBehaviour
{
    VtsMap()
    {
        VtsLog.Dummy();
    }

    void OnEnable()
    {
        Debug.Assert(map == null);
        map = new Map("");
        map.DataInitialize();
        map.RenderInitialize();
        map.SetMapConfigPath("https://cdn.melown.com/mario/store/melown2015/map-config/melown/Melown-Earth-Intergeo-2017/mapConfig.json");

        map.EventLoadTexture += LoadTexture;
        map.EventLoadMesh += LoadMesh;
    }

    void Update()
    {
        Util.Log(LogLevel.info2, "Unity update frame index: " + frameIndex++);
        Debug.Assert(map != null);

        double[] pan = new double[3];
        pan[0] = 1;
        map.Pan(pan);

        map.DataTick();
        map.RenderTickPrepare(Time.deltaTime);
    }

    System.Object LoadTexture(vts.Texture t)
    {
        Debug.Assert(map != null);
        // todo
        return new System.Object();
    }

    System.Object LoadMesh(vts.Mesh m)
    {
        Debug.Assert(map != null);
        // todo
        return new System.Object();
    }

    void OnDisable()
    {
        Debug.Assert(map != null);
        map.DataDeinitialize();
        map.RenderDeinitialize();
        map = null;
    }
    
    private uint frameIndex;
    private Map map;

    public Map Handle { get { return map; } }
}

