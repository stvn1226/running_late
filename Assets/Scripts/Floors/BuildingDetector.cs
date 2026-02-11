using UnityEngine;

public class BuildingDetector : MonoBehaviour
{
    public float buildingZ;
    public string buildingName;
    
    public void Initialize(float z, string name)
    {
        buildingZ = z;
        buildingName = name;
    }
}
