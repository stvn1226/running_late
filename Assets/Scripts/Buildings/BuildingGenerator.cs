using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BuildingGenerator : MonoBehaviour
{
    [Header("Building Generation Settings")]
    public GameObject buildingPrefab;
    public Transform levelParent;
    public Transform groundReference;

    [Header("Building Type")]
    public string buildingTag = ""; // Tag for buildings created by this generator

    [Header("Generation Parameters")]
    public int numberOfBuildings = 10;
    public Vector2 spawnAreaMin = new Vector2(-50, 0);
    public Vector2 spawnAreaMax = new Vector2(50, 0);

    [Header("Building Size Constraints")]
    public int minWidth = 3;
    public int maxWidth = 12;
    public int minHeight = 5;
    public int maxHeight = 15;

    [Header("Building Spacing")]
    public float minDistanceBetweenBuildings = 2f;

    private List<Vector2> occupiedPositions = new List<Vector2>();

    public void GenerateBuildings()
    {
        ClearExistingBuildings();
        occupiedPositions.Clear();

        for (int i = 0; i < numberOfBuildings; i++)
        {
            Vector2 spawnPosition = FindValidSpawnPosition();
            if (spawnPosition != Vector2.zero)
            {
                CreateRandomBuilding(spawnPosition);
            }
        }
    }

    private Vector2 FindValidSpawnPosition()
    {
        int maxAttempts = 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            Vector2 candidatePosition = new Vector2(x, groundReference.position.y);

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

    private void CreateRandomBuilding(Vector2 position)
    {
        GameObject building = Instantiate(buildingPrefab, levelParent);
        building.transform.position = position;

        int randomWidth = Random.Range(minWidth, maxWidth + 1);
        int randomHeight = Random.Range(minHeight, maxHeight + 1);

        BuildingResizer resizer = building.GetComponent<BuildingResizer>();
        if (resizer == null)
        {
            resizer = building.AddComponent<BuildingResizer>();
        }

        resizer.ResizeBuilding(randomWidth, randomHeight, groundReference.position.y);
    }

    private void ClearExistingBuildings()
    {
        GameObject[] existingBuildings = GameObject.FindGameObjectsWithTag(buildingTag);

        for (int i = existingBuildings.Length - 1; i >= 0; i--)
        {
            // Make sure the building is actually a child of our level parent
            if (existingBuildings[i].transform.IsChildOf(levelParent))
            {
                if (Application.isPlaying)
                {
                    Destroy(existingBuildings[i]);
                }
                else
                {
                    DestroyImmediate(existingBuildings[i]);
                }
            }
        }
    }

    [ContextMenu("Generate Buildings")]
    public void GenerateBuildingsInEditor()
    {
        GenerateBuildings();
    }
}
