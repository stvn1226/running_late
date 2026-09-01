using UnityEngine;

[CreateAssetMenu(fileName = "ModuleObstacleConfig", menuName = "Runner/Module Obstacle Config")]
public class ModuleObstacleConfig : ScriptableObject
{
    [Header("🏢 Building Sets (Fuente de Verdad)")]
    [Tooltip("Prefabs de edificios tipo Slim")]
    public StructureBuilder[] slimBuildingPrefabs;

    [Tooltip("Prefabs de edificios tipo Medium")]
    public StructureBuilder[] mediumBuildingPrefabs;

    [Tooltip("Prefabs de edificios tipo Wide")]
    public StructureBuilder[] wideBuildingPrefabs;

    [Header("🚗 Terrestrial Obstacles (Carretera & Suelo)")]
    [Tooltip("Obstáculos terrestres: Carros, Camiones, Motos, Bicis, Perros")]
    public GameObject[] groundObstacles;

    [Header("✈️ Aerial Obstacles")]
    [Tooltip("Obstáculos aéreos: Aviones, etc.")]
    public GameObject[] flyingObstacles;
}
