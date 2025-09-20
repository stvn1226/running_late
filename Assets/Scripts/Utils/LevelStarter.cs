using System.Collections.Generic;
using UnityEngine;

public class LevelStarter : MonoBehaviour
{
    //public Transform levelParent;
    public Transform groundReference;
    public PolygonCollider2D cameraBounds;
    public StructureGenerator structureGenerator;
    public LevelManager levelManager;
    public SpriteRenderer[] background;
    public SpriteRenderer[] clouds;
    public GameObject home;
    public GameObject school;

    List<SpriteRenderer> cloudsInScene = new List<SpriteRenderer>();
    private GameObject cloudCache;
    public Transform cloudParent;

    public int minLevelWidth = 1000;
    public int maxLevelWidth = 2000;

    public bool autoClearStructures = true;

    private int newWidth;

    private void Awake()
    {
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
        structureGenerator.numberOfBuildings = (int) (newWidth / 20);
        structureGenerator.GenerateStructuresInEditor();
        levelManager.ResetLevel();
    }
}
