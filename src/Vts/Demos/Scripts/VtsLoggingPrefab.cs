using UnityEngine;

public class VtsLoggingPrefab : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Mesh: " + GetComponent<MeshFilter>().mesh.name);
    }
}
