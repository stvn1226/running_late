using System.Collections.Generic;
using UnityEngine;

public class LevelStarter : MonoBehaviour
{
    //public string myObjRefID;

    public Transform levelParent;
    public Transform groundReference;
    public PolygonCollider2D cameraBounds;
    public StructureGenerator structureGenerator;
    public LevelManager levelManager;
    public SpriteRenderer[] background;
    public SpriteRenderer[] clouds;
    public GameObject home;
    public GameObject school;
    public SpawnManager spawnManager;

    List<SpriteRenderer> cloudsInScene = new List<SpriteRenderer>();
    private GameObject cloudCache;
    public Transform cloudParent;

    public int minLevelWidth = 1000;
    public int maxLevelWidth = 2000;

    public bool autoClearStructures = true;

    private int newWidth;

    [Header("Modular Level Settings")]
    public ModuleBuilder[] modules;
    public ModuleBuilder moduleTest;
    public ModuleBuilder homeModule;
    public ModuleBuilder schoolModule;
    private int numberOfModules;
    private List<ModuleBuilder> modulesInScene = new List<ModuleBuilder>();
    public GameObject backgroundTile;

    private void Awake()
    {
        //myObjRefID = this.gameObject.GetHashCode().ToString();
        //structureGenerator.ClearStructuresInEditor();
    }

    private void ResizeLevel()
    {
        newWidth = Random.Range(minLevelWidth, maxLevelWidth);
        groundReference.GetComponent<SpriteRenderer>().size = new Vector2(newWidth, groundReference.GetComponent<SpriteRenderer>().size.y);
        groundReference.GetComponent<BoxCollider2D>().size = new Vector2(newWidth, groundReference.GetComponent<BoxCollider2D>().size.y);
        cameraBounds.points = new Vector2[]
        {
            new Vector2(-newWidth / 2f, -8.75f),
            new Vector2(-newWidth / 2f, 100f),
            new Vector2(newWidth / 2f, 100f),
            new Vector2(newWidth / 2f, -8.75f)
        };

        foreach (SpriteRenderer bg in background)
        {
            if (bg != null)
            {
                bg.size = new Vector2(newWidth, bg.size.y);
                //bg.transform.position = new Vector3(0, bg.transform.position.y, bg.transform.position.z);
            }
        }

        if (cloudsInScene.Count == 0)
        {
            SpriteRenderer[] lostClouds = cloudParent.gameObject.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer c in lostClouds)
            {
                if (c != null)
                {
                    DestroyImmediate(c.gameObject);
                }
            }
        }
        else
        {
            foreach (SpriteRenderer cloud in cloudsInScene)
            {
                if (cloud != null) DestroyImmediate(cloud.gameObject);
            }
        }

        for (int i = 0; i < newWidth / 2; i++)
        {
            cloudCache = Instantiate(clouds[Random.Range(0, clouds.Length)].gameObject, new Vector2(Random.Range(-newWidth / 2f, newWidth / 2f), Random.Range(8f, 100f)), Quaternion.identity, cloudParent);
            cloudsInScene.Add(cloudCache.GetComponent<SpriteRenderer>());
        }

        PositionEndPoints(newWidth);
    }

    private void ResizeLevelModular()
    {
        newWidth = 0;
        numberOfModules = Random.Range(minLevelWidth, maxLevelWidth); //change the minLevelWidth and maxLevelWidth into the NUMBER OF MODULES to fill the level with, rather than width in units.
        List<ModuleBuilder> modulesToSpawn = new List<ModuleBuilder>();
        modulesToSpawn.Add(homeModule);
        for (int i = 0; i < numberOfModules; i++)
        {
            moduleTest = modules[Random.Range(0, modules.Length)];
            modulesToSpawn.Add(moduleTest);
        }
        modulesToSpawn.Add(schoolModule);

        foreach (ModuleBuilder module in modulesToSpawn)
        {
            newWidth += module.moduleConfig.currentTerrainWidth;
        }
        Vector2 moduleSpawner = new Vector2(-newWidth / 2f, 0f);
        moduleSpawner.x += homeModule.moduleConfig.currentTerrainWidth / 2f;
        ModuleBuilder spawnedHome = Instantiate(homeModule, moduleSpawner, Quaternion.identity, levelParent);
        modulesInScene.Add(spawnedHome);
        moduleSpawner.x += homeModule.moduleConfig.currentTerrainWidth / 2f;
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
        moduleSpawner.x += schoolModule.moduleConfig.currentTerrainWidth / 2f;
        ModuleBuilder spawnedSchool = Instantiate(schoolModule, moduleSpawner, Quaternion.identity, levelParent);
        modulesInScene.Add(spawnedSchool);
        moduleSpawner.x += schoolModule.moduleConfig.currentTerrainWidth / 2f;

        ResizeCamBounds();
        LayOutBackground();
    }

    private void ClearModules()
    {
        List<Transform> modulesToClear = new List<Transform>();

        for (int i = 0; i < levelParent.childCount; i++)
        {
            modulesToClear.Add(levelParent.GetChild(i));

        }

        foreach (Transform module in modulesToClear)
        {
            DestroyImmediate(module.gameObject);
        }
    }

    private void ResizeCamBounds()
    {
        cameraBounds.points = new Vector2[]
        {
            new Vector2(-newWidth / 2f, 0f),
            new Vector2(-newWidth / 2f, 100f),
            new Vector2(newWidth / 2f, 100f),
            new Vector2(newWidth / 2f, 0f)
        };
    }

    private void LayOutBackground()
    {
        Vector2 bgPos = new Vector2(-(newWidth / 2) - 5, 0f);
        for (int i = 0; i <= (newWidth / 10); i++)
        {
            bgPos.x += 10;
            Instantiate(backgroundTile, bgPos, Quaternion.identity, levelParent);
        }

    }

    private void PositionEndPoints(int levelWidth)
    {
        home.transform.position = new Vector3(-levelWidth / 2f + 10f, groundReference.position.y + .5f, home.transform.position.z);
        school.transform.position = new Vector3(levelWidth / 2f - 10f, groundReference.position.y + .5f, school.transform.position.z);
    }

    [ContextMenu("Start Level")]
    public void StartLevelInEditor()
    {
        if (autoClearStructures) structureGenerator.ClearStructuresInEditor();
        ResizeLevel();
        structureGenerator.spawnAreaMin = new Vector2(-newWidth / 2f + 50f, 0);
        structureGenerator.spawnAreaMax = new Vector2(newWidth / 2f - 50f, 0);
        structureGenerator.numberOfBuildings = (int)(newWidth / 20);
        structureGenerator.GenerateStructuresInEditor();
        levelManager.ReplayLevel();
    }

    [ContextMenu("Start Level Electric Boogaloo")]
    public void StartLevelInEditorTwain()
    {
        if (autoClearStructures) structureGenerator.ClearStructuresInEditor();
        ResizeLevel();
        structureGenerator.spawnAreaMin = new Vector2(-newWidth / 2f + 50f, 0);
        structureGenerator.spawnAreaMax = new Vector2(newWidth / 2f - 50f, 0);
        structureGenerator.numberOfBuildings = (int)(newWidth / 20);
        structureGenerator.GenerateStructuresInEditor();
        spawnManager.InitializeAndSpawnPlayer();
    }

    [ContextMenu("New Start Level")]
    public void StartLevelModular()
    {
        if (autoClearStructures) ClearModules();
        ResizeLevelModular();
    }
}
