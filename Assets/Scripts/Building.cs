using UnityEngine;

public class Building : MonoBehaviour
{
    [Header("Building Configuration")]
    public float buildingWidth = 4f;
    public float buildingHeight = 6f;
    public float roofThickness = 0.5f;
    public float wallThickness = 0.2f;
    
    [Header("Wall Physics")]
    [Tooltip("Should walls allow wall running?")]
    public bool allowWallRunning = true;
    
    [Tooltip("Should walls be passable from certain directions?")]
    public bool allowWallPassing = false;
    
    [Header("Visual")]
    public Sprite wallSprite;
    public Sprite roofSprite;
    public Color buildingColor = Color.white;
    
    // Building parts
    private GameObject leftWall;
    private GameObject rightWall;
    private GameObject roof;
    private GameObject floor;
    
    private void Start()
    {
        SetupBuilding();
    }
    
    private void SetupBuilding()
    {
        // Clear existing building parts
        ClearBuildingParts();
        
        // Create building structure
        CreateRoof();
        CreateWalls();
        
        // Setup colliders based on configuration
        SetupBuildingColliders();
    }
    
    private void ClearBuildingParts()
    {
        // Remove existing building parts
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
    
    private void CreateRoof()
    {
        roof = new GameObject("Roof");
        roof.transform.SetParent(transform);
        roof.transform.localPosition = new Vector3(0, buildingHeight, 0);
        
        // Add sprite renderer
        SpriteRenderer roofRenderer = roof.AddComponent<SpriteRenderer>();
        roofRenderer.sprite = roofSprite;
        roofRenderer.color = buildingColor;
        roofRenderer.size = new Vector2(buildingWidth, roofThickness);
        
        // Add box collider for solid roof (player can stand on)
        BoxCollider2D roofCollider = roof.AddComponent<BoxCollider2D>();
        roofCollider.size = new Vector2(buildingWidth, roofThickness);
        
        // Roof should be on Ground layer for solid collision
        roof.layer = LayerMask.NameToLayer("Default"); // Will use Ground layer when we set it up
    }
    
    private void CreateWalls()
    {
        // Left Wall
        leftWall = new GameObject("LeftWall");
        leftWall.transform.SetParent(transform);
        leftWall.transform.localPosition = new Vector3(-buildingWidth/2 + wallThickness/2, buildingHeight/2, 0);
        
        SpriteRenderer leftRenderer = leftWall.AddComponent<SpriteRenderer>();
        leftRenderer.sprite = wallSprite;
        leftRenderer.color = buildingColor;
        leftRenderer.size = new Vector2(wallThickness, buildingHeight);
        
        BoxCollider2D leftCollider = leftWall.AddComponent<BoxCollider2D>();
        leftCollider.size = new Vector2(wallThickness, buildingHeight);
        
        // Right Wall
        rightWall = new GameObject("RightWall");
        rightWall.transform.SetParent(transform);
        rightWall.transform.localPosition = new Vector3(buildingWidth/2 - wallThickness/2, buildingHeight/2, 0);
        
        SpriteRenderer rightRenderer = rightWall.AddComponent<SpriteRenderer>();
        rightRenderer.sprite = wallSprite;
        rightRenderer.color = buildingColor;
        rightRenderer.size = new Vector2(wallThickness, buildingHeight);
        
        BoxCollider2D rightCollider = rightWall.AddComponent<BoxCollider2D>();
        rightCollider.size = new Vector2(wallThickness, buildingHeight);
        
        // Set wall layers based on configuration
        if (allowWallRunning)
        {
            // Walls should be on Wall layer for wall running
            leftWall.layer = LayerMask.NameToLayer("Default"); // Will use Wall layer when we set it up
            rightWall.layer = LayerMask.NameToLayer("Default");
        }
        
        if (allowWallPassing)
        {
            // Make walls triggers so player can pass through from certain directions
            leftCollider.isTrigger = true;
            rightCollider.isTrigger = true;
            
            // Add wall passing logic components
            leftWall.AddComponent<SoftWall>();
            rightWall.AddComponent<SoftWall>();
        }
    }
    
    private void SetupBuildingColliders()
    {
        // Configure colliders based on building settings
        // This will be expanded when we set up proper layers
    }
    
    private void OnValidate()
    {
        // Update building when values change in inspector
        if (Application.isPlaying)
        {
            SetupBuilding();
        }
    }
    
    // Helper method to get building bounds
    public Bounds GetBuildingBounds()
    {
        return new Bounds(transform.position + new Vector3(0, buildingHeight/2, 0), 
                         new Vector3(buildingWidth, buildingHeight, 1));
    }
}