using UnityEngine;

public class VtsCursorCoordinates : MonoBehaviour
{
    public UnityEngine.UI.Text coordsUnity;
    public UnityEngine.UI.Text coordsVts;

    private Camera cam;
    private VtsMap map;

    void Start()
    {
        cam = GetComponent<Camera>();
        map = GetComponent<VtsCameraBase>().mapObject.GetComponent<VtsMap>();
    }

    void Update()
    {
        coordsUnity.text = "";
        coordsVts.text = "";
        if (!map.GetVtsMap().GetMapconfigAvailable())
            return;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit))
            return;
        double[] p = VtsUtil.U2V3(hit.point);
        p = map.UnityToVtsNavigation(p);
        coordsUnity.text = "Unity World: " + hit.point.ToString("F5");
        coordsVts.text = "Vts Navigation: " + VtsUtil.V2U3(p).ToString("F5");
    }
}
