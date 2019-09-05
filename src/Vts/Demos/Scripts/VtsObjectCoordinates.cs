using UnityEngine;

public class VtsObjectCoordinates : MonoBehaviour
{
    public UnityEngine.UI.Text coordsUnity;
    public UnityEngine.UI.Text coordsVts;

    private VtsMap map;

    void Start()
    {
        map = GetComponent<VtsCameraBase>().mapObject.GetComponent<VtsMap>();
    }

    void Update()
    {
        coordsUnity.text = "";
        coordsVts.text = "";
        if (!map.GetVtsMap().GetMapconfigAvailable())
            return;
        double[] p = VtsUtil.U2V3(transform.position);
        p = map.UnityToVtsNavigation(p);
        coordsUnity.text = "Unity World: " + transform.position.ToString("F5");
        coordsVts.text = "Vts Navigation: " + VtsUtil.V2U3(p).ToString("F5");
    }
}
