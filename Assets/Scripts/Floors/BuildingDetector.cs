using UnityEngine;

public class BuildingDetector : MonoBehaviour
{
    public float buildingZ;
    public string buildingName;
    public int sortingOrder;
    
    public void Initialize(float z, string name, int order)
    {
        buildingZ = z;
        buildingName = name;
        sortingOrder = order;
    }
}
