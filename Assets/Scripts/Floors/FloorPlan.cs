using UnityEngine;

[CreateAssetMenu(fileName = "FloorPlan", menuName = "Scriptable Objects/FloorPlan")]
public class FloorPlan : ScriptableObject
{
    //[Header("Structure Set")]
    //public int floorSet = 0;

    [Header("Building Size")]
    public int minFloors = 2;
    public int maxFloors = 2;
    public float floorHeight = 3f;

    [Header("Building Spacing")]
    public float minDistanceBetweenBuildings = 10f;

}
