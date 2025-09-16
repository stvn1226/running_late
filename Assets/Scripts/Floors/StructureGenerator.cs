using System.Collections.Generic;
using UnityEngine;

public class StructureGenerator : MonoBehaviour
{
    //StructureGenerator will be used to lay out the buildings on the map, while StructureBuilder will be used to create each building

    [Header("Environment Data")]
    public Transform levelParent;
    public Transform groundReference;

    [Header("Generation Parameters")]
    public int numberOfBuildings = 10;
    public Vector2 spawnAreaMin = new Vector2(-50, 0);
    public Vector2 spawnAreaMax = new Vector2(50, 0);
    public StructureBuilder structureBuilder;

    private List<Vector2> occupiedPositions = new List<Vector2>();


    private void CreateRandomStructure()
    {
        for (int i = 0; i < numberOfBuildings; i++)
        {
            float xpos = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            structureBuilder.transform.position = new Vector2(xpos, groundReference.position.y + 1.75f);
            StructureBuilder newStructure = Instantiate(structureBuilder);
            newStructure.ConstructStructure();
        }
    }

    [ContextMenu("Generate Structures")]
    public void GenerateStructuresInEditor()
    {
        CreateRandomStructure();
    }
}
