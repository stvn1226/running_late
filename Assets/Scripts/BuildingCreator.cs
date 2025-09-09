using UnityEngine;

[System.Serializable]
public class BuildingCreator : MonoBehaviour
{
    [Header("Quick Building Setup")]
    [Tooltip("Click to create a basic building for testing")]
    public bool createBuilding = false;
    
    [Header("Building Dimensions")]
    public float width = 4f;
    public float height = 6f;
    public float wallThickness = 0.3f;
    public float roofThickness = 0.3f;
    
    [Header("Wall Type")]
    [Tooltip("Soft walls = passable but climbable, Hard walls = solid")]
    public bool useSoftWalls = true;
    
    [Header("Colors")]
    public Color wallColor = new Color(0.7f, 0.5f, 0.3f, 1f); // Brown
    public Color roofColor = new Color(0.5f, 0.3f, 0.2f, 1f); // Dark brown
    
    private void OnValidate()
    {
        if (createBuilding)
        {
            createBuilding = false;
            CreateBasicBuilding();
        }
    }
    
    [ContextMenu("Create Basic Building")]
    public void CreateBasicBuilding()
    {
        // Clear existing children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        CreateRoof();
        CreateWalls();
        
        string wallType = useSoftWalls ? "soft (passable + climbable)" : "hard (solid)";
        Debug.Log($"Building created! Roof = solid platform, Walls = {wallType}");
    }
    
    private void CreateRoof()
    {
        GameObject roof = new GameObject("Roof");
        roof.transform.SetParent(transform);
        roof.transform.localPosition = new Vector3(0, height, 0);
        
        // Visual representation with proper sprite
        SpriteRenderer roofRenderer = roof.AddComponent<SpriteRenderer>();
        roofRenderer.sprite = GetBuiltInSprite();
        roofRenderer.color = roofColor;
        roofRenderer.size = new Vector2(width, roofThickness);
        roofRenderer.sortingOrder = 1;
        
        // Physics - solid platform for now (can be enhanced later)
        BoxCollider2D roofCollider = roof.AddComponent<BoxCollider2D>();
        roofCollider.size = new Vector2(width, roofThickness);
        roofCollider.isTrigger = false; // Solid platform
        
        roof.layer = LayerMask.NameToLayer("Default");
        
        Debug.Log("Roof created - solid platform for standing");
    }
    
    private void CreateWalls()
    {
        // Left Wall (passable from right)
        CreateWall("LeftWall", new Vector3(-width/2 + wallThickness/2, height/2, 0), PlatformDirection.LeftOnly);
        
        // Right Wall (passable from left)
        CreateWall("RightWall", new Vector3(width/2 - wallThickness/2, height/2, 0), PlatformDirection.RightOnly);
    }
    
    private void CreateWall(string wallName, Vector3 position, PlatformDirection direction)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(transform);
        wall.transform.localPosition = position;
        
        // Visual representation with proper sprite
        SpriteRenderer wallRenderer = wall.AddComponent<SpriteRenderer>();
        wallRenderer.sprite = GetBuiltInSprite();
        wallRenderer.color = wallColor;
        wallRenderer.size = new Vector2(wallThickness, height);
        wallRenderer.sortingOrder = 0;
        
        if (useSoftWalls)
        {
            CreateSoftWall(wall, direction);
        }
        else
        {
            CreateHardWall(wall, direction);
        }
        
        string wallType = useSoftWalls ? "soft (dynamic + one-way)" : "hard (one-way only)";
        Debug.Log($"{wallName} created - {wallType}, direction: {direction}");
    }
    
    private void CreateSoftWall(GameObject wall, PlatformDirection direction)
    {
        // Main trigger collider for soft wall behavior
        BoxCollider2D triggerCollider = wall.AddComponent<BoxCollider2D>();
        triggerCollider.size = new Vector2(wallThickness, height);
        triggerCollider.isTrigger = true;
        
        // Create a child object for wall running detection
        GameObject wallRunDetector = new GameObject("WallRunDetector");
        wallRunDetector.transform.SetParent(wall.transform);
        wallRunDetector.transform.localPosition = Vector3.zero;
        
        // Non-trigger collider for wall running (slightly smaller to avoid conflicts)
        BoxCollider2D wallRunCollider = wallRunDetector.AddComponent<BoxCollider2D>();
        wallRunCollider.size = new Vector2(wallThickness * 0.8f, height);
        wallRunCollider.isTrigger = false;
        
        // Set layers for wall running detection
        wall.layer = LayerMask.NameToLayer("Default");
        wallRunDetector.layer = LayerMask.NameToLayer("Default");
        
        // Add soft wall behavior with proper direction
        SoftWall softWall = wall.AddComponent<SoftWall>();
        softWall.wallDirection = direction;
        softWall.speedThreshold = 3f;
        softWall.allowWallRunning = true;
    }
    
    private void CreateHardWall(GameObject wall, PlatformDirection direction)
    {
        // Hard walls are just solid colliders for now
        BoxCollider2D wallCollider = wall.AddComponent<BoxCollider2D>();
        wallCollider.size = new Vector2(wallThickness, height);
        wallCollider.isTrigger = false; // Solid wall
        
        wall.layer = LayerMask.NameToLayer("Default");
    }
    
    private Sprite GetBuiltInSprite()
    {
        // Create a simple white square sprite for Unity 6 compatibility
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        sprite.name = "BuildingSprite";
        
        return sprite;
    }
    
    private void OnDrawGizmos()
    {
        // Preview building bounds
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, height/2, 0), 
                           new Vector3(width, height, 0.1f));
        
        // Show roof
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, height, 0),
                           new Vector3(width, roofThickness, 0.1f));
        
        // Show wall type
        Gizmos.color = useSoftWalls ? Color.yellow : Color.red;
        Vector3 leftWallPos = transform.position + new Vector3(-width/2 + wallThickness/2, height/2, 0);
        Vector3 rightWallPos = transform.position + new Vector3(width/2 - wallThickness/2, height/2, 0);
        
        Gizmos.DrawWireCube(leftWallPos, new Vector3(wallThickness, height, 0.1f));
        Gizmos.DrawWireCube(rightWallPos, new Vector3(wallThickness, height, 0.1f));
    }
}