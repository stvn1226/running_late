using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component attached to buildings to identify their visual depth (Z) and sorting order.
/// Maintains a static registry of all active building detectors to eliminate expensive FindObjectsByType queries.
/// </summary>
public class BuildingDetector : MonoBehaviour
{
    public static readonly List<BuildingDetector> All = new List<BuildingDetector>();

    public float buildingZ;
    public string buildingName;
    public int sortingOrder;

    private void OnEnable()
    {
        if (!All.Contains(this))
        {
            All.Add(this);
        }
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public void Initialize(float z, string name, int order)
    {
        buildingZ = z;
        buildingName = name;
        sortingOrder = order;
    }
}
