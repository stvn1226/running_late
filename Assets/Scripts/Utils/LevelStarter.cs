using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

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

        // Home keeps it's height
        int homeHeight = homeModule.moduleConfig.minTerrainHeight;
        GameLogger.Log("LEVEL", $"Will spawn Home, Width={homeWidth:F2}, Height={homeHeight} (FIXED)");

        newWidth += (int)homeWidth;

        // Generate random modules
        for (int i = 0; i < numberOfModules; i++)
        {
            ModuleBuilder module = modules[Random.Range(0, modules.Length)];
            modulesToSpawn.Add(module);
            newWidth += module.moduleConfig.currentTerrainWidth;
        }

        // SCHOOL
        int schoolHeight = Random.Range(schoolModule.moduleConfig.minTerrainHeight, schoolModule.moduleConfig.maxTerrainHeight + 1);
        modulesToSpawn.Add(schoolModule);
        newWidth += schoolModule.moduleConfig.currentTerrainWidth;

        GameLogger.Log("LEVEL", $"Total modules: {modulesToSpawn.Count}, Width: {newWidth}, School Height: {schoolHeight}");

        // ========== SPAWNING CON SLOPES ==========
        
        float moduleSpawnerX = levelStartX + (homeWidth / 2f);
        Vector2 homePos = new Vector2(moduleSpawnerX, 0f);

        // Spawn HOME 
        ModuleBuilder spawnedHome = Instantiate(homeModule, homePos, Quaternion.identity, levelParent);
        spawnedHome.GenerateModule(homeHeight);
        modulesInScene.Add(spawnedHome);

        int previousHeight = homeHeight;
        GameLogger.Log("LEVEL", $"Home spawned at X={moduleSpawnerX:F2}, Height={previousHeight}");

        moduleSpawnerX += homeWidth / 2f;

        // Spawning
        for (int i = 0; i < modulesToSpawn.Count; i++)
        {
            ModuleBuilder module = modulesToSpawn[i];
            
            // Skip Home y School
            if (module == homeModule)
                continue;
            
            if (module == schoolModule)
            {
                break;
            }

            int heightChange = Random.Range(-1, 2);
            int nextHeight = previousHeight + heightChange;
            nextHeight = Mathf.Clamp(nextHeight, module.moduleConfig.minTerrainHeight, module.moduleConfig.maxTerrainHeight);

            // *** INSERT SLOPES WHEN HEIGHT CHANGES ***
            int heightDifference = nextHeight - previousHeight;
            
            if (heightDifference != 0)
            {
                GameLogger.Log("LEVEL", $"Height change detected: {previousHeight} -> {nextHeight} (diff: {heightDifference})");
                
                // INSERT SLOPES LEVEL BY LEVEL
                int slopesToSpawn = Mathf.Abs(heightDifference);
                bool isAscending = heightDifference > 0;
                ModuleBuilder slopeToUse = isAscending ? slopeLeftModule : slopeRightModule;
                
                for (int s = 0; s < slopesToSpawn; s++)
                {
                    moduleSpawnerX += slopeToUse.moduleConfig.currentTerrainWidth / 2f;
                    Vector2 slopePos = new Vector2(moduleSpawnerX, 0f);
                    
                    int slopeHeight = isAscending ? (previousHeight + s + 1) : (previousHeight - s);
                    
                    ModuleBuilder spawnedSlope = Instantiate(slopeToUse, slopePos, Quaternion.identity, levelParent);
                    spawnedSlope.GenerateModule(slopeHeight);
                    modulesInScene.Add(spawnedSlope);
                    
                    newWidth += slopeToUse.moduleConfig.currentTerrainWidth;
                    
                    GameLogger.Log("LEVEL", $"  Slope {s+1}/{slopesToSpawn} at X={moduleSpawnerX:F2}: Height={slopeHeight} ({(isAscending ? "UP" : "DOWN")})");
                    
                    moduleSpawnerX += slopeToUse.moduleConfig.currentTerrainWidth / 2f;
                }
            }

            // Spawn normal module
            moduleSpawnerX += module.moduleConfig.currentTerrainWidth / 2f;
            Vector2 spawnPos = new Vector2(moduleSpawnerX, 0f);

            ModuleBuilder newModule = Instantiate(module, spawnPos, Quaternion.identity, levelParent);
            newModule.GenerateModule(nextHeight);
            modulesInScene.Add(newModule);

            GameLogger.Log("LEVEL", $"Module at X={moduleSpawnerX:F2}: Height={nextHeight} (previous={previousHeight}, change={heightChange:+0;-0;0})");

            previousHeight = nextHeight;
            moduleSpawnerX += module.moduleConfig.currentTerrainWidth / 2f;
        }

        // *** TRANSITION TO SCHOOL WITH SLOPES ***
        int schoolHeightDiff = schoolHeight - previousHeight;
        
        if (schoolHeightDiff != 0)
        {
            GameLogger.Log("LEVEL", $"School transition: {previousHeight} -> {schoolHeight} (diff: {schoolHeightDiff})");
            
            int slopesToSchool = Mathf.Abs(schoolHeightDiff);
            bool isAscending = schoolHeightDiff > 0;
            ModuleBuilder slopeToUse = isAscending ? slopeLeftModule : slopeRightModule;
            
            for (int s = 0; s < slopesToSchool; s++)
            {
                moduleSpawnerX += slopeToUse.moduleConfig.currentTerrainWidth / 2f;
                Vector2 slopePos = new Vector2(moduleSpawnerX, 0f);
                
                int slopeHeight = isAscending ? (previousHeight + s + 1) : (previousHeight - s);
                
                ModuleBuilder spawnedSlope = Instantiate(slopeToUse, slopePos, Quaternion.identity, levelParent);
                spawnedSlope.GenerateModule(slopeHeight);
                modulesInScene.Add(spawnedSlope);
                
                newWidth += slopeToUse.moduleConfig.currentTerrainWidth;
                
                GameLogger.Log("LEVEL", $"  School Slope {s+1}/{slopesToSchool} at X={moduleSpawnerX:F2}: Height={slopeHeight}");
                
                moduleSpawnerX += slopeToUse.moduleConfig.currentTerrainWidth / 2f;
            }
        }

        // Spawn SCHOOL
        moduleSpawnerX += schoolModule.moduleConfig.currentTerrainWidth / 2f;
        Vector2 schoolPos = new Vector2(moduleSpawnerX, 0f);

        ModuleBuilder spawnedSchool = Instantiate(schoolModule, schoolPos, Quaternion.identity, levelParent);
        spawnedSchool.GenerateModule(schoolHeight);
        modulesInScene.Add(spawnedSchool);

        levelEndX = moduleSpawnerX + (schoolModule.moduleConfig.currentTerrainWidth / 2f);

        GameLogger.Log("LEVEL", $"School at X={moduleSpawnerX:F2}, Height={schoolHeight}");
        GameLogger.Log("LEVEL", $"=== GENERATION COMPLETE: StartX={levelStartX:F2}, EndX={levelEndX:F2} ===");

        ResizeCamBounds(0f);
        GenerateModularCloudsInRange(levelStartX, levelEndX);
        LayOutBackgroundInRange(levelStartX, levelEndX);

        OnCameraBoundsUpdated?.Invoke();
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

        SpriteRenderer tileSprite = backgroundTile.GetComponent<SpriteRenderer>();
        float tileWidth = tileSprite != null ? tileSprite.bounds.size.x : 10f;

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
}
