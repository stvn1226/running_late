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
    public StructureBuilder[] structureBuilders;
    public bool clearExistingStructures = true;

    private List<Vector2> occupiedPositions = new List<Vector2>();
    private List<StructureBuilder> generatedStructures = new List<StructureBuilder>();

    private void GenerateStructures()
    {
        if (clearExistingStructures) ClearExistingStructures();

        for (int i = 0; i < numberOfBuildings; i++)
        {
            CreateRandomStructure();
        }
    }

    private void ClearExistingStructures()
    {
        if (generatedStructures.Count == 0)
        {
            StructureBuilder[] lostStructures = levelParent.gameObject.GetComponentsInChildren<StructureBuilder>();
            foreach (StructureBuilder structure in lostStructures)
            {
                if (structure != null)
                {
                    DestroyImmediate(structure.gameObject);
                }
            }
        }

        foreach (StructureBuilder structure in generatedStructures)
        {
            if (structure != null)
            {
                DestroyImmediate(structure.gameObject);
            }
        }

        generatedStructures.Clear();
        occupiedPositions.Clear();
    }

    private Vector2 FindValidSpawnPosition(float minDistanceBetweenBuildings)
    {
        int maxAttempts = 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            Vector2 candidatePosition = new Vector2(x, groundReference.position.y + .5f);

            bool validPosition = true;
            foreach (Vector2 occupied in occupiedPositions)
            {
                if (Vector2.Distance(candidatePosition, occupied) < minDistanceBetweenBuildings)
                {
                    validPosition = false;
                    break;
                }
            }

            if (validPosition)
            {
                occupiedPositions.Add(candidatePosition);
                return candidatePosition;
            }
        }

        return Vector2.zero;
    }

    private void CreateRandomStructure()
    {
        StructureBuilder structureToBuild = structureBuilders[Random.Range(0, structureBuilders.Length)];
        Vector2 whereToSpawn = FindValidSpawnPosition(structureToBuild.config.minDistanceBetweenBuildings);
        if (whereToSpawn == Vector2.zero) return;
        StructureBuilder newStructure = Instantiate(structureToBuild, levelParent);
        newStructure.transform.position = whereToSpawn;
        newStructure.ConstructStructure();
        generatedStructures.Add(newStructure);
        occupiedPositions.Add(newStructure.transform.position);
    }

    [ContextMenu("Generate Structures")]
    public void GenerateStructuresInEditor()
    {
        GenerateStructures();
    }

    [ContextMenu("Clear Structures")]
    public void ClearStructuresInEditor()
    {
        ClearExistingStructures();
    }

}
