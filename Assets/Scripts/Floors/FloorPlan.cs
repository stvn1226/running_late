using UnityEngine;

[CreateAssetMenu(fileName = "FloorPlan", menuName = "Scriptable Objects/FloorPlan")]
public class FloorPlan : ScriptableObject
{
    //[Header("Structure Set")]
    //public int floorSet = 0;

    [Header("Building Size")]
    public int minFloors = 2;
    public int maxFloors = 2;
    public float floorWidth = 20f;

    [Header("Floor Heights")]
    [Tooltip("Si está activo, usa las alturas individuales (groundHeight, midFloorHeight, roofHeight). Si está inactivo, usa floorHeight para todo.")]
    public bool useCustomFloorHeights = false;
    public float groundHeight = 3.38f;
    public float midFloorHeight = 3.0f;
    public float roofHeight = 3.0f;

    [Tooltip("Altura uniforme por piso (usada si useCustomFloorHeights es false)")]
    public float floorHeight = 3f;

    [Header("Building Spacing")]
    public float minDistanceBetweenBuildings = 10f;

    public float GetGroundHeight() => useCustomFloorHeights && groundHeight > 0f ? groundHeight : floorHeight;
    public float GetMidFloorHeight() => useCustomFloorHeights && midFloorHeight > 0f ? midFloorHeight : floorHeight;
    public float GetRoofHeight() => useCustomFloorHeights && roofHeight > 0f ? roofHeight : floorHeight;

}
