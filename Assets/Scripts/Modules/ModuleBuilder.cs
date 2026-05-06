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
    public GameObject[] flyingObstaclePrefabs;
    public int obstacleRate = 40;
    public int flyingObstacleRate = 20;

    [Header("Generation Settings")]
    public bool autoGenerateOnStart = false;
    [Range(0, 100)]
    public int buildingSpawnChance = 90;

    [Header("Dual Level Config")]
    public bool isElevatedSidewalk = true;
    public bool omitFoundationAtBaseLevel = true;
    public bool pushPaddingToBackground = true;
    public bool disablePaddingColliders = true;
    public float paddingZOffset = 5f;
    [Tooltip("Z offset for obstacles relative to the module root. Use negative values to move them closer to camera.")]
    public float obstacleZOffset = -1.5f;
    public int baseLevelHeight = 1;

    private bool hasGenerated = false;
    private int actualTerrainHeight = -1;

    private void Start()
    {
        if (autoGenerateOnStart && !hasGenerated)
        {
            GameLogger.Log("MODULE", $"Auto-generating module '{gameObject.name}' at Start");
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
            GameLogger.LogWarning("MODULE", $"Module '{gameObject.name}' already generated, skipping");
            return;
        }

        if (moduleConfig == null)
        {
            GameLogger.LogError("MODULE", $"ModuleBuilder '{gameObject.name}': No module config assigned!");
            return;
        }

        GameLogger.Log("MODULE", $"=== GenerateModule '{gameObject.name}' ===");
        GameLogger.Log("MODULE", $"  - Position: {transform.position}");
        GameLogger.Log("MODULE", $"  - Explicit Height: {(explicitHeight.HasValue ? explicitHeight.Value.ToString() : "None")}");

        if (placementTracker != null)
        {
            placementTracker.position = transform.position;
            GameLogger.Log("MODULE", $"  - Placement tracker set to {placementTracker.position}");
        }

        GenerateBaseTerrain(explicitHeight);
        GenerateBuildings();
        SpawnFlyingObstacles(explicitHeight);

        hasGenerated = true;
        GameLogger.Log("MODULE", $"✓ Module '{gameObject.name}' generated with height={actualTerrainHeight}");
    }
    private void GenerateBaseTerrain(int? explicitHeight = null)
    {
        if (placementTracker == null)
        {
            GameLogger.LogError("MODULE", $"ModuleBuilder '{gameObject.name}': No placement tracker assigned!");
            return;
        }

        int groundBlocksCount;
        if (explicitHeight.HasValue)
        {
            // SAFETY: System requires at least 1 block to provide a walkable floor
            if (explicitHeight.Value < 0)
            {
                GameLogger.LogWarning("MODULE", $"ModuleBuilder '{gameObject.name}': Requested height {explicitHeight.Value} is invalid (min is 0). Clamping to 0.");
            }

            int minHeight = Mathf.Max(0, moduleConfig.minTerrainHeight);
            groundBlocksCount = Mathf.Clamp(explicitHeight.Value, minHeight, moduleConfig.maxTerrainHeight);
            
            if (groundBlocksCount == 0)
            {
                actualTerrainHeight = 0;
                GameLogger.Log("MODULE", "  - Height is 0, skipping terrain generation (Road Level).");
                return;
            }
        }
        else
        {
            int minHeight = Mathf.Max(1, moduleConfig.minTerrainHeight);
            groundBlocksCount = Random.Range(minHeight, moduleConfig.maxTerrainHeight + 1);
            GameLogger.Log("MODULE", $"  - Using random height: {groundBlocksCount}");
        }

        actualTerrainHeight = groundBlocksCount;

        Vector3 startPos = placementTracker.position;

        // Ground blocks except the top one (foundation)
        for (int i = 0; i < groundBlocksCount - 1; i++)
        {
            if (padding != null)
            {
                GameObject paddingBlock = Instantiate(padding, placementTracker.position, Quaternion.identity, transform);
                paddingBlock.name = $"Padding_{i}";
                
                if (isElevatedSidewalk)
                {
                    if (pushPaddingToBackground)
                    {
                        paddingBlock.transform.position += new Vector3(0, 0, paddingZOffset);
                        
                        // Force SpriteRenderer sorting layer if needed
                        SpriteRenderer[] renderers = paddingBlock.GetComponentsInChildren<SpriteRenderer>();
                        foreach(var sr in renderers)
                        {
                            sr.sortingOrder = -50; // Push far back
                        }
                    }
                    if (disablePaddingColliders)
                    {
                        Collider2D[] cols = paddingBlock.GetComponentsInChildren<Collider2D>();
                        foreach(var c in cols) c.enabled = false;
                    }
                }
            }

            placementTracker.position += Vector3.up * moduleConfig.groundBlockHeight;
        }

        // The top-most block that the player walks on
        if (foundation != null)
        {
            bool shouldOmit = isElevatedSidewalk && omitFoundationAtBaseLevel && groundBlocksCount <= baseLevelHeight;
            if (!shouldOmit)
            {
                GameObject groundBlock = Instantiate(foundation, placementTracker.position, Quaternion.identity, transform);
                groundBlock.name = "Foundation";

                if (isElevatedSidewalk)
                {
                    Collider2D col = groundBlock.GetComponent<Collider2D>();
                    if (col == null) col = groundBlock.GetComponentInChildren<Collider2D>();
                    if (col != null)
                    {
                        col.usedByEffector = true;
                        int platformLayer = LayerMask.NameToLayer("OneWayPlatform");
                        if (platformLayer != -1) groundBlock.layer = platformLayer;
                    }
                }
            }
            placementTracker.position += Vector3.up * moduleConfig.groundBlockHeight;
        }

        if (obstaclePrefabs != null && obstaclePrefabs.Length > 0 && obstacleSpawnPoint != null)
        {
            obstacleSpawnPoint.position = new Vector3(
                obstacleSpawnPoint.position.x,
                placementTracker.position.y,
                obstacleSpawnPoint.position.z
            );

            int obstacleRoll = Random.Range(0, 100);
            if (obstacleRoll < obstacleRate)
            {
                GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                GameObject newObstacle = Instantiate(obstaclePrefab, obstacleSpawnPoint.position, Quaternion.identity, transform);
                
                // FINAL FIX: Explicitly set local Z to ensure enemies are in front of buildings
                Vector3 locPos = newObstacle.transform.localPosition;
                newObstacle.transform.localPosition = new Vector3(locPos.x, locPos.y, obstacleZOffset);
                
                newObstacle.name = $"{obstaclePrefab.name}_Generated";
                GameLogger.Log("MODULE", $"  - Spawned obstacle: {newObstacle.name} at {newObstacle.transform.position} (Local Z: {obstacleZOffset})");
            }
        }

        GameLogger.Log("MODULE", $"  - Terrain: {groundBlocksCount} blocks, from y={startPos.y:F2} to y={placementTracker.position.y:F2}");
    }

    private void GenerateBuildings()
    {
        if (buildingSpawnPoints == null || buildingSpawnPoints.Length == 0)
        {
            GameLogger.Log("MODULE", $"  - No building spawn points");
            return;
        }

        if (buildingPrefabs == null || buildingPrefabs.Length == 0)
        {
            GameLogger.Log("MODULE", $"  - No building prefabs");
            return;
        }

        int buildingsSpawned = 0;

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

            buildingsSpawned++;
        }

        GameLogger.Log("MODULE", $"  - Buildings: {buildingsSpawned}/{buildingSpawnPoints.Length} spawn points used");
    }

    private void SpawnFlyingObstacles(int? explicitHeight = 0)
    {
        if (flyingObstaclePrefabs == null || flyingObstaclePrefabs.Length == 0) return;

        float flyingObstacleRoll = Random.Range(0, 100);

        if (flyingObstacleRoll < flyingObstacleRate)
        {
            // Usar groundBlockHeight del config en vez de hardcodear *3,
            // así si el diseñador ajusta la escala de bloques el obstáculo sigue al terreno.
            float blockHeight = moduleConfig != null ? moduleConfig.groundBlockHeight : 3f;

            GameObject flyingObstaclePrefab = flyingObstaclePrefabs[Random.Range(0, flyingObstaclePrefabs.Length)];
            
            // Apply obstacleZOffset to flying obstacles too
            Vector3 flyingPosition = new Vector3(transform.position.x, ((float)explicitHeight * blockHeight) + 50, transform.position.z + obstacleZOffset);
            
            GameObject newFlyingObstacle = Instantiate(flyingObstaclePrefab, flyingPosition, Quaternion.identity, transform);
            newFlyingObstacle.name = $"{flyingObstaclePrefab.name}_Generated";
            GameLogger.Log("MODULE", $"  - Spawned obstacle: {newFlyingObstacle.name} at {newFlyingObstacle.transform.position} (Local Z: {obstacleZOffset})");
        }
    }

    public void ClearGeneratedContent()
    {
        GameLogger.Log("MODULE", $"Clearing generated content for '{gameObject.name}'");

        int cleared = 0;

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

                cleared++;
            }
        }

        hasGenerated = false;
        actualTerrainHeight = -1;

        GameLogger.Log("MODULE", $"✓ Cleared {cleared} objects from '{gameObject.name}'");
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
