using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LevelStarter : MonoBehaviour
{
    [Header("Level Generation Settings")]
    public bool generateOnStart = true;
    public bool generateOnSceneReload = true;
    public float generationDelay = 0.1f;

    [Header("Generation Mode")]
    [SerializeField] private bool useModularGeneration = true;
    [SerializeField] private bool generateBuildingsInModules = false;

    [Header("Seed Settings")]
    public bool useManualSeed = false;
    [SerializeField] private int manualSeed = 12345;
    [SerializeField] private int currentSeed;
    [Space]
    [SerializeField] private bool showSeedInConsole = true;

    [Header("References")]
    public Transform levelParent;
    public Transform groundReference;
    public PolygonCollider2D cameraBounds;
    public StructureGenerator structureGenerator;
    public LevelManager levelManager;
    public SpawnManager spawnManager;
    
    [Header("Endpoints")]
    public GameObject home;
    public GameObject school;
    
    [Header("Visual Elements")]
    public SpriteRenderer[] background;
    public SpriteRenderer[] clouds;
    public GameObject backgroundTile;
    public Transform backgroundParent;
    public Transform cloudParent;
    
    [Header("Level Size")]
    public int minLevelWidth = 1000;
    public int maxLevelWidth = 2000;
    public bool autoClearStructures = true;
    
    [Header("Modular Level Settings")]
    public ModuleBuilder[] modules;
    public ModuleBuilder moduleTest;
    public ModuleBuilder homeModule;
    public ModuleBuilder schoolModule;
    
    [Header("Building Generation Settings")]
    [Range(10, 50)]
    public int buildingsPerUnit = 20;
    [Range(0.5f, 2.0f)]
    public float buildingDensityMultiplier = 1.0f;
    
    private int newWidth;
    private int numberOfModules;
    private List<ModuleBuilder> modulesInScene = new List<ModuleBuilder>();
    private List<SpriteRenderer> cloudsInScene = new List<SpriteRenderer>();
    private GameObject cloudCache;
    private bool hasGenerated = false;
    
    private DateTime generationStartTime;
    private DateTime playerSpawnStartTime;
    
    public static LevelStarter Instance { get; private set; }
    
    public System.Action OnLevelGenerationStarted;
    public System.Action OnLevelGenerationCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
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
        if (generateOnStart && !hasGenerated)
        {
            StartCoroutine(GenerateLevelWithDelay());
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
            Debug.Log($"Level Seed: {currentSeed}");
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
        
        yield return new WaitForSeconds(generationDelay);
        
        InitializeSeed();
        
        if (autoClearStructures) ClearModules();
        
        if (useModularGeneration)
        {
            ResizeLevelModular();
        }
        else
        {
            ResizeLevel();
        }
        
        hasGenerated = true;
        OnLevelGenerationCompleted?.Invoke();
        
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);
        
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
        StartCoroutine(GenerateLevelWithDelay());
    }
    
    public void ForceRegenerate()
    {
        hasGenerated = false;
        StartCoroutine(GenerateLevelWithDelay());
    }

    private void GenerateBuildings()
    {
        if (structureGenerator == null) return;
        
        int totalBuildings = Mathf.RoundToInt((newWidth / buildingsPerUnit) * buildingDensityMultiplier);
        
        structureGenerator.numberOfBuildings = totalBuildings;
        structureGenerator.spawnAreaMin = new Vector2(-newWidth / 2f + 50f, 0);
        structureGenerator.spawnAreaMax = new Vector2(newWidth / 2f - 50f, 0);
        
        if (Application.isPlaying)
        {
            structureGenerator.GenerateStructuresInEditor();
        }
        else
        {
            structureGenerator.GenerateStructuresInEditor();
        }
        
        if (showSeedInConsole)
        {
            Debug.Log($"Generated {totalBuildings} buildings across {newWidth} units (density: {buildingDensityMultiplier:F1}x)");
        }
    }

    private void ResizeLevel()
    {
        newWidth = UnityEngine.Random.Range(minLevelWidth, maxLevelWidth);
        if (groundReference != null)
        {
            groundReference.GetComponent<SpriteRenderer>().size = new Vector2(newWidth, groundReference.GetComponent<SpriteRenderer>().size.y);
            groundReference.GetComponent<BoxCollider2D>().size = new Vector2(newWidth, groundReference.GetComponent<BoxCollider2D>().size.y);
        }
        
        ResizeCamBounds(-8.75f);

        foreach (SpriteRenderer bg in background)
        {
            if (bg != null)
            {
                bg.size = new Vector2(newWidth, bg.size.y);
            }
        }

        GenerateProceduralClouds();
        PositionEndPoints(newWidth);
        GenerateBuildings();
    }

    private void ResizeLevelModular()
    {
        newWidth = 0;
        numberOfModules = UnityEngine.Random.Range(6, 12);
        List<ModuleBuilder> modulesToSpawn = new List<ModuleBuilder>();
        modulesToSpawn.Add(homeModule);
        
        for (int i = 0; i < numberOfModules; i++)
        {
            moduleTest = modules[UnityEngine.Random.Range(0, modules.Length)];
            modulesToSpawn.Add(moduleTest);
        }
        modulesToSpawn.Add(schoolModule);

        foreach (ModuleBuilder module in modulesToSpawn)
        {
            newWidth += module.moduleConfig.currentTerrainWidth;
        }
        
        Vector2 moduleSpawner = new Vector2(-newWidth / 2f, 0f);
        
        moduleSpawner.x = newWidth / 2f - schoolModule.moduleConfig.currentTerrainWidth / 2f;
        ModuleBuilder spawnedSchool = Instantiate(schoolModule, moduleSpawner, Quaternion.identity, levelParent);
        spawnedSchool.name = "SchoolModule_Final";
        modulesInScene.Add(spawnedSchool);
        
        moduleSpawner.x = -newWidth / 2f + homeModule.moduleConfig.currentTerrainWidth;
        foreach (ModuleBuilder module in modulesToSpawn)
        {
            if (module != homeModule && module != schoolModule)
            {
                moduleSpawner.x += module.moduleConfig.currentTerrainWidth / 2f;
                ModuleBuilder newModule = Instantiate(module, moduleSpawner, Quaternion.identity, levelParent);
                newModule.GenerateModule();
                modulesInScene.Add(newModule);
                moduleSpawner.x += module.moduleConfig.currentTerrainWidth / 2f;
            }
        }
        
        moduleSpawner.x = -newWidth / 2f + homeModule.moduleConfig.currentTerrainWidth / 2f;
        ModuleBuilder spawnedHome = Instantiate(homeModule, moduleSpawner, Quaternion.identity, levelParent);
        spawnedHome.name = "HomeModule_Final";
        modulesInScene.Add(spawnedHome);

        ResizeCamBounds(0f);
        LayOutBackground();
        
        if (generateBuildingsInModules)
        {
            GenerateBuildings();
        }
        
        if (showSeedInConsole)
        {
            Debug.Log($"Modular Level: Width={newWidth}, Modules={numberOfModules}, Seed={currentSeed}");
        }
    }

    private void GenerateProceduralClouds()
    {
        if (clouds == null || cloudParent == null) return;
        
        ClearClouds();

        for (int i = 0; i < newWidth / 2; i++)
        {
            Vector2 cloudPosition = new Vector2(
                UnityEngine.Random.Range(-newWidth / 2f, newWidth / 2f), 
                UnityEngine.Random.Range(8f, 100f)
            );
            int cloudIndex = UnityEngine.Random.Range(0, clouds.Length);
            
            cloudCache = Instantiate(clouds[cloudIndex].gameObject, cloudPosition, Quaternion.identity, cloudParent);
            cloudsInScene.Add(cloudCache.GetComponent<SpriteRenderer>());
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
        List<Transform> modulesToClear = new List<Transform>();

        for (int i = 0; i < levelParent.childCount; i++)
        {
            modulesToClear.Add(levelParent.GetChild(i));
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
        cameraBounds.points = new Vector2[]
        {
            new Vector2(-newWidth / 2f, yMin),
            new Vector2(-newWidth / 2f, 100f),
            new Vector2(newWidth / 2f, 100f),
            new Vector2(newWidth / 2f, yMin)
        };
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
}
