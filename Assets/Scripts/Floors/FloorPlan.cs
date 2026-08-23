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
    public float floorWidth = 20f;

    [Header("Building Spacing")]
    public float minDistanceBetweenBuildings = 10f;

    [Header("Centralized Themes")]
    [Tooltip("Lista de temas visuales permitidos para este tipo de edificio. Si se define aquí, todos los edificios (Single y DoubleDecker) que usen esta config lo compartirán.")]
    public BuildingThemeSet[] allowedThemes;

    [Header("Rooftop Obstacles & Power-Ups")]
    public GameObject[] roofObstaclePrefabs;
    public GameObject[] powerUps;
    public int obstacleRate = 50;
    public int powerUpRate = 20;

    [Header("Color Shaders")]
    public Material[] colorMaterials;
}
