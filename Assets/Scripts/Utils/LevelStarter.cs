using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

[System.Serializable]
public class BiomeData
{
    public string biomeName;
    public ModuleBuilder[] modules;
    public GameObject[] continuousRoadPrefabs;
    public SpriteRenderer[] backgroundTiles;
}

public class LevelStarter : MonoBehaviour
{
    [Header("Level Generation Settings")]
    public bool generateOnStart = false;
    public bool generateOnSceneReload = true;
    public float generationDelay = 0.1f;

    [Header("Generation Mode")]
    [SerializeField] private bool useModularGeneration = true;

    [Header("Seed Settings")]
    public bool useManualSeed = false;
    [SerializeField] private int manualSeed = 12345;
    [SerializeField] private int currentSeed;
    [Space]
    [SerializeField] private bool showSeedInConsole = true;

    [Header("References")]
    public Transform levelParent;
    public PolygonCollider2D cameraBounds;
    [Tooltip("Keep it empty")]
    public StructureGenerator structureGenerator;
    public LevelManager levelManager;
    [Tooltip("Keep it empty")]
    public SpawnManager spawnManager;

    [Header("Visual Elements")]
    public SpriteRenderer[] clouds;
    public GameObject backgroundTile;
    public Transform backgroundParent;
    [Tooltip("Keep it empty")]
    public Transform cloudParent;

    [Header("Module Count Settings")]
    public int minModules = 5;
    public int maxModules = 10;
    public int targetSchoolHeight = 0; // The goal height for the END of the level

    [Header("Modular Level Settings")]
    public ModuleBuilder[] modules;
    public ModuleBuilder homeModule;
    public ModuleBuilder schoolModule;
    
    [Header("Visual Mask")]
    public GameObject homeMask;

    [Header("Building Generation Settings")]
    [Range(10, 50)]
    public int buildingsPerUnit = 20;
    [Range(0.5f, 2.0f)]
    public float buildingDensityMultiplier = 1.0f;
    
    [Header("Continuous Road Settings (Lower Level)")]
    public GameObject[] continuousRoadPrefabs;
    public Transform continuousRoadReference;
    public float continuousRoadModuleWidth = 10f;
    private List<GameObject> continuousRoadBlocks = new List<GameObject>();

    [Header("Biome System (Architecture Ready)")]
    [Tooltip("List of biomes to cycle through during the level.")]
    public List<BiomeData> biomesList = new List<BiomeData>();

    // -------------------------------------------------------------------------
    // DESIGNER SCALE SETTINGS
    // -------------------------------------------------------------------------
    // The designer works with "levels" starting at 0 (road level).
    // Internally the system uses "blocks" starting at 1.
    // -------------------------------------------------------------------------
    [Header("Designer Scale Settings")]
    [Tooltip("Base sidewalk height in blocks. Defines how high the second path is above the road.\n" +
             "2 = standard (recommended). 3 = high. 1 = very low.\n" +
             "DEFAULT = 2.")]
    public int sidewalkMinBlocks = 2;

    [Tooltip("Maximum height in blocks the sidewalk can reach.\n" +
             "The random walk will stay within sidewalkMinBlocks and this value.\n" +
             "DEFAULT = 5.")]
    public int sidewalkMaxBlocks = 5;

    [Tooltip("Vertical offset for the sidewalk path relative to the road.\n" +
             "DEFAULT = 0.0f (Locked to Baseline)")]
    public float sidewalkHeightOffset = 0.0f;

    [Tooltip("Z-axis separation for the entire sidewalk path.\n" +
             "0.5 = Background Path (Back).\n" +
             "DEFAULT = 0.5f")]
    public float sidewalkZOffset = 0.5f;

    [Tooltip("How many internal blocks the sidewalk rises for every 1 designer unit.\n" +
             "Value 1 = 1:1 ratio (recommended).")]
    public int blocksPerDesignerUnit = 1;

    [Header("Inter-Layer Slopes")]
    [Tooltip("Definitions for slope prefabs and the designer height units they cover.\n" +
             "The system combines these to match the current sidewalk height.")]
    public InterLayerSlopeEntry[] interLayerSlopePrefabs;
    
    [Range(0, 100)] 
    [Tooltip("Chance (0-100) to spawn a slope connection in a module.")]
    public int interLayerSlopeSpawnChance = 10;

    [Tooltip("Fine vertical adjustment for the slope's base connection to the road.")]
    public float interLayerSlopeYOffset = 0f;

    [Tooltip("Z-axis separation for the slope. Must be BETWEEN road and sidewalk.\n" +
             "Example: Road(0) < Slope(0.25) < Sidewalk(0.5).\n" +
             "DEFAULT = 0.25f")]
    public float interLayerSlopeZOffset = 0.25f;

    [Header("Slope Modules")]
    public ModuleBuilder slopeLeftModule;
    public ModuleBuilder slopeRightModule;

    [Header("=== LEGACY PROCEDURAL (Unused) ===")]
    [SerializeField] private bool generateBuildingsInModules = false;
    public ModuleBuilder moduleTest;
    public Transform groundReference;
    public SpriteRenderer[] background;
    public GameObject home;
    public GameObject school;
    public int minLevelWidth = 1000;
    public int maxLevelWidth = 2000;
    public bool autoClearStructures = true;

    private int newWidth;
    private int numberOfModules;
    private float levelStartX;
    private float levelEndX;
    private List<ModuleBuilder> modulesInScene = new List<ModuleBuilder>();
    private List<SpriteRenderer> cloudsInScene = new List<SpriteRenderer>();
    private GameObject cloudCache;
    private bool hasGenerated = false;

    private DateTime generationStartTime;
    private DateTime playerSpawnStartTime;

    public static LevelStarter Instance { get; private set; }

    public System.Action OnLevelGenerationStarted;
    public System.Action OnLevelGenerationCompleted;
    public System.Action OnCameraBoundsUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            GameLogger.Log("Level", "Duplicate LevelStarter Instance");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelReplayStarted += OnLevelRestart;
        }
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt("UseRetrySeed", 0) == 1)
        {
            int retrySeed = PlayerPrefs.GetInt("RetrySeed", 0);
            SetManualSeed(retrySeed);
            PlayerPrefs.DeleteKey("UseRetrySeed");
            PlayerPrefs.DeleteKey("RetrySeed");
            GameLogger.Log("LEVEL", $"Retry with same seed: {retrySeed}");
        }
        else if (PlayerPrefs.GetInt("UseNewLevelSeed", 0) == 1)
        {
            int newSeed = PlayerPrefs.GetInt("NewLevelSeed", 0);
            SetManualSeed(newSeed);
            PlayerPrefs.DeleteKey("UseNewLevelSeed");
            PlayerPrefs.DeleteKey("NewLevelSeed");
            GameLogger.Log("LEVEL", $"New level with manual seed: {newSeed}");
        }

        if (homeMask != null)
        {
            homeMask.SetActive(true);
            GameLogger.Log("LEVEL", "Home mask activated for menu");
        }
    
        if (generateOnStart)
        {
            GameLogger.Log("LEVEL", "Auto-generating level on Start (generateOnStart = true)");
            ForceRegenerate();
        }
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelReplayStarted -= OnLevelRestart;
        }
    }

    private void LogCurrentLevel()
    {
        if (LevelLogger.Instance == null) return;

        LevelData levelData = new LevelData();
        levelData.seed = currentSeed;
        levelData.generationType = useModularGeneration ? "Modular" : "Procedural";
        levelData.levelWidth = newWidth;
        levelData.moduleCount = numberOfModules;
        levelData.loadTime = (float)(DateTime.Now - generationStartTime).TotalSeconds;

        levelData.totalGameObjects = levelParent.childCount;
        if (backgroundParent != null) levelData.totalGameObjects += backgroundParent.childCount;
        if (cloudParent != null) levelData.totalGameObjects += cloudParent.childCount;

        if (structureGenerator != null)
        {
            StructureBuilder[] buildings = levelParent.GetComponentsInChildren<StructureBuilder>();
            levelData.buildingCount = buildings.Length;
            levelData.buildingDensity = (float)buildings.Length / (newWidth / 100f);
        }

        levelData.cameraBoundsWidth = Mathf.RoundToInt(cameraBounds.bounds.size.x);

        List<string> moduleNames = new List<string>();
        foreach (ModuleBuilder module in modulesInScene)
        {
            string moduleName = module.name.Replace("(Clone)", "").Replace("Module_Final", "");
            moduleNames.Add(moduleName);
        }
        levelData.moduleSequence = moduleNames.ToArray();

        if (playerSpawnStartTime != DateTime.MinValue)
        {
            levelData.playerSpawnTime = (float)(DateTime.Now - playerSpawnStartTime).TotalSeconds;
        }

        if (generateBuildingsInModules) levelData.notes += "Buildings in modules; ";
        if (useManualSeed) levelData.notes += "Manual seed; ";

        LevelLogger.Instance.LogLevel(levelData);
    }

    private void InitializeSeed()
    {
        if (useManualSeed)
        {
            currentSeed = manualSeed;
        }
        else
        {
            currentSeed = System.DateTime.Now.Millisecond +
                         System.DateTime.Now.Second * 1000 +
                         System.DateTime.Now.Minute * 60000;
        }

        UnityEngine.Random.InitState(currentSeed);

        if (showSeedInConsole)
        {
            GameLogger.Log("LEVEL", $"Level Seed: {currentSeed}");
        }
    }

    public void SetManualSeed(int seed)
    {
        manualSeed = seed;
        useManualSeed = true;
        currentSeed = seed;
    }

    public void GenerateWithSeed(int seed)
    {
        SetManualSeed(seed);
        RegenerateLevel();
    }

    public void RandomizeSeed()
    {
        useManualSeed = false;
        RegenerateLevel();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DESIGNER SCALE CONVERSION HELPERS
    //
    //   DesignerToCodeHeight(0)  →  baseSidewalkHeight        (suelo vereda)
    //   DesignerToCodeHeight(1)  →  baseSidewalkHeight + 1    (1 nivel arriba)
    //   DesignerToCodeHeight(2)  →  baseSidewalkHeight + 2    ...etc
    //
    //   CodeToDesignerHeight(3)  →  0   (si baseSidewalkHeight == 3)
    //   CodeToDesignerHeight(5)  →  2
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Convierte un nivel del diseñador (0 = suelo de la vereda) a la altura
    /// interna en bloques que usa el generador.
    /// </summary>
    public int DesignerToCodeHeight(int designerLevel)
    {
        return sidewalkMinBlocks + (designerLevel * blocksPerDesignerUnit);
    }

    /// <summary>
    /// Returns the active biome for a given position. 
    /// Architecture ready for 4-biome segmentation.
    /// </summary>
    private BiomeData GetCurrentBiome(float xPos)
    {
        if (biomesList == null || biomesList.Count == 0) return null;
        
        // Simple division by 4 for biome segmentation
        float progress = Mathf.Clamp01((xPos - levelStartX) / (levelEndX - levelStartX));
        int index = Mathf.FloorToInt(progress * biomesList.Count);
        index = Mathf.Clamp(index, 0, biomesList.Count - 1);
        
        return biomesList[index];
    }

    /// <summary>
    /// Convierte una altura interna en bloques al nivel del diseñador
    /// (inversa de DesignerToCodeHeight). Útil para logs y debug.
    /// </summary>
    public int CodeToDesignerHeight(int codeHeight)
    {
        if (blocksPerDesignerUnit <= 0) return 0;
        return (codeHeight - sidewalkMinBlocks) / blocksPerDesignerUnit;
    }

    private void OnLevelRestart()
    {
        hasGenerated = false;
    }

    private IEnumerator GenerateLevelWithDelay()
    {
        OnLevelGenerationStarted?.Invoke();
        generationStartTime = DateTime.Now;

        yield return new WaitForSecondsRealtime(generationDelay);

        InitializeSeed();

        if (AnalyticsManager.Instance != null)
        {
            string levelType = useModularGeneration ? "Modular" : "Procedural";
            AnalyticsManager.Instance.TrackLevelStart(currentSeed, levelType);
        }

        if (useModularGeneration)
        {
            if (autoClearStructures) ClearModules();
            ResizeLevelModular();
        }
        else
        {
            if (autoClearStructures && structureGenerator != null) 
                structureGenerator.ClearStructuresInEditor();
            ResizeLevel();
        }

        if (homeMask != null)
        {
            homeMask.SetActive(false);
            GameLogger.Log("LEVEL", "Home mask deactivated - level generated");
        }

        hasGenerated = true;
        OnLevelGenerationCompleted?.Invoke();

        yield return new WaitForEndOfFrame();

        RecentLevelsList.instance.AddRecentLevel(currentSeed);
        LogCurrentLevel();
        NotifySpawnManager();
    }

    private void NotifySpawnManager()
    {
        playerSpawnStartTime = DateTime.Now;
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.OnLevelGenerated();
        }
    }

    public void RegenerateLevel()
    {
        if (!hasGenerated) return;
        hasGenerated = false;
        
        if (homeMask != null)
        {
            homeMask.SetActive(true);
            GameLogger.Log("LEVEL", "Home mask reactivated for regeneration");
        }
        
        StartCoroutine(GenerateLevelWithDelay());
    }

    public void ForceRegenerate()
    {
        hasGenerated = false;
        StartCoroutine(GenerateLevelWithDelay());
    }

    private void ResizeLevel()
    {
        newWidth = Random.Range(minLevelWidth, maxLevelWidth);

        if (groundReference != null)
        {
            groundReference.GetComponent<SpriteRenderer>().size = new Vector2(newWidth, groundReference.GetComponent<SpriteRenderer>().size.y);
            groundReference.GetComponent<BoxCollider2D>().size = new Vector2(newWidth, groundReference.GetComponent<BoxCollider2D>().size.y);
        }

        levelStartX = -newWidth / 2f;
        levelEndX = newWidth / 2f;

        ResizeCamBounds(-8.75f);

        if (background != null)
        {
            foreach (SpriteRenderer bg in background)
            {
                if (bg != null)
                {
                    bg.size = new Vector2(newWidth, bg.size.y);
                }
            }
        }

        GenerateProceduralClouds();
        PositionEndPoints(newWidth);
        GenerateBuildings();
    }

    private void ResizeLevelModular()
    {
        GameLogger.Log("LEVEL", "=== MODULAR GENERATION START ===");

        newWidth = 0;
        numberOfModules = Random.Range(minModules, maxModules);
        List<ModuleBuilder> modulesToSpawn = new List<ModuleBuilder>();

        modulesToSpawn.Add(homeModule);
        float homeWidth = homeModule.moduleConfig.currentTerrainWidth;
        levelStartX = -homeWidth / 2f;

        // RESTORED: Home starts at Level 1 to have its foundation and avoid gaps
        int homeHeight = 1; 
        GameLogger.Log("LEVEL", $"Spawning Home at Road Level: Width={homeWidth:F2}, HeightBlocks={homeHeight}");

        newWidth += (int)homeWidth;

        // Generate random modules
        for (int i = 0; i < numberOfModules; i++)
        {
            ModuleBuilder module = modules[Random.Range(0, modules.Length)];
            modulesToSpawn.Add(module);
            newWidth += module.moduleConfig.currentTerrainWidth;
        }

        // SCHOOL - Calculate target height within global sidewalk range
        int finalSchoolHeight = Random.Range(sidewalkMinBlocks, sidewalkMaxBlocks + 1);
        GameLogger.Log("LEVEL", $"School target height: {finalSchoolHeight} (Range: {sidewalkMinBlocks}-{sidewalkMaxBlocks})");
        
        modulesToSpawn.Add(schoolModule);
        newWidth += schoolModule.moduleConfig.currentTerrainWidth;

        GameLogger.Log("LEVEL", $"Total modules: {modulesToSpawn.Count}, Width: {newWidth}, Target School Height: {finalSchoolHeight}");

        // ========== SPAWNING CON SLOPES ==========

        // ROAD REFERENCE: The House dictates the height of the entire level.
        // We assume the Home module is already placed correctly or its Y is the standard.
        float roadBaseY = continuousRoadReference != null ? continuousRoadReference.position.y : 0f;
        float blockH = homeModule.moduleConfig.groundBlockHeight;
        
        // Force sidewalk to start exactly at road level to prevent floating objects
        float sidewalkSpawnY = roadBaseY;
        
        GameLogger.Log("LEVEL", $"Base Y: {roadBaseY:F2}, Sidewalk Z: {sidewalkZOffset:F2}");

        float moduleSpawnerX = levelStartX + (homeWidth / 2f);
        Vector3 homePos = new Vector3(moduleSpawnerX, sidewalkSpawnY, sidewalkZOffset);

        // Spawn HOME - FIXED POSITION (No initial gap)
        ModuleBuilder spawnedHome = Instantiate(homeModule, homePos, Quaternion.identity, levelParent);
        spawnedHome.omitFoundationAtBaseLevel = false; // REFILL HOME
        spawnedHome.GenerateModule(homeHeight);
        modulesInScene.Add(spawnedHome);
        
        // Rule 5: Starting Spacing Buffer (Applied AFTER the house)
        moduleSpawnerX += (homeWidth / 2f) + 20f; 
        
        int previousHeight = homeHeight;

        // Spawning
        int modulesSpawningCount = modulesToSpawn.Count;
        for (int i = 0; i < modulesSpawningCount; i++)
        {
            ModuleBuilder module = modulesToSpawn[i];
            
            // Skip Home (already spawned)
            if (module == homeModule)
                continue;
            
            // Skip School (spawned after loop, but we need its target height now)
            if (module == schoolModule)
                continue;

            // --- BIASED HEIGHT COMPENSATION LOGIC ---
            int remainingModules = (modulesSpawningCount - i); // Updated calculation for better foresight
            int heightDiff = finalSchoolHeight - previousHeight;
            
            int heightChange = 0;

            // EMERGENCY MODE: If we MUST change height now to make it in time
            if (Mathf.Abs(heightDiff) >= remainingModules && remainingModules > 0)
            {
                heightChange = (int)Mathf.Sign(heightDiff);
                GameLogger.Log("LEVEL", $"FORCE HEIGHT CHANGE: Diff={heightDiff}, Remaining={remainingModules}, Forcing Change={heightChange}");
            }
            else
            {
                // BIASED RANDOM WALK:
                float randomVal = Random.value;
                
                if (heightDiff > 0) // Target is higher
                {
                    if (randomVal < 0.45f) heightChange = 1;
                    else if (randomVal < 0.90f) heightChange = 0;
                    else heightChange = -1;
                }
                else if (heightDiff < 0) // Target is lower
                {
                    if (randomVal < 0.45f) heightChange = -1;
                    else if (randomVal < 0.90f) heightChange = 0;
                    else heightChange = 1;
                }
                else // Already at target height
                {
                    if (randomVal < 0.20f) heightChange = 1;
                    else if (randomVal < 0.40f) heightChange = -1;
                    else heightChange = 0;
                }
            }

            int nextHeight = previousHeight + heightChange;
            
            // CLAMP usando los límites globales del sidewalk, no el moduleConfig,
            // porque el ScriptableObject puede tener maxTerrainHeight=1 y bloquear la variación.
            nextHeight = Mathf.Clamp(nextHeight, sidewalkMinBlocks, sidewalkMaxBlocks);

            // *** INSERT SLOPES WHEN HEIGHT CHANGES ***
            int heightDifference = nextHeight - previousHeight;
            
            if (heightDifference != 0)
            {
                GameLogger.Log("LEVEL", $"Height change: {previousHeight} -> {nextHeight} (diff: {heightDifference})");
                
                int slopesToSpawn = Mathf.Abs(heightDifference);
                bool isAscending = heightDifference > 0;
                ModuleBuilder slopeToUse = isAscending ? slopeLeftModule : slopeRightModule;
                
                for (int s = 0; s < slopesToSpawn; s++)
                {
                    moduleSpawnerX += slopeToUse.moduleConfig.currentTerrainWidth / 2f;
                    Vector3 slopePos = new Vector3(moduleSpawnerX, sidewalkSpawnY, sidewalkZOffset);
                    
                    // Rule 3: Perfect Slope Logic (Restored from history)
                    int slopeHeight = isAscending ? (previousHeight + s + 1) : (previousHeight - s);
                    
                    ModuleBuilder spawnedSlope = Instantiate(slopeToUse, slopePos, Quaternion.identity, levelParent);
                    spawnedSlope.omitFoundationAtBaseLevel = false; // REFILL SLOPES
                    spawnedSlope.GenerateModule(slopeHeight);
                    modulesInScene.Add(spawnedSlope);
                    
                    newWidth += slopeToUse.moduleConfig.currentTerrainWidth;
                    moduleSpawnerX += slopeToUse.moduleConfig.currentTerrainWidth / 2f;
                }
            }

            // INTER-LAYER SLOPES REMOVED (As requested, using standard slopes only)
            bool isFlatSection = nextHeight == previousHeight;

            // Spawn normal module
            moduleSpawnerX += module.moduleConfig.currentTerrainWidth / 2f;
            Vector3 spawnPos = new Vector3(moduleSpawnerX, sidewalkSpawnY, sidewalkZOffset);

            ModuleBuilder newModule = Instantiate(module, spawnPos, Quaternion.identity, levelParent);
            newModule.GenerateModule(nextHeight);
            modulesInScene.Add(newModule);

            previousHeight = nextHeight;
            moduleSpawnerX += module.moduleConfig.currentTerrainWidth / 2f;
        }

        // *** FINAL TRANSITION TO SCHOOL ***
        int schoolHeightDiff = finalSchoolHeight - previousHeight;
        
        if (schoolHeightDiff != 0)
        {
            int slopesToSchool = Mathf.Abs(schoolHeightDiff);
            bool isAscending = schoolHeightDiff > 0;
            ModuleBuilder slopeToUse = isAscending ? slopeLeftModule : slopeRightModule;
            
            for (int s = 0; s < slopesToSchool; s++)
            {
                moduleSpawnerX += slopeToUse.moduleConfig.currentTerrainWidth / 2f;
                // Rule 3: Perfect Slope Logic for school transition
                int slopeHeight = isAscending ? (previousHeight + s + 1) : (previousHeight - s);
                
                Vector3 slopePos = new Vector3(moduleSpawnerX, sidewalkSpawnY, sidewalkZOffset);
                ModuleBuilder spawnedSlope = Instantiate(slopeToUse, slopePos, Quaternion.identity, levelParent);
                spawnedSlope.omitFoundationAtBaseLevel = false; // REFILL SLOPES
                spawnedSlope.GenerateModule(slopeHeight);
                modulesInScene.Add(spawnedSlope);
                newWidth += slopeToUse.moduleConfig.currentTerrainWidth;
                moduleSpawnerX += slopeToUse.moduleConfig.currentTerrainWidth / 2f;
            }
        }

        // Spawn SCHOOL
        moduleSpawnerX += schoolModule.moduleConfig.currentTerrainWidth / 2f;
        Vector3 schoolPos = new Vector3(moduleSpawnerX, sidewalkSpawnY, sidewalkZOffset);

        ModuleBuilder spawnedSchool = Instantiate(schoolModule, schoolPos, Quaternion.identity, levelParent);
        spawnedSchool.GenerateModule(finalSchoolHeight);
        modulesInScene.Add(spawnedSchool);

        levelEndX = moduleSpawnerX + (schoolModule.moduleConfig.currentTerrainWidth / 2f);

        GameLogger.Log("LEVEL", $"School at X={moduleSpawnerX:F2}, Height={finalSchoolHeight}");
        GameLogger.Log("LEVEL", $"=== GENERATION COMPLETE: StartX={levelStartX:F2}, EndX={levelEndX:F2} ===");

        ResizeCamBounds(0f);

        // Road starts AFTER the house to avoid overlapping with the house's foundation
        float roadStart = levelStartX + homeWidth; 
        GenerateContinuousRoad(roadStart, levelEndX, roadBaseY);

        GenerateModularCloudsInRange(levelStartX, levelEndX);
        LayOutBackgroundInRange(levelStartX, levelEndX);

        OnCameraBoundsUpdated?.Invoke();
    }

    private void GenerateContinuousRoad(float startX, float endX, float targetY)
    {
        if (continuousRoadReference == null || continuousRoadPrefabs == null || continuousRoadPrefabs.Length == 0) return;

        GameLogger.Log("LEVEL", $"Generating Continuous Road from {startX} to {endX} at Y={targetY}");
        
        // CLEANUP PREVIOUS ROAD BLOCKS TO PREVENT DUPLICATION
        foreach (GameObject road in continuousRoadBlocks)
        {
            if (road != null)
            {
                if (Application.isPlaying) Destroy(road);
                else DestroyImmediate(road);
            }
        }
        continuousRoadBlocks.Clear();

        float currentX = startX;
        float finalX = endX + 50f; // Padding to ensure it covers the whole visible area
        int safetyCounter = 0;

        while (currentX < finalX && safetyCounter < 1000)
        {
            GameObject roadPrefab = continuousRoadPrefabs[Random.Range(0, continuousRoadPrefabs.Length)];
            // Road is at the front layer (Z=0)
            Vector3 spawnPos = new Vector3(currentX, targetY, 0.0f); 
            GameObject roadInstance = Instantiate(roadPrefab, spawnPos, Quaternion.identity, levelParent);
            roadInstance.name = "ContinuousRoad_Generated";
            
            continuousRoadBlocks.Add(roadInstance);
            
            // STABLE WIDTH: Strictly use the designer-set width, no dynamic bounds logic
            float width = continuousRoadModuleWidth;
            
            // Safety: Never use a width near zero
            if (width <= 0.1f) width = 20f; // Fallback hardcoded width
            
            currentX += width;
            safetyCounter++;
        }

        if (safetyCounter >= 1000)
        {
            GameLogger.LogWarning("LEVEL", "Continuous Road generation hit safety limit (1000 blocks). Possible width calculation error.");
        }
    }

    private void GenerateModularCloudsInRange(float startX, float endX)
    {
        if (clouds == null || clouds.Length == 0 || cloudParent == null)
        {
            GameLogger.LogWarning("LEVEL", "Cannot generate clouds: clouds array is empty or cloudParent is null");
            return;
        }

        float rangeWidth = endX - startX;
        int cloudCount = Mathf.RoundToInt(rangeWidth / 2f);

        GameLogger.Log("LEVEL", $"Generating {cloudCount} clouds from X={startX:F2} to X={endX:F2}");

        for (int i = 0; i < cloudCount; i++)
        {
            Vector2 cloudPosition = new Vector2(
                Random.Range(startX, endX),
                Random.Range(8f, 100f)
            );
            int cloudIndex = Random.Range(0, clouds.Length);

            cloudCache = Instantiate(clouds[cloudIndex].gameObject, cloudPosition, Quaternion.identity, cloudParent);
            cloudsInScene.Add(cloudCache.GetComponent<SpriteRenderer>());
        }
    }

    private void LayOutBackgroundInRange(float startX, float endX)
    {
        if (backgroundTile == null || backgroundParent == null)
        {
            GameLogger.LogWarning("LEVEL", "Cannot generate background: backgroundTile or backgroundParent is null");
            return;
        }

        SpriteRenderer tileSprite = backgroundTile.GetComponentInChildren<SpriteRenderer>();
        float tileWidth = tileSprite != null ? tileSprite.bounds.size.x : 40f;

        float rangeWidth = endX - startX;
        int tileCount = Mathf.CeilToInt(rangeWidth / tileWidth);

        GameLogger.Log("LEVEL", $"Generating {tileCount} background tiles from X={startX:F2} to X={endX:F2}");

        for (int i = 0; i < tileCount; i++)
        {
            float xPos = startX + (i * tileWidth) + (tileWidth / 2f);
            Vector3 tilePos = new Vector3(xPos, backgroundTile.transform.position.y, backgroundTile.transform.position.z);
            Instantiate(backgroundTile, tilePos, Quaternion.identity, backgroundParent);
        }
    }

    private void GenerateModularBuildingsInRange(float startX, float endX)
    {
        if (structureGenerator == null)
        {
            GameLogger.LogWarning("LEVEL", "Cannot generate buildings: structureGenerator is null");
            return;
        }

        float rangeWidth = endX - startX;
        int buildingCount = Mathf.RoundToInt((rangeWidth / 100f) * buildingsPerUnit * buildingDensityMultiplier);

        GameLogger.Log("LEVEL", $"Generating {buildingCount} buildings from X={startX:F2} to X={endX:F2}");

        structureGenerator.numberOfBuildings = buildingCount;
        structureGenerator.spawnAreaMin = new Vector2(startX, 0f);
        structureGenerator.spawnAreaMax = new Vector2(endX, 0f);
        structureGenerator.GenerateStructuresInEditor();
    }

    private void GenerateProceduralClouds()
    {
        if (clouds == null || clouds.Length == 0 || cloudParent == null) return;

        ClearClouds();

        for (int i = 0; i < newWidth / 2; i++)
        {
            Vector2 cloudPosition = new Vector2(
                Random.Range(-newWidth / 2f, newWidth / 2f),
                Random.Range(8f, 100f)
            );
            int cloudIndex = Random.Range(0, clouds.Length);

            cloudCache = Instantiate(clouds[cloudIndex].gameObject, cloudPosition, Quaternion.identity, cloudParent);
            cloudsInScene.Add(cloudCache.GetComponent<SpriteRenderer>());
        }
    }

    private void GenerateBuildings()
    {
        if (structureGenerator == null) return;

        int totalBuildings = Mathf.RoundToInt((newWidth / buildingsPerUnit) * buildingDensityMultiplier);

        structureGenerator.numberOfBuildings = totalBuildings;
        structureGenerator.spawnAreaMin = new Vector2(-newWidth / 2f + 50f, 0);
        structureGenerator.spawnAreaMax = new Vector2(newWidth / 2f - 50f, 0);

        structureGenerator.GenerateStructuresInEditor();

        if (showSeedInConsole)
        {
            GameLogger.Log("LEVEL", $"Generated {totalBuildings} buildings across {newWidth} units (density: {buildingDensityMultiplier:F1}x)");
        }
    }

    private void ClearClouds()
    {
        if (cloudParent != null)
        {
            if (cloudsInScene.Count == 0)
            {
                SpriteRenderer[] lostClouds = cloudParent.gameObject.GetComponentsInChildren<SpriteRenderer>();
                foreach (SpriteRenderer c in lostClouds)
                {
                    if (c != null)
                    {
                        if (Application.isPlaying)
                            Destroy(c.gameObject);
                        else
                            DestroyImmediate(c.gameObject);
                    }
                }
            }
            else
            {
                foreach (SpriteRenderer cloud in cloudsInScene)
                {
                    if (cloud != null)
                    {
                        if (Application.isPlaying)
                            Destroy(cloud.gameObject);
                        else
                            DestroyImmediate(cloud.gameObject);
                    }
                }
            }
        }
        cloudsInScene.Clear();
    }

    private void ClearModules()
    {
        GameLogger.Log("LEVEL", "Clearing all modules except pre-placed Home");

        List<Transform> modulesToClear = new List<Transform>();

        for (int i = 0; i < levelParent.childCount; i++)
        {
            Transform child = levelParent.GetChild(i);
            modulesToClear.Add(child);
        }

        if (backgroundParent != null)
        {
            for (int i = 0; i < backgroundParent.childCount; i++)
            {
                modulesToClear.Add(backgroundParent.GetChild(i));
            }
        }

        ClearClouds();

        foreach (Transform module in modulesToClear)
        {
            if (Application.isPlaying)
                Destroy(module.gameObject);
            else
                DestroyImmediate(module.gameObject);
        }

        modulesInScene.Clear();

        // Clear Continuous Road
        foreach (GameObject road in continuousRoadBlocks)
        {
            if (road != null)
            {
                if (Application.isPlaying) Destroy(road);
                else DestroyImmediate(road);
            }
        }
        continuousRoadBlocks.Clear();
        
        // Clear Inter-Layer Slopes
        Transform[] allChildren = levelParent.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child != null && child.name == "InterLayerSlope_Generated")
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        if (structureGenerator != null)
        {
            structureGenerator.ClearStructuresInEditor();
        }
    }

    private void ResizeCamBounds(float yMin)
    {
        if (cameraBounds == null) return;

        cameraBounds.points = new Vector2[]
        {
            new Vector2(levelStartX, yMin),
            new Vector2(levelStartX, 100f),
            new Vector2(levelEndX, 100f),
            new Vector2(levelEndX, yMin)
        };

        GameLogger.Log("LEVEL", $"Camera bounds resized: X=[{levelStartX:F2}, {levelEndX:F2}], Y=[{yMin:F2}, 100]");
    }

    private void LayOutBackground()
    {
        if (backgroundTile == null || backgroundParent == null) return;

        Vector2 bgPos = new Vector2(-(newWidth / 2) - 5, 0f);
        for (int i = 0; i <= (newWidth / 10); i++)
        {
            bgPos.x += 10;
            Instantiate(backgroundTile, bgPos, Quaternion.identity, backgroundParent);
        }
    }

    private void PositionEndPoints(int levelWidth)
    {
        if (home != null && groundReference != null)
            home.transform.position = new Vector3(-levelWidth / 2f + 10f, groundReference.position.y + .5f, home.transform.position.z);
        if (school != null && groundReference != null)
            school.transform.position = new Vector3(levelWidth / 2f - 10f, groundReference.position.y + .5f, school.transform.position.z);
    }

    [ContextMenu("Start Procedural Level (Buildings)")]
    public void StartLevelInEditor()
    {
        InitializeSeed();
        useModularGeneration = false;
        if (autoClearStructures && structureGenerator != null) structureGenerator.ClearStructuresInEditor();
        ResizeLevel();
        if (levelManager != null) levelManager.ReplayLevel();
    }

    [ContextMenu("Start Procedural Level + Spawn")]
    public void StartLevelInEditorTwain()
    {
        InitializeSeed();
        useModularGeneration = false;
        if (autoClearStructures && structureGenerator != null) structureGenerator.ClearStructuresInEditor();
        ResizeLevel();
        if (spawnManager != null) spawnManager.InitializeAndSpawnPlayer();
    }

    [ContextMenu("Start Modular Level")]
    public void StartLevelModular()
    {
        InitializeSeed();
        useModularGeneration = true;
        if (autoClearStructures) ClearModules();
        ResizeLevelModular();
    }

    [ContextMenu("Show Level Log")]
    public void ShowLevelLog()
    {
        if (LevelLogger.Instance != null)
        {
            LevelLogger.Instance.ShowLogStats();
        }
    }

    [ContextMenu("Generate Level (Runtime)")]
    public void GenerateLevelManual()
    {
        hasGenerated = false;
        StartCoroutine(GenerateLevelWithDelay());
    }

    [ContextMenu("Clear Level")]
    public void ClearLevelManual()
    {
        ClearModules();
        hasGenerated = false;
    }

    [ContextMenu("Generate Random Seed")]
    public void GenerateRandomSeed()
    {
        RandomizeSeed();
    }

    [ContextMenu("Regenerate Same Seed")]
    public void RegenerateSameSeed()
    {
        ForceRegenerate();
    }

    public bool HasGeneratedLevel => hasGenerated;
    public int GetLevelWidth() => newWidth;
    public int GetModuleCount() => numberOfModules;
    public int GetCurrentSeed() => currentSeed;
    public bool IsUsingManualSeed() => useManualSeed;
    public bool IsUsingModularGeneration() => useModularGeneration;
    public float GetLevelStartX() => levelStartX;
    public float GetLevelEndX() => levelEndX;

    // -------------------------------------------------------------------------
    // INTER-LAYER SLOPE: SPAWN LOGIC
    // -------------------------------------------------------------------------
    // Dada la posición X y la altura actual de la vereda (en bloques internos),
    // elige y coloca la combinación correcta de slopes para ir desde el road
    // hasta la vereda, usando los prefabs disponibles de mayor a menor tamaño
    // (estrategia greedy).
    //
    // IMPORTANTE: El slope se posiciona en Y = continuousRoadReference.position.y
    // para que arranque desde el piso del road, no desde el origen del mundo.
    // -------------------------------------------------------------------------
    /// <summary>
    /// Spawns inter-layer slopes connecting the road and the sidewalk.
    /// If the prefab has a ModuleBuilder, it will generate its own foundation/padding.
    /// </summary>
    private void TrySpawnInterLayerSlopes(float xPos, int blocksToCover)
    {
        if (interLayerSlopePrefabs == null || interLayerSlopePrefabs.Length == 0) return;

        // Slopes MUST start exactly at the road surface level
        float roadBaseY = continuousRoadReference != null ? continuousRoadReference.position.y : 0f;
        float currentY = roadBaseY + interLayerSlopeYOffset;
        float blockH = homeModule.moduleConfig.groundBlockHeight;

        int remaining = blocksToCover;
        int slopeIndex = 0;

        // Sort descending
        var sorted = new System.Collections.Generic.List<InterLayerSlopeEntry>(interLayerSlopePrefabs);
        sorted.Sort((a, b) => b.designerHeightUnits.CompareTo(a.designerHeightUnits));

        while (remaining > 0)
        {
            var chosen = sorted.Find(s => s.designerHeightUnits <= remaining);
            if (chosen == null) chosen = sorted[sorted.Count - 1];

            // Spawn at road level or stacked above the previous slope part
            Vector3 spawnPos = new Vector3(xPos, currentY, interLayerSlopeZOffset);
            GameObject instance = Instantiate(chosen.prefab, spawnPos, Quaternion.identity, levelParent);
            instance.name = $"InterLayerSlope_H{chosen.designerHeightUnits}_X{xPos:F0}";
            
            ModuleBuilder mb = instance.GetComponent<ModuleBuilder>();
            if (mb != null) 
            {
                mb.omitFoundationAtBaseLevel = false; // REFILL SLOPES
                mb.GenerateModule(chosen.designerHeightUnits);
            }

            // Stack vertically to reach the sidewalk
            currentY += chosen.designerHeightUnits * blockH;
            remaining -= chosen.designerHeightUnits;
            slopeIndex++;
        }
    }
}
