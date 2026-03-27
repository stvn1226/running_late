using UnityEngine;
using UnityEngine.Tilemaps;

public class StructureBuilder : MonoBehaviour
{
    [Header("Configuration")]
    public FloorPlan config;

    [Header("Component References")]
    public GameObject[] groundPrefabs;
    public GameObject[] floorPrefabs;
    public GameObject[] roofPrefabs;

    [Header("Rooftop Obstacles")]
    //public Transform roofObstacleSpawnPoint;
    public GameObject[] roofObstaclePrefabs;
    public GameObject[] powerUps;
    public int obstacleRate = 50; // % chance to spawn rooftop obstacle
    public int powerUpRate = 20; // % chance to spawn rooftop power-up
    
    private BoxCollider2D buildingCollider;

    public void ConstructStructure()
    {
        int floorsCt = 0;
        floorsCt = Random.Range(config.minFloors, config.maxFloors + 1);

        GameLogger.Log("WallRunWorkflow", $"[BUILD START] Constructing building with {floorsCt} floors at {transform.position}");

        if (groundPrefabs.Length > 0)
        {
            GameObject pickedGroundPrefab = groundPrefabs[Random.Range(0, groundPrefabs.Length)];
            GameObject ground = Instantiate(pickedGroundPrefab, transform.position, Quaternion.identity, transform);
            ground.transform.position = new Vector3(transform.position.x, transform.position.y, 10f);
            if (ground.GetComponentInChildren<SpriteRenderer>() != null && ground.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(ground, floorsCt);
            else SortTilemap(ground, floorsCt);
        }
        else return;

        if (floorPrefabs.Length > 0)
        {
            GameLogger.LogVerbose("BUILDING", $"Constructing {floorsCt} floors for building at {transform.position}");
            for (int i = 0; i < floorsCt - 2; i++)
            {
                Vector2 floorPosition = new Vector2(transform.position.x, transform.position.y + ((i + 1) * config.floorHeight));
                GameObject floor = Instantiate(floorPrefabs[Random.Range(0, floorPrefabs.Length)], floorPosition, Quaternion.identity, transform);
                if (floor.GetComponentInChildren<SpriteRenderer>() != null && floor.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(floor, floorsCt);
                else SortTilemap(floor, floorsCt);
            }
        }

        if (roofPrefabs.Length > 0)
        {
            GameObject pickedRoofPrefab = roofPrefabs[Random.Range(0, roofPrefabs.Length)];
            GameObject roof = Instantiate(pickedRoofPrefab, new Vector2(transform.position.x, transform.position.y + (((floorsCt > 0 ? floorsCt : 2) - 1) * config.floorHeight)), Quaternion.identity, transform);
            if (roof.GetComponentInChildren<SpriteRenderer>() != null && roof.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(roof, floorsCt);
            else SortTilemap(roof, floorsCt);
        }
        else return;

        int obstacleChance = Random.Range(0, 100);
        if (obstacleChance < obstacleRate) // % chance to spawn rooftop obstacle
        { 
            if (roofObstaclePrefabs.Length > 0)
            {
                float varyObstacleSpawnX = Random.Range(-3f, 4f);
                Vector2 obstaclePosition = new Vector2(transform.position.x + varyObstacleSpawnX, transform.position.y + ((floorsCt > 0 ? floorsCt : 2) * config.floorHeight));
                GameObject obstacle = Instantiate(roofObstaclePrefabs[Random.Range(0, roofObstaclePrefabs.Length)], obstaclePosition, Quaternion.identity, transform);
            }
        }

        int powerUpChance = Random.Range(0, 100);
        if (powerUpChance < powerUpRate) // % chance to spawn rooftop power-up
        {
            if (powerUps.Length > 0)
            {
                Vector2 powerUpPosition = new Vector2(transform.position.x, transform.position.y + ((floorsCt > 0 ? floorsCt : 2) * config.floorHeight) + 1f);
                GameObject powerUp = Instantiate(powerUps[Random.Range(0, powerUps.Length)], powerUpPosition, Quaternion.identity, transform);
            }
        }
        
        // Setup building detection collider AFTER all floors are created
        SetupBuildingCollider(floorsCt);
    }

    private void SortTilemap(GameObject toUse, int floorsCt)
    {
        TilemapRenderer renderer = toUse.GetComponentInChildren<TilemapRenderer>();
        int originalSortingOrder = renderer.sortingOrder;
        renderer.sortingOrder -= floorsCt;
        
        int newSortingOrder = renderer.sortingOrder;
        Vector3 currentPos = toUse.transform.position;
        float calculatedZ = newSortingOrder * 0.01f + (transform.position.x * 0.000001f);
        toUse.transform.position = new Vector3(currentPos.x, currentPos.y, calculatedZ);
    }

    private void SortSprite(GameObject toUse, int floorsCt)
    {
        SpriteRenderer renderer = toUse.GetComponentInChildren<SpriteRenderer>();
        int originalSortingOrder = renderer.sortingOrder;
        renderer.sortingOrder -= floorsCt;
        
        int newSortingOrder = renderer.sortingOrder;
        Vector3 currentPos = toUse.transform.position;
        float calculatedZ = newSortingOrder * 0.01f + (transform.position.x * 0.000001f);
        toUse.transform.position = new Vector3(currentPos.x, currentPos.y, calculatedZ);
    }
    
    private void SetupBuildingCollider(int floorsCt)
    {
        buildingCollider = gameObject.GetComponent<BoxCollider2D>();
        if (buildingCollider == null)
        {
            buildingCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        buildingCollider.isTrigger = true;
        gameObject.tag = "Building";
        
        // Automatic width calculation from instantiated parts
        Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);
        bool foundRenderer = false;
        
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // Only include floors, ground, and roof components (ignore obstacles/powerups)
            if (r.gameObject == gameObject) continue;
            
            if (!foundRenderer)
            {
                combinedBounds = r.bounds;
                foundRenderer = true;
            }
            else
            {
                combinedBounds.Encapsulate(r.bounds);
            }
        }

        float buildingWidth = foundRenderer ? combinedBounds.size.x : config.floorWidth;
        float buildingHeight = floorsCt * config.floorHeight;
        float offsetX = foundRenderer ? combinedBounds.center.x - transform.position.x : 0f;
        
        buildingCollider.size = new Vector2(buildingWidth, buildingHeight);
        buildingCollider.offset = new Vector2(offsetX, buildingHeight * 0.5f);
        
        // Calculate building Z from first floor (ground floor)
        // Ground floor is instantiated FIRST so it's always child 0
        Transform firstFloor = transform.childCount > 0 ? transform.GetChild(0) : null;
        float buildingZ = firstFloor != null ? firstFloor.position.z : transform.position.z;
        
        // Get Sorting Order from Sprite/Tilemap for Lane Logic
        int sortingOrder = 0;
        if (firstFloor != null)
        {
            SpriteRenderer sr = firstFloor.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sortingOrder = sr.sortingOrder;
            else
            {
                TilemapRenderer tr = firstFloor.GetComponentInChildren<TilemapRenderer>();
                if (tr != null) sortingOrder = tr.sortingOrder;
            }
        }

        // Add BuildingDetector component to store Z and name
        BuildingDetector detector = gameObject.GetComponent<BuildingDetector>();
        if (detector == null)
        {
            detector = gameObject.AddComponent<BuildingDetector>();
        }
        detector.Initialize(buildingZ, gameObject.name, sortingOrder);
    }
}
