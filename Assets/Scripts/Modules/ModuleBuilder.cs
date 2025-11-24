using UnityEngine;

public class ModuleBuilder : MonoBehaviour
{
    [Header("Config")]
    public ModulePlan moduleConfig;

    [Header("Building Materials")]
    public GameObject foundation;
    public GameObject padding;
    public StructureBuilder[] buildingPrefabs;

    [Header("Spawn Points")]
    public Transform placementTracker;
    public Transform[] buildingSpawnPoints;
    public Transform obstacleSpawnPoint;

    [Header("Obstacles")]
    public GameObject[] obstaclePrefabs;

    [Header("Generation Settings")]
    public bool autoGenerateOnStart = false;
    [Range(0, 100)]
    public int buildingSpawnChance = 90;
    
    private bool hasGenerated = false;
    private int actualTerrainHeight = -1;

    private void Start()
    {
        if (autoGenerateOnStart && !hasGenerated)
        {
            GenerateModule();
        }
        
        if (placementTracker != null)
        {
            placementTracker.position = transform.position;
        }
    }

    public void GenerateModule(int? explicitHeight = null)
    {
        if (hasGenerated)
        {
            return;
        }
        
        if (moduleConfig == null)
        {
            Debug.LogError($"ModuleBuilder {gameObject.name}: No module config assigned!");
            return;
        }
        
        if (placementTracker != null)
        {
            placementTracker.position = transform.position;
        }
        
        GenerateBaseTerrain(explicitHeight);
        GenerateBuildings();
        
        hasGenerated = true;
    }
    
    private void GenerateBaseTerrain(int? explicitHeight = null)
    {
        if (placementTracker == null)
        {
            Debug.LogError($"ModuleBuilder {gameObject.name}: No placement tracker assigned!");
            return;
        }
        
        int groundBlocksCount;
        if (explicitHeight.HasValue)
        {
            groundBlocksCount = Mathf.Clamp(explicitHeight.Value, moduleConfig.minTerrainHeight, moduleConfig.maxTerrainHeight);
        }
        else
        {
            groundBlocksCount = Random.Range(moduleConfig.minTerrainHeight, moduleConfig.maxTerrainHeight + 1);
        }
        
        actualTerrainHeight = groundBlocksCount;
        
        for (int i = 0; i < groundBlocksCount - 1; i++)
        {
            if (padding != null)
            {
                GameObject paddingBlock = Instantiate(padding, placementTracker.position, Quaternion.identity, transform);
                paddingBlock.name = $"Padding_{i}";
            }
            
            placementTracker.position += Vector3.up * moduleConfig.groundBlockHeight;
        }
        
        if (foundation != null)
        {
            GameObject groundBlock = Instantiate(foundation, placementTracker.position, Quaternion.identity, transform);
            groundBlock.name = "Foundation";
            placementTracker.position += Vector3.up * moduleConfig.groundBlockHeight;
        }

        if (obstaclePrefabs != null && obstaclePrefabs.Length > 0 && obstacleSpawnPoint != null)
        {
            obstacleSpawnPoint.position = new Vector3(
                obstacleSpawnPoint.position.x, 
                placementTracker.position.y, 
                obstacleSpawnPoint.position.z
            ); // Position obstacle at ground level
            int obstacleRoll = Random.Range(0, 100);
            if (obstacleRoll < 40) // 40% chance to spawn an obstacle
            {
                GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                GameObject newObstacle = Instantiate(obstaclePrefab, obstacleSpawnPoint.position, Quaternion.identity, transform);
                newObstacle.name = $"{obstaclePrefab.name}_Generated";
            }
        }
    }
    
    private void GenerateBuildings()
    {
        if (buildingSpawnPoints == null || buildingSpawnPoints.Length == 0)
        {
            return;
        }
        
        if (buildingPrefabs == null || buildingPrefabs.Length == 0)
        {
            return;
        }

        foreach (Transform spawnPoint in buildingSpawnPoints)
        {
            if (spawnPoint == null) continue;
            
            int spawnRoll = Random.Range(0, 100);
            if (spawnRoll >= buildingSpawnChance)
            {
                continue;
            }
            
            Vector3 spawnPosition = new Vector3(
                spawnPoint.position.x, 
                placementTracker.position.y, 
                spawnPoint.position.z
            );
            
            StructureBuilder buildingTemplate = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
            
            GameObject newBuilding = Instantiate(buildingTemplate.gameObject, spawnPosition, Quaternion.identity, transform);
            newBuilding.name = $"{buildingTemplate.name}_Generated";
            
            StructureBuilder buildingBuilder = newBuilding.GetComponent<StructureBuilder>();
            if (buildingBuilder != null)
            {
                buildingBuilder.ConstructStructure();
            }
        }
    }
    
    public void ClearGeneratedContent()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            
            if (child == placementTracker) continue;
            
            bool isSpawnPoint = false;
            foreach (Transform spawnPoint in buildingSpawnPoints)
            {
                if (child == spawnPoint)
                {
                    isSpawnPoint = true;
                    break;
                }
            }
            
            if (!isSpawnPoint)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
        
        hasGenerated = false;
    }
    
    public void RegenerateModule()
    {
        ClearGeneratedContent();
        GenerateModule();
    }
    
    public bool HasGenerated => hasGenerated;
    public ModulePlan GetConfig() => moduleConfig;
    public int GetActualTerrainHeight() => actualTerrainHeight;
    
    private void OnValidate()
    {
        if (placementTracker == null)
        {
            placementTracker = transform.Find("PlacementTracker");
        }
    }
}
