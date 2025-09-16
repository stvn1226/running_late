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


    private void CreateRandomStructure(Vector2 position)
    {
        structureBuilder.ConstructStructure();
    }

    [ContextMenu("Generate Structures")]
    public void GenerateStructuresInEditor()
    {
        CreateRandomStructure(new Vector2(-100, groundReference.position.y));
    }
}
