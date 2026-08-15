using UnityEngine;
using UnityEngine.Tilemaps;

public class StructureBuilder : MonoBehaviour
{
    [Header("Configuration")]
    public FloorPlan config;

    [Header("Component References (Default)")]
    public GameObject[] groundPrefabs;
    public GameObject[] floorPrefabs;
    public GameObject[] roofPrefabs;

    [Header("Thematic Sets (Optional)")]
    [Tooltip("Si asignas sets aquí, el edificio elegirá uno al azar y usará solo sus piezas. Si está vacío, usará los de arriba.")]
    public BuildingThemeSet[] allowedThemes;

    [Header("Rooftop Obstacles")]
    //public Transform roofObstacleSpawnPoint;
    public GameObject[] roofObstaclePrefabs;
    public GameObject[] powerUps;
    public int obstacleRate = 50; // % chance to spawn rooftop obstacle
    public int powerUpRate = 20; // % chance to spawn rooftop power-up

    [Header("Color Shaders")]
    public Material[] colorMaterials; // Assign different color materials in the Inspector
    
    private BoxCollider2D buildingCollider;

    public void ConstructStructure()
    {
        int floorsCt = 0;
        floorsCt = Random.Range(config.minFloors, config.maxFloors + 1);
        Material chosenColor = colorMaterials.Length > 0 ? colorMaterials[Random.Range(0, colorMaterials.Length)] : null;

        GameLogger.Log("WallRunWorkflow", $"[BUILD START] Constructing building with {floorsCt} floors at {transform.position}");

        BuildingThemeSet activeTheme = null;
        if (allowedThemes != null && allowedThemes.Length > 0)
        {
            activeTheme = allowedThemes[Random.Range(0, allowedThemes.Length)];
        }

        GameObject[] currentGrounds = (activeTheme != null && activeTheme.groundPrefabs != null && activeTheme.groundPrefabs.Length > 0) ? activeTheme.groundPrefabs : groundPrefabs;
        GameObject[] currentFloors = (activeTheme != null && activeTheme.floorPrefabs != null && activeTheme.floorPrefabs.Length > 0) ? activeTheme.floorPrefabs : floorPrefabs;
        GameObject[] currentRoofs = (activeTheme != null && activeTheme.roofPrefabs != null && activeTheme.roofPrefabs.Length > 0) ? activeTheme.roofPrefabs : roofPrefabs;
        GameObject[] currentObstacles = (activeTheme != null && activeTheme.roofObstaclePrefabs != null && activeTheme.roofObstaclePrefabs.Length > 0) ? activeTheme.roofObstaclePrefabs : roofObstaclePrefabs;

        if (currentGrounds.Length > 0)
        {
            GameObject pickedGroundPrefab = currentGrounds[Random.Range(0, currentGrounds.Length)];
            GameObject ground = Instantiate(pickedGroundPrefab, transform.position, Quaternion.identity, transform);
            ground.GetComponentInChildren<SpriteRenderer>().material = chosenColor;
            //ground.GetComponentInChildren<SpriteRenderer>().material = colorMaterials;
            ground.transform.position = new Vector3(transform.position.x, transform.position.y, 10f);
            if (ground.GetComponentInChildren<SpriteRenderer>() != null && ground.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(ground, floorsCt);
            else SortTilemap(ground, floorsCt);
        }
        else return;

        if (currentFloors.Length > 0)
        {
            GameLogger.LogVerbose("BUILDING", $"Constructing {floorsCt} floors for building at {transform.position}");
            for (int i = 0; i < floorsCt - 2; i++) //Construct all floors EXCEPT the ground floor and roof
            {
                Vector2 floorPosition = new Vector2(transform.position.x, transform.position.y + ((i + 1) * config.floorHeight));
                GameObject floor = Instantiate(currentFloors[Random.Range(0, currentFloors.Length)], floorPosition, Quaternion.identity, transform);
                floor.GetComponentInChildren<SpriteRenderer>().material = chosenColor;
                if (floor.GetComponentInChildren<SpriteRenderer>() != null && floor.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(floor, floorsCt);
                else SortTilemap(floor, floorsCt);
            }
        }

        if (currentRoofs.Length > 0)
        {
            GameObject pickedRoofPrefab = currentRoofs[Random.Range(0, currentRoofs.Length)];
            GameObject roof = Instantiate(pickedRoofPrefab, new Vector2(transform.position.x, transform.position.y + (((floorsCt > 0 ? floorsCt : 2) - 1) * config.floorHeight)), Quaternion.identity, transform);
            roof.GetComponentInChildren<SpriteRenderer>().material = chosenColor;
            if (roof.GetComponentInChildren<SpriteRenderer>() != null && roof.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(roof, floorsCt);
            else SortTilemap(roof, floorsCt);
        }
        else return;

        int obstacleChance = Random.Range(0, 100);
        if (obstacleChance < obstacleRate) // % chance to spawn rooftop obstacle
        { 
            if (currentObstacles.Length > 0)
            {
                float varyObstacleSpawnX = Random.Range(-3f, 4f);
                Vector2 obstaclePosition = new Vector2(transform.position.x + varyObstacleSpawnX, transform.position.y + ((floorsCt > 0 ? floorsCt : 2) * config.floorHeight));
                GameObject obstacle = Instantiate(currentObstacles[Random.Range(0, currentObstacles.Length)], obstaclePosition, Quaternion.identity, transform);
            }
        }

        int powerUpChance = Random.Range(0, 100);
        if (powerUpChance < powerUpRate) // % chance to spawn rooftop power-up
        {
            if (powerUps.Length > 0)
            {
                Vector2 powerUpPosition = new Vector2(transform.position.x, transform.position.y + ((floorsCt > 0 ? floorsCt : 2) * config.floorHeight) + 2f);
                GameObject powerUp = Instantiate(powerUps[Random.Range(0, powerUps.Length)], powerUpPosition, Quaternion.identity, transform);
            }
        }
        
        // Setup building detection collider AFTER all floors are created
        SetupBuildingCollider(floorsCt);
    }

    private void SortTilemap(GameObject toUse, int floorsCt)
    {
        TilemapRenderer renderer = toUse.GetComponentInChildren<TilemapRenderer>();
        if (renderer == null) return;

        // sortingOrders in prefabs are negative (-2, -50, etc.).
        // Multiplying by 0.01f keeps Z negative = in front of camera. Correct as-is.
        // Do NOT subtract floorsCt — that was shifting -2 to -9 on 7-floor buildings.
        int baseSortingOrder = renderer.sortingOrder;
        Vector3 currentPos = toUse.transform.position;
        float calculatedZ = (baseSortingOrder * 0.01f) + (transform.position.x * 0.000001f);
        toUse.transform.position = new Vector3(currentPos.x, currentPos.y, calculatedZ);
    }

    private void SortSprite(GameObject toUse, int floorsCt)
    {
        // Use FloorAssetPicker.Base as the authoritative renderer — avoids picking a
        // decoration renderer that happens to be first in the hierarchy.
        FloorAssetPicker picker = toUse.GetComponentInChildren<FloorAssetPicker>();
        SpriteRenderer renderer = (picker != null && picker.Base != null)
            ? picker.Base
            : toUse.GetComponentInChildren<SpriteRenderer>();

        if (renderer == null) return;

        // sortingOrders in prefabs are negative (-2, -50, etc.).
        // Multiplying by 0.01f keeps Z negative = correct depth in front of camera.
        // Do NOT subtract floorsCt — that was shifting -2 to -9 on a 7-floor building,
        // conflicting with cable sorting order -10 and causing z-fighting artifacts.
        int baseSortingOrder = renderer.sortingOrder;
        Vector3 currentPos = toUse.transform.position;
        float calculatedZ = (baseSortingOrder * 0.01f) + (transform.position.x * 0.000001f);
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
        
        // Width calculation: Find the WIDEST internal child collider (e.g., MWall)
        float maxChildWidth = 0f;
        float bestOffsetX = 0f;
        bool foundStructureBase = false;
        GameObject widestChildObj = null;

        BoxCollider2D[] allChildColliders = GetComponentsInChildren<BoxCollider2D>();
        foreach (var col in allChildColliders)
        {
            // Skip the parent's own buildingCollider
            if (col.gameObject == gameObject) continue;
            
            // MATH FIX: Factor in the scale of the child and parent
            float worldWidth = col.size.x * col.transform.lossyScale.x;
            float parentLocalWidth = transform.lossyScale.x != 0 ? worldWidth / transform.lossyScale.x : worldWidth;

            if (parentLocalWidth > maxChildWidth)
            {
                maxChildWidth = parentLocalWidth;
                
                // Calculate horizontal center relative to this StructureBuilder parent
                Vector3 worldCenter = col.transform.TransformPoint(col.offset);
                Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
                bestOffsetX = localCenter.x;
                
                foundStructureBase = true;
                widestChildObj = col.gameObject;
            }
        }

        float buildingWidth;
        float offsetX;

        if (foundStructureBase)
        {
            buildingWidth = maxChildWidth;
            offsetX = bestOffsetX;
        }
        else
        {
            buildingWidth = config.floorWidth;
            offsetX = 0f;
        }
        
        float buildingHeight = floorsCt * config.floorHeight;
        
        buildingCollider.size = new Vector2(buildingWidth, buildingHeight);
        buildingCollider.offset = new Vector2(offsetX, buildingHeight * 0.5f);
        
        // SYNC: Push dimensions ONLY to the main wall trigger (the one we built the width from)
        // or to the parent itself if it has one. This avoids touching LWall/RWall.
        if (foundStructureBase && widestChildObj != null)
        {
            BuildingWallsTrigger childTrigger = widestChildObj.GetComponent<BuildingWallsTrigger>();
            if (childTrigger != null)
            {
                childTrigger.UpdateColliderDimensions(buildingCollider.size, buildingCollider.offset);
            }
        }

        BuildingWallsTrigger parentTrigger = GetComponent<BuildingWallsTrigger>();
        if (parentTrigger != null)
        {
            parentTrigger.UpdateColliderDimensions(buildingCollider.size, buildingCollider.offset);
        }
        
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
