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

    /// <summary>El BuildingThemeSet elegido al azar en la última llamada a ConstructStructure(). Null si usa los prefabs default.</summary>
    public BuildingThemeSet ActiveTheme { get; private set; }

    public void ConstructStructure(string excludeThemeId = null)
    {
        int floorsCt = 0;
        floorsCt = Random.Range(config.minFloors, config.maxFloors + 1);
        Material chosenColor = colorMaterials.Length > 0 ? colorMaterials[Random.Range(0, colorMaterials.Length)] : null;

        GameLogger.Log("WallRunWorkflow", $"[BUILD START] Constructing building with {floorsCt} floors at {transform.position}");

        BuildingThemeSet activeTheme = null;
        if (allowedThemes != null && allowedThemes.Length > 0)
        {
            System.Collections.Generic.List<BuildingThemeSet> validThemes = new System.Collections.Generic.List<BuildingThemeSet>();
            for (int i = 0; i < allowedThemes.Length; i++)
            {
                if (allowedThemes[i] == null) continue;
                if (!string.IsNullOrEmpty(excludeThemeId) && allowedThemes[i].themeId == excludeThemeId)
                {
                    continue;
                }
                validThemes.Add(allowedThemes[i]);
            }

            // Fallback si todos fueron filtrados
            if (validThemes.Count == 0)
            {
                for (int i = 0; i < allowedThemes.Length; i++)
                {
                    if (allowedThemes[i] != null) validThemes.Add(allowedThemes[i]);
                }
            }

            if (validThemes.Count > 0)
            {
                activeTheme = validThemes[Random.Range(0, validThemes.Count)];
            }
        }
        ActiveTheme = activeTheme;

        GameObject[] currentGrounds = (activeTheme != null && activeTheme.groundPrefabs != null && activeTheme.groundPrefabs.Length > 0) ? activeTheme.groundPrefabs : groundPrefabs;
        GameObject[] currentFloors = (activeTheme != null && activeTheme.floorPrefabs != null && activeTheme.floorPrefabs.Length > 0) ? activeTheme.floorPrefabs : floorPrefabs;
        GameObject[] currentRoofs = (activeTheme != null && activeTheme.roofPrefabs != null && activeTheme.roofPrefabs.Length > 0) ? activeTheme.roofPrefabs : roofPrefabs;
        GameObject[] currentObstacles = (activeTheme != null && activeTheme.roofObstaclePrefabs != null && activeTheme.roofObstaclePrefabs.Length > 0) ? activeTheme.roofObstaclePrefabs : roofObstaclePrefabs;

        float floorH = config != null ? config.floorHeight : 3f;

        if (currentGrounds.Length > 0)
        {
            GameObject pickedGroundPrefab = currentGrounds[Random.Range(0, currentGrounds.Length)];
            GameObject ground = Instantiate(pickedGroundPrefab, transform.position, Quaternion.identity, transform);
            FloorAssetPicker groundPicker = ground.GetComponentInChildren<FloorAssetPicker>();
            if (groundPicker != null) groundPicker.RandomizeBaseSprite();
            ground.GetComponentInChildren<SpriteRenderer>().material = chosenColor;
            ground.transform.position = new Vector3(transform.position.x, transform.position.y, 10f);
            if (ground.GetComponentInChildren<SpriteRenderer>() != null && ground.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(ground, 0);
            else SortTilemap(ground, 0);
        }
        else return;

        if (currentFloors.Length > 0)
        {
            GameLogger.LogVerbose("BUILDING", $"Constructing {floorsCt} floors for building at {transform.position}");
            for (int i = 0; i < floorsCt - 2; i++) //Construct all floors EXCEPT the ground floor and roof
            {
                int floorIndex = i + 1; // Floor 2 is index 1, Floor 3 is index 2, etc.
                Vector2 floorPosition = new Vector2(transform.position.x, transform.position.y + (floorIndex * floorH));
                GameObject floor = Instantiate(currentFloors[Random.Range(0, currentFloors.Length)], floorPosition, Quaternion.identity, transform);
                FloorAssetPicker floorPicker = floor.GetComponentInChildren<FloorAssetPicker>();
                if (floorPicker != null) floorPicker.RandomizeBaseSprite();
                floor.GetComponentInChildren<SpriteRenderer>().material = chosenColor;
                if (floor.GetComponentInChildren<SpriteRenderer>() != null && floor.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(floor, floorIndex);
                else SortTilemap(floor, floorIndex);
            }
        }

        if (currentRoofs.Length > 0)
        {
            int roofIndex = (floorsCt > 0 ? floorsCt : 2) - 1;
            GameObject pickedRoofPrefab = currentRoofs[Random.Range(0, currentRoofs.Length)];
            GameObject roof = Instantiate(pickedRoofPrefab, new Vector2(transform.position.x, transform.position.y + (roofIndex * floorH)), Quaternion.identity, transform);
            FloorAssetPicker roofPicker = roof.GetComponentInChildren<FloorAssetPicker>();
            if (roofPicker != null) roofPicker.RandomizeBaseSprite();
            roof.GetComponentInChildren<SpriteRenderer>().material = chosenColor;
            if (roof.GetComponentInChildren<SpriteRenderer>() != null && roof.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(roof, roofIndex);
            else SortTilemap(roof, roofIndex);
        }
        else return;

        int obstacleChance = Random.Range(0, 100);
        if (obstacleChance < obstacleRate) // % chance to spawn rooftop obstacle
        { 
            if (currentObstacles.Length > 0)
            {
                float varyObstacleSpawnX = Random.Range(-3f, 4f);
                Vector2 obstaclePosition = new Vector2(transform.position.x + varyObstacleSpawnX, transform.position.y + (((floorsCt > 0 ? floorsCt : 2) - 1) * floorH));
                GameObject obstacle = Instantiate(currentObstacles[Random.Range(0, currentObstacles.Length)], obstaclePosition, Quaternion.identity, transform);
            }
        }

        int powerUpChance = Random.Range(0, 100);
        if (powerUpChance < powerUpRate) // % chance to spawn rooftop power-up
        { 
            if (powerUps.Length > 0)
            {
                Vector2 powerUpPosition = new Vector2(transform.position.x, transform.position.y + (((floorsCt > 0 ? floorsCt : 2) - 1) * floorH) + 2f);
                GameObject powerUp = Instantiate(powerUps[Random.Range(0, powerUps.Length)], powerUpPosition, Quaternion.identity, transform);
            }
        }
        
        // Setup building detection collider AFTER all floors are created
        SetupBuildingCollider(floorsCt);
    }

    private void SortTilemap(GameObject toUse, int floorIndex)
    {
        TilemapRenderer[] renderers = toUse.GetComponentsInChildren<TilemapRenderer>();
        foreach (var r in renderers)
        {
            if (r != null) r.sortingOrder -= floorIndex;
        }

        TilemapRenderer mainRenderer = toUse.GetComponentInChildren<TilemapRenderer>();
        if (mainRenderer != null)
        {
            int baseSortingOrder = mainRenderer.sortingOrder;
            Vector3 currentPos = toUse.transform.position;
            float calculatedZ = (baseSortingOrder * 0.01f) + (transform.position.x * 0.000001f);
            toUse.transform.position = new Vector3(currentPos.x, currentPos.y, calculatedZ);
        }
    }

    private void SortSprite(GameObject toUse, int floorIndex)
    {
        SpriteRenderer[] renderers = toUse.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            if (r != null) r.sortingOrder -= floorIndex;
        }

        FloorAssetPicker picker = toUse.GetComponentInChildren<FloorAssetPicker>();
        SpriteRenderer baseRenderer = (picker != null && picker.Base != null)
            ? picker.Base
            : toUse.GetComponentInChildren<SpriteRenderer>();

        if (baseRenderer != null)
        {
            int baseSortingOrder = baseRenderer.sortingOrder;
            Vector3 currentPos = toUse.transform.position;
            float calculatedZ = (baseSortingOrder * 0.01f) + (transform.position.x * 0.000001f);
            toUse.transform.position = new Vector3(currentPos.x, currentPos.y, calculatedZ);
        }
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
