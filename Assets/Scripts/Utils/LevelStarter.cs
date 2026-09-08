using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public enum LevelPathMode { DualPath, SingleSidewalk }

[System.Serializable]
public class FloorTheme
{
    public string themeId;
    public ModuleBuilder[] flatModules;
    public ModuleBuilder slopeLeftModule;
    public ModuleBuilder slopeRightModule;
}

public class LevelStarter : MonoBehaviour
{
    [Header("Level Generation Settings")]
    public bool generateOnStart = false;
    public bool generateOnSceneReload = true;
    public float generationDelay = 0.1f;

    [Header("Generation Mode")]
    [SerializeField] private bool useModularGeneration = true;
    public LevelPathMode pathMode = LevelPathMode.DualPath;

    [Header("Single Sidewalk Mode Themes")]
    public List<FloorTheme> sidewalkThemes;
    public string forcedThemeId = "";

    [Header("Difficulty Configuration Lists")]
    public List<DifficultyConfig> desktopConfigs; // PC, Console, Web
    public List<DifficultyConfig> mobileConfigs;

    [Header("Active Difficulty")]
    public GameDifficulty selectedDifficulty = GameDifficulty.Medium;
    
    [Header("Resolved Config (Read Only)")]
    public DifficultyConfig currentDifficulty;

    [Header("Editor Debug Settings")]
    [Tooltip("If true, ignores the actual device and uses 'Editor Simulated Platform'. Only works in Unity Editor.")]
    public bool overridePlatformInEditor = false;
    public TargetPlatform editorSimulatedPlatform = TargetPlatform.PC_Console;

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
    public int targetSchoolHeight = 0; // The goal height for the end of the level

    [Header("Safe Zone Settings")]
    public int safeZoneModules = 2; // Number of modules at start without obstacles

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

    [Header("Dual-Layer Settings")]
    public float sidewalkZOffset = 0.5f;
    public float sidewalkYOffset = 1.5f;
    public bool useRelativeSidewalkHeights = false; // Phase 2 gate — keep false until Phase 3 is ready
    [Range(0, 100)] public int roadHeightVarianceChance = 20;
    [Range(0, 100)] public int sidewalkRelHeightVarianceChance = 30;
    public int sidewalkMinRelativeHeight = 0;
    public int sidewalkMaxRelativeHeight = 2;
    public int roadMaxHeight = 2;
    public Transform continuousRoadReference;

    [Header("Road-Specific Modules")]
    public ModuleBuilder roadModule; // Fallback
    public ModuleBuilder[] roadModules; // List of roads to match sidewalk widths
    public ModuleBuilder roadSlopeLeftModule;
    public ModuleBuilder roadSlopeRightModule;

    [Header("Interlayer Slope Modules (Road ↔ Sidewalk Z-transition)")]
    public ModuleBuilder interlayerSlopeLeftModule;
    public ModuleBuilder interlayerSlopeRightModule;



    [Header("=== LEGACY PROCEDURAL (Unused) ===")]
    [SerializeField] private bool generateBuildingsInModules = false;
    public ModuleBuilder moduleTest;
    public Transform groundReference;
    public SpriteRenderer[] background;
    public GameObject home;
    public float homeYOffset = 0.5f;
    public GameObject school;
    public float schoolYOffset = 0.5f;
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

        // Load PathMode from settings (0 = DualPath, 1 = SingleSidewalk)
        // Default to DualPath (0) if not set
        if (PlayerPrefs.HasKey("PathMode"))
        {
            int savedPathMode = PlayerPrefs.GetInt("PathMode", 0);
            pathMode = (LevelPathMode)savedPathMode;
            GameLogger.Log("LEVEL", $"Loaded PathMode from settings: {pathMode}");
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelReplayStarted += OnLevelRestart;
        }

        ResolveDifficultyConfig();
    }

    private void ResolveDifficultyConfig()
    {
        bool isMobile = Application.isMobilePlatform;

#if UNITY_EDITOR
        if (overridePlatformInEditor)
        {
            isMobile = (editorSimulatedPlatform == TargetPlatform.Mobile);
            GameLogger.Log("LEVEL", $"[EDITOR OVERRIDE] Simulating Platform: {editorSimulatedPlatform}");
        }
#endif

        List<DifficultyConfig> targetList = isMobile ? mobileConfigs : desktopConfigs;
        
        if (targetList == null || targetList.Count == 0)
            return;

        foreach(var config in targetList)
        {
            if (config != null && config.difficulty == selectedDifficulty)
            {
                currentDifficulty = config;
                return;
            }
        }
        
        // Fallback to the first available config in the target list if the specific difficulty is not found
        currentDifficulty = targetList[0];
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt("UseRetrySeed", 0) == 1)
        {
            int retrySeed = PlayerPrefs.GetInt("RetrySeed", 0);
            SetManualSeed(retrySeed);
            // Restore the exact path mode so the replayed level is identical.
            if (PlayerPrefs.HasKey("RetryPathMode"))
            {
                pathMode = (LevelPathMode)PlayerPrefs.GetInt("RetryPathMode");
                PlayerPrefs.DeleteKey("RetryPathMode");
            }
            PlayerPrefs.DeleteKey("UseRetrySeed");
            PlayerPrefs.DeleteKey("RetrySeed");
            GameLogger.Log("LEVEL", $"Retry with same seed: {retrySeed}, pathMode: {pathMode}");
        }
        else if (PlayerPrefs.GetInt("UseNewLevelSeed", 0) == 1)
        {
            int newSeed = PlayerPrefs.GetInt("NewLevelSeed", 0);
            if (PlayerPrefs.HasKey("NewLevelPathMode"))
            {
                pathMode = (LevelPathMode)PlayerPrefs.GetInt("NewLevelPathMode");
                PlayerPrefs.DeleteKey("NewLevelPathMode");
            }
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
            if (LevelManager.Instance != null) LevelManager.Instance.LastSeed = currentSeed;
        }
        else
        {
            currentSeed = System.DateTime.Now.Millisecond +
                         System.DateTime.Now.Second * 1000 +
                         System.DateTime.Now.Minute * 60000;
            if (LevelManager.Instance != null) LevelManager.Instance.LastSeed = currentSeed;
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

        // Single seed initialization for the entire level. All random decisions
        // (numberOfModules, themes, blueprint) must draw from this one seed so
        // that Retry with the same seed reproduces the exact same level.
        InitializeSeed();
        numberOfModules = Random.Range(minModules, maxModules);

        if (AnalyticsManager.Instance != null)
        {
            string levelType = pathMode.ToString();
            AnalyticsManager.Instance.TrackLevelStart(currentSeed, levelType);
        }

        if (useModularGeneration)
        {
            if (autoClearStructures) ClearModules();
            ResizeLevelModular();
        }
        else
        {
            GameLogger.LogWarning("LEVEL", "Procedural generation is deprecated. Falling back to modular.");
            if (autoClearStructures) ClearModules();
            ResizeLevelModular();
        }

        if (homeMask != null)
        {
            homeMask.SetActive(false);
            GameLogger.Log("LEVEL", "Home mask deactivated - level generated");
        }

        hasGenerated = true;
        OnLevelGenerationCompleted?.Invoke();

        yield return new WaitForEndOfFrame();

        RecentLevelsList.instance.AddRecentLevel(currentSeed, (int)pathMode);
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

    #region Deprecated Old Generation
    /*
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
    */
    #endregion


    private void ResizeLevelModular()
    {
        GameLogger.Log("LEVEL", "=== MULTI-PASS GENERATION START ===");
        ClearModules();
        ModuleBuilder.ResetStaticThemeTracking();

        // numberOfModules and seed are set by the caller (GenerateLevelWithDelay)
        // before entering here, so the RNG state is already deterministic.
        float roadBaseY = continuousRoadReference != null ? continuousRoadReference.position.y : 0f;

        FloorTheme selectedTheme = null;
        if (pathMode == LevelPathMode.SingleSidewalk && sidewalkThemes != null && sidewalkThemes.Count > 0)
        {
            if (!string.IsNullOrEmpty(forcedThemeId))
            {
                selectedTheme = sidewalkThemes.Find(t => t.themeId == forcedThemeId);
            }
            if (selectedTheme == null)
            {
                selectedTheme = sidewalkThemes[Random.Range(0, sidewalkThemes.Count)];
            }
            GameLogger.Log("LEVEL", $"Selected Theme: {selectedTheme.themeId}");
        }

        // 1. GENERATE BLUEPRINT (PURE MATH)
        LevelBlueprint blueprint = LevelGeneratorEngine.GenerateBlueprint(
            homeModule,
            schoolModule,
            roadModules,
            modules, 
            slopeLeftModule,
            slopeRightModule,
            roadSlopeLeftModule,
            roadSlopeRightModule,
            interlayerSlopeLeftModule != null ? interlayerSlopeLeftModule : slopeLeftModule,
            interlayerSlopeRightModule != null ? interlayerSlopeRightModule : slopeRightModule,
            numberOfModules,
            safeZoneModules,
            roadMaxHeight,
            sidewalkMaxRelativeHeight,
            currentSeed,
            useRelativeSidewalkHeights,
            pathMode,
            selectedTheme
        );

        // 2. RENDER BLUEPRINT (PHYSICAL INSTANTIATION)
        int baselineH = homeModule != null ? homeModule.moduleConfig.minTerrainHeight : 1;
        RenderBlueprint(blueprint, roadBaseY, baselineH);

        // 3. FINALIZE
        levelStartX = blueprint.HouseAnchor.X_Position;
        levelEndX = blueprint.TotalLength;
        newWidth = Mathf.RoundToInt(levelEndX - levelStartX);

        ResizeCamBounds(0f);
        GenerateModularCloudsInRange(levelStartX, levelEndX);
        LayOutBackgroundInRange(levelStartX, levelEndX);
        OnCameraBoundsUpdated?.Invoke();
        
        GameLogger.Log("LEVEL", $"=== MULTI-PASS GENERATION COMPLETE | Width={newWidth} ===");
    }

    private void RenderBlueprint(LevelBlueprint blueprint, float baseY, int baselineH)
    {
        // 1. Render House
        float homeCenter = blueprint.HouseAnchor.X_Position + (blueprint.HouseAnchor.Width / 2f);
        SpawnModularSegment(blueprint.HouseAnchor.Template, homeCenter, baseY, 0f, baselineH, blueprint.HouseAnchor.Label, false, false, false);
        if (home != null) home.transform.position = new Vector3(homeCenter, baseY + homeYOffset, home.transform.position.z);

        // 2. Render Road Track — with EndConnectionAudit for last 5
        int totalRoadSegs = blueprint.RoadTrack != null && blueprint.RoadTrack.Segments != null ? blueprint.RoadTrack.Segments.Count : 0;
        if (blueprint.RoadTrack != null && blueprint.RoadTrack.Segments != null)
        {
            for (int ri = 0; ri < blueprint.RoadTrack.Segments.Count; ri++)
            {
                var rseg = blueprint.RoadTrack.Segments[ri];
                bool isNearEnd = ri >= totalRoadSegs - 5;
                if (isNearEnd)
                {
                    string rCfg = rseg.Template != null && rseg.Template.moduleConfig != null ? rseg.Template.moduleConfig.name : "NULL";
                    float rGbh = rseg.Template != null && rseg.Template.moduleConfig != null ? rseg.Template.moduleConfig.groundBlockHeight : -1f;
                    int rAbsH = baselineH + rseg.Y_Elevation;
                    float rExpectedTopY = rGbh > 0 ? baseY + rAbsH * rGbh : -999f;
                    GameLogger.Log("EndConnectionAudit",
                        $"Type=Road " +
                        $"Idx={ri}/{totalRoadSegs - 1} " +
                        $"X={rseg.X_Position:F1} " +
                        $"Width={rseg.Width:F1} " +
                        $"Y_Elevation={rseg.Y_Elevation} " +
                        $"RoadElevationAtX={rseg.Y_Elevation} " +
                        $"SidewalkRelativeLevel=N/A " +
                        $"TransformYActual={baseY:F3} " +
                        $"ExpectedTopY={rExpectedTopY:F3} " +
                        $"AbsH={rAbsH} " +
                        $"IsSlope={rseg.IsSlope} " +
                        $"Label={rseg.Label} " +
                        $"PrefabName={(rseg.Template != null ? rseg.Template.name : "NULL")} " +
                        $"ConfigName={rCfg} " +
                        $"gbh={rGbh}");
                }
            }
        }
        RenderTrack(blueprint.RoadTrack, baseY, baselineH, 0f);

        // 3. Render Sidewalk Track
        RenderSidewalkTrack(blueprint, baseY, baselineH);

        // 4. Render Diagonal Ramps (Road-Sidewalk connections)
        // Each ramp is anchored at the road's world-Y at its elevation, and placed at Z midway
        // between road (z=0) and sidewalk (z=sidewalkZOffset) to visually bridge both layers.
        foreach (var ramp in blueprint.DiagonalRamps)
        {
            if (ramp.Template == null) continue;

            float rampCenter = ramp.X_Position + (ramp.Width / 2f);

            float gbh = ramp.Template.moduleConfig != null ? ramp.Template.moduleConfig.groundBlockHeight : 3f;

            // Y: the road surface at this elevation.
            // Road transform is always baseY; road top = baseY + (baselineH + roadElev - 1) * gbh.
            // We set the ramp root to that same surface so its visual content sits flush with the road.
            float rampTransformY = baseY + ramp.ConnectorRoadElevation * gbh;

            // Z: midpoint between road (0) and sidewalk (sidewalkZOffset).
            float rampZ = sidewalkZOffset / 2f;

            // rampTransformY already lifts the module to the correct road elevation.
            // We need the slope to be exactly 1 block taller than the road to reach the sidewalk.
            int rampAbsH = baselineH + 1;

            SpawnModularSlope(ramp.Template, rampCenter, rampTransformY, rampZ, rampAbsH, ramp.Label, true);
        }
        // 5. Render School
        float schoolCenter = blueprint.SchoolAnchor.X_Position + (blueprint.SchoolAnchor.Width / 2f);
        SpawnModularSegment(blueprint.SchoolAnchor.Template, schoolCenter, baseY, 0f, baselineH, blueprint.SchoolAnchor.Label, false, false, false, 0);
        if (school != null) school.transform.position = new Vector3(schoolCenter, baseY + schoolYOffset, school.transform.position.z);
    }

    private void RenderSidewalkTrack(LevelBlueprint blueprint, float baseY, int baselineH)
    {
        if (pathMode == LevelPathMode.SingleSidewalk)
        {
            RenderTrack(blueprint.SidewalkTrack, baseY, baselineH, 0f);
        }
        else
        {
            // OLD PATH: the Y_Elevation in Pass3 already accounts for +1 (e.g., roadH_end + 1).
            // We RESTORE sidewalkYOffset here because the user relies on it for the 1.5 unit vertical shift.
            // And we pass absH = 1 inside RenderTrack implicitly by letting seg.Y_Elevation be 0 relative to road.
            RenderTrack(blueprint.SidewalkTrack, baseY + sidewalkYOffset, baselineH, sidewalkZOffset);
        }
    }
    private void RenderTrack(TrackBlueprint track, float baseY, int baselineH, float z)
    {
        if (track == null || track.Segments == null) return;

        string trackType = z == 0f ? "Road" : "Sidewalk";
        int totalSegs = track.Segments.Count;

        foreach (var seg in track.Segments)
        {
            float originalWidth = (seg.Template != null && seg.Template.moduleConfig != null) ? seg.Template.moduleConfig.currentTerrainWidth : seg.Width;
            float center = seg.X_Position + (originalWidth / 2f);
            int absH = baselineH + seg.Y_Elevation;
            float gbh = seg.Template != null && seg.Template.moduleConfig != null ? seg.Template.moduleConfig.groundBlockHeight : 3f;
            bool isSidewalk = z != 0f;
            float segTransformY = baseY + seg.RoadElevationAtX * gbh; // Used primarily by OLD PATH sidewalk

            if (!isSidewalk)
            {
                segTransformY = baseY;
            }

            if (seg.IsSlope) {
                SpawnModularSlope(seg.Template, center, baseY, z, absH, seg.Label, isSidewalk);
            } else {
                // generateContent=true for both Road and Sidewalk — obstacles/vehicles allowed on Road.
                // isSidewalk flag keeps ModuleBuilder identity correct (z!=0 = elevated sidewalk).
                SpawnModularSegment(seg.Template, center, baseY, z, absH, seg.Label, isSidewalk, true, seg.SpawnObstacles, seg.MaxObstacleLevel);
            }
        }
    }

    private ModuleBuilder GetModuleOfWidth(float targetWidth, ModuleBuilder fallback)
    {
        if (modules == null || modules.Length == 0) return fallback;
        
        // Find a module that matches the width closely
        foreach (var m in modules) {
            if (m != null && Mathf.Abs(m.moduleConfig.currentTerrainWidth - targetWidth) < 0.1f)
                return m;
        }
        return fallback;
    }

    private ModuleBuilder GetClosestRoadModule(float targetWidth, ModuleBuilder fallback)
    {
        if (roadModules == null || roadModules.Length == 0) return fallback;

        ModuleBuilder best = null;
        float minDiff = float.MaxValue;

        foreach (var m in roadModules) {
            if (m != null && m.moduleConfig != null) {
                float diff = Mathf.Abs(m.moduleConfig.currentTerrainWidth - targetWidth);
                if (diff < minDiff) {
                    minDiff = diff;
                    best = m;
                }
            }
        }

        return best != null ? best : fallback;
    }






    private void SpawnModularSlope(ModuleBuilder template, float x, float baseY, float z, int h, string name, bool isSidewalk)
    {
        if (template == null) return;
        ModuleBuilder slope = Instantiate(template, new Vector3(x, baseY, z), Quaternion.identity, levelParent);
        slope.name = name;
        slope.isElevatedSidewalk = isSidewalk;
        slope.omitFoundationAtBaseLevel = false;
        // Slopes usually don't have content (buildings/obstacles)
        slope.GenerateModule(h, false, false);
        modulesInScene.Add(slope);
    }

    private void SpawnModularSegment(ModuleBuilder template, float x, float baseY, float z, int h, string name, bool isSidewalk, bool generateContent, bool spawnObstacles = true, int maxObsLevel = 3)
    {
        if (template == null) return;
        ModuleBuilder seg = Instantiate(template, new Vector3(x, baseY, z), Quaternion.identity, levelParent);
        seg.name = name;
        seg.isElevatedSidewalk = isSidewalk;
        seg.omitFoundationAtBaseLevel = false;
        seg.GenerateModule(h, generateContent, spawnObstacles, maxObsLevel, currentDifficulty);
        modulesInScene.Add(seg);
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
        bool usingFallback = tileSprite == null;

        // [BackgroundAudit] — pre-generation snapshot
        GameLogger.Log("BackgroundAudit",
            $"[Setup] backgroundTile={backgroundTile.name} " +
            $"backgroundParent={backgroundParent.name} " +
            $"tileWidth={tileWidth:F3} " +
            $"fallback={usingFallback} " +
            $"tileY={backgroundTile.transform.position.y:F3} " +
            $"tileZ={backgroundTile.transform.position.z:F3} " +
            $"backgroundParentChildCount_before={backgroundParent.childCount}");

#if UNITY_EDITOR
        // [BackgroundAudit] — scan for background SpriteRenderers outside backgroundParent
        SpriteRenderer[] allSRs = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        int orphanCount = 0;
        foreach (var sr in allSRs)
        {
            if (sr == null) continue;
            string srName = sr.gameObject.name.ToLower();
            if ((srName.Contains("background") || srName.Contains("bg")) && !sr.transform.IsChildOf(backgroundParent))
            {
                GameLogger.Log("BackgroundAudit",
                    $"[OrphanBG] name={sr.gameObject.name} " +
                    $"parent={(sr.transform.parent != null ? sr.transform.parent.name : "ROOT")} " +
                    $"worldY={sr.transform.position.y:F3} " +
                    $"worldZ={sr.transform.position.z:F3} " +
                    $"sortingLayer={sr.sortingLayerName} " +
                    $"sortingOrder={sr.sortingOrder}");
                orphanCount++;
            }
        }
        if (orphanCount == 0)
            GameLogger.Log("BackgroundAudit", "[OrphanBG] None found outside backgroundParent.");
#endif

        float rangeWidth = endX - startX;
        int tileCount = Mathf.CeilToInt(rangeWidth / tileWidth);

        GameLogger.Log("LEVEL", $"Generating {tileCount} background tiles from X={startX:F2} to X={endX:F2}");

        for (int i = 0; i < tileCount; i++)
        {
            float xPos = startX + (i * tileWidth) + (tileWidth / 2f);
            Vector3 tilePos = new Vector3(xPos, backgroundTile.transform.position.y, backgroundTile.transform.position.z);
            Instantiate(backgroundTile, tilePos, Quaternion.identity, backgroundParent);
        }

        // [BackgroundAudit] — post-generation snapshot
        GameLogger.Log("BackgroundAudit",
            $"[Done] tileCount={tileCount} " +
            $"backgroundParentChildCount_after={backgroundParent.childCount} " +
            $"firstTileX={startX + tileWidth / 2f:F3} " +
            $"lastTileX={startX + (tileCount - 1) * tileWidth + tileWidth / 2f:F3}");
    }

    #region Deprecated Old Generation
    /*
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
    */
    #endregion

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

    #region Deprecated Procedural Generation Utils (cont)
    /*
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
    */
    #endregion


    /*
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
    */

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

#if UNITY_EDITOR
    [ContextMenu("Debug: Generate Level (Editor)")]
    public void DebugGenerateLevelInEditor()
    {
        ResolveDifficultyConfig();
        InitializeSeed();
        
        if (useModularGeneration)
        {
            if (autoClearStructures) ClearModules();
            ResizeLevelModular();
            GameLogger.Log("LEVEL DEBUG", $"Successfully generated Modular level in Editor with seed {currentSeed}");
        }
        else
        {
            GameLogger.LogWarning("LEVEL DEBUG", "Editor debugging is primarily for Modular Generation. Enable 'useModularGeneration'.");
        }
    }

    [ContextMenu("Debug: Clear Level (Editor)")]
    public void DebugClearLevelInEditor()
    {
        ClearModules();
        GameLogger.Log("LEVEL DEBUG", "Cleared all generated level content from Editor.");
    }
#endif

    public bool HasGeneratedLevel => hasGenerated;
    public int GetLevelWidth() => newWidth;
    public int GetModuleCount() => numberOfModules;
    public int GetCurrentSeed() => currentSeed;
    public LevelPathMode GetPathMode() => pathMode;
    public bool IsUsingManualSeed() => useManualSeed;
    public bool IsUsingModularGeneration() => useModularGeneration;
    public float GetLevelStartX() => levelStartX;
    public float GetLevelEndX() => levelEndX;
}
