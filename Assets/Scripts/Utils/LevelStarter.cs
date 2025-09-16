using UnityEngine;

public class LevelStarter : MonoBehaviour
{
    //public Transform levelParent;
    public Transform groundReference;
    public PolygonCollider2D cameraBounds;
    public StructureGenerator structureGenerator;
    public SpriteRenderer[] background;
    public GameObject home;
    public GameObject school;

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
        groundReference.localScale = new Vector3(newWidth, groundReference.localScale.y, groundReference.localScale.z);
        cameraBounds.points = new Vector2[]
        {
            new Vector2(-newWidth / 2f, -10f),
            new Vector2(-newWidth / 2f, 100f),
            new Vector2(newWidth / 2f, 100f),
            new Vector2(newWidth / 2f, -10f)
        };

        foreach (SpriteRenderer bg in background)
        {
            if (bg != null)
            {
                bg.size = new Vector2(newWidth, bg.size.y);
                //bg.transform.position = new Vector3(0, bg.transform.position.y, bg.transform.position.z);
            }
        }

        PositionEndPoints(newWidth);
    }

    private void PositionEndPoints(int levelWidth)
    {
        home.transform.position = new Vector3(-levelWidth / 2f + 10f, groundReference.position.y + 1.75f, home.transform.position.z);
        school.transform.position = new Vector3(levelWidth / 2f - 10f, groundReference.position.y + 1.75f, school.transform.position.z);
    }

    [ContextMenu("Start Level")]
    public void StartLevelInEditor()
    {
        if (autoClearStructures) structureGenerator.ClearStructuresInEditor();
        ResizeLevel();
        structureGenerator.spawnAreaMin = new Vector2(-newWidth / 2f + 50f, 0);
        structureGenerator.spawnAreaMax = new Vector2(newWidth / 2f - 50f, 0);
        structureGenerator.GenerateStructuresInEditor();
    }
}
