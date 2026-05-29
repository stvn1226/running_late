using UnityEngine;
using System.Collections.Generic;

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

    public void GenerateModule(int? explicitHeight = null, bool includeContent = true, bool spawnObstacles = true, int maxObstacleLevel = 3, DifficultyConfig diffConfig = null)
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

        // Verbose per-module logs suppressed — check LEVEL logs for generation summary

        if (placementTracker != null)
        {
            placementTracker.position = transform.position;
        }

        GenerateBaseTerrain(explicitHeight, spawnObstacles, maxObstacleLevel, diffConfig);
        
        if (includeContent)
        {
            GenerateBuildings();
            if (spawnObstacles)
            {
                SpawnFlyingObstacles(explicitHeight, diffConfig);
            }
        }

        hasGenerated = true;
        // Module generated — height visible in LEVEL [step] logs
    }
    private void GenerateBaseTerrain(int? explicitHeight = null, bool spawnObstacles = true, int maxObstacleLevel = 3, DifficultyConfig diffConfig = null)
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

        // [BuildingAudit] — terrain entry snapshot
        int gbhForLog = moduleConfig != null ? moduleConfig.groundBlockHeight : -1;
        bool shouldOmitForLog = isElevatedSidewalk && omitFoundationAtBaseLevel && groundBlocksCount <= baseLevelHeight;
        GameLogger.Log("BuildingAudit",
            $"[TerrainEntry] Segment={gameObject.name} " +
            $"TransformY={transform.position.y:F3} " +
            $"explicitHeight={groundBlocksCount} " +
            $"groundBlockHeight={gbhForLog} " +
            $"omitFoundationAtBaseLevel={omitFoundationAtBaseLevel} " +
            $"baseLevelHeight={baseLevelHeight} " +
            $"isElevatedSidewalk={isElevatedSidewalk} " +
            $"groundBlocksCount={groundBlocksCount} " +
            $"shouldOmit={shouldOmitForLog} " +
            $"placementTrackerY_beforeTerrain={placementTracker.position.y:F3}");

        // Ground blocks except the top one (foundation)
        int paddingBlocksSpawned = 0;
        for (int i = 0; i < groundBlocksCount - 1; i++)
        {
            if (padding != null)
            {
                GameObject paddingBlock = Instantiate(padding, placementTracker.position, Quaternion.identity, transform);
                paddingBlock.name = $"Padding_{i}";
                paddingBlocksSpawned++;
                
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
        bool foundationInstantiated = false;
        if (foundation != null)
        {
            bool shouldOmit = isElevatedSidewalk && omitFoundationAtBaseLevel && groundBlocksCount <= baseLevelHeight;
            if (!shouldOmit)
            {
                foundationInstantiated = true;
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

                // Tracker advances only when foundation was actually placed.
                // For elevated sidewalk with shouldOmit=true, tracker stays at transform.position.y
                // so buildings spawn on the actual surface, not one groundBlockHeight above it.
                placementTracker.position += Vector3.up * moduleConfig.groundBlockHeight;
            }
            else if (!isElevatedSidewalk)
            {
                // Road path: shouldOmit is always false here (isElevatedSidewalk=false),
                // but guard this branch explicitly so Road never regresses.
                placementTracker.position += Vector3.up * moduleConfig.groundBlockHeight;
            }
        }

        // [BuildingAudit] — terrain exit snapshot
        GameLogger.Log("BuildingAudit",
            $"[TerrainExit] Segment={gameObject.name} " +
            $"paddingBlocksSpawned={paddingBlocksSpawned} " +
            $"foundationInstantiated={foundationInstantiated} " +
            $"placementTrackerY_afterTerrain={placementTracker.position.y:F3}");

        if (spawnObstacles && obstaclePrefabs != null && obstaclePrefabs.Length > 0 && obstacleSpawnPoint != null)
        {
            obstacleSpawnPoint.position = new Vector3(
                obstacleSpawnPoint.position.x,
                placementTracker.position.y,
                obstacleSpawnPoint.position.z
            );

            int currentObstacleRate = diffConfig != null ? diffConfig.groundObstacleChance : obstacleRate;
            int obstacleRoll = Random.Range(0, 100);
            if (obstacleRoll < currentObstacleRate)
            {
                // TEMP: obstacle list on the prefab defines valid spawns — no name-based level filter.
                // Filter by name (truck/car/etc.) and MaxObstacleLevel disabled while heights are being stabilized.
                List<GameObject> filteredPrefabs = new List<GameObject>();
                foreach (var p in obstaclePrefabs)
                {
                    if (p == null) continue;
                    // string n = p.name.ToLower();
                    // int level = 1;
                    // if (n.Contains("truck") || n.Contains("camion") || n.Contains("van")) level = 3;
                    // else if (n.Contains("car") || n.Contains("auto")) level = 2;
                    // if (level <= maxObstacleLevel) filteredPrefabs.Add(p);
                    filteredPrefabs.Add(p);
                }

                if (filteredPrefabs.Count > 0)
                {
                    GameObject obstaclePrefab = filteredPrefabs[Random.Range(0, filteredPrefabs.Count)];
                    GameObject newObstacle = Instantiate(obstaclePrefab, obstacleSpawnPoint.position, Quaternion.identity, transform);
                    
                    Vector3 locPos = newObstacle.transform.localPosition;
                    newObstacle.transform.localPosition = new Vector3(locPos.x, locPos.y, obstacleZOffset);
                    newObstacle.name = $"{obstaclePrefab.name}_Generated";
                }
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

        int buildingsSpawned = 0;

        // [BuildingAudit] — placementTracker snapshot before any building spawns
        GameLogger.Log("BuildingAudit",
            $"[BuildingEntry] Segment={gameObject.name} " +
            $"placementTrackerY_beforeBuildings={placementTracker.position.y:F3} " +
            $"transformY={transform.position.y:F3}");

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

            // [BuildingAudit] — per-building spawn snapshot
            GameLogger.Log("BuildingAudit",
                $"[BuildingSpawn] Segment={gameObject.name} " +
                $"buildingPrefab={buildingTemplate.name} " +
                $"buildingSpawnY={spawnPosition.y:F3} " +
                $"buildingLocalY={newBuilding.transform.localPosition.y:F3}");

            StructureBuilder buildingBuilder = newBuilding.GetComponent<StructureBuilder>();
            if (buildingBuilder != null)
            {
                buildingBuilder.ConstructStructure();
            }

            buildingsSpawned++;
        }


    }

    private void SpawnFlyingObstacles(int? explicitHeight = 0, DifficultyConfig diffConfig = null)
    {
        if (flyingObstaclePrefabs == null || flyingObstaclePrefabs.Length == 0) return;

        int currentFlyingRate = diffConfig != null ? diffConfig.flyingObstacleChance : flyingObstacleRate;
        float flyingObstacleRoll = Random.Range(0, 100);

        if (flyingObstacleRoll < currentFlyingRate)
        {
            // Use groundBlockHeight from config instead of hardcoding *3,
            // so obstacles follow the terrain if block scale is adjusted.
            float blockHeight = moduleConfig != null ? moduleConfig.groundBlockHeight : 3f;

            GameObject flyingObstaclePrefab = flyingObstaclePrefabs[Random.Range(0, flyingObstaclePrefabs.Length)];
            
            // Apply obstacleZOffset to flying obstacles too
            Vector3 flyingPosition = new Vector3(transform.position.x, ((float)explicitHeight * blockHeight) + 50, transform.position.z + obstacleZOffset);
            
            GameObject newFlyingObstacle = Instantiate(flyingObstaclePrefab, flyingPosition, Quaternion.identity, transform);
            newFlyingObstacle.name = $"{flyingObstaclePrefab.name}_Generated";
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
