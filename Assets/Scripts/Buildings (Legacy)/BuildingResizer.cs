using UnityEngine;

public class BuildingResizer : MonoBehaviour
{
    private Transform roofTransform;
    private Transform leftWallTransform;
    private Transform rightWallTransform;
    private Transform floorTransform;
    private Transform fillTransform;

    private void Awake()
    {
        CacheChildReferences();
    }

    private void CacheChildReferences()
    {
        roofTransform = transform.Find("Roof");
        leftWallTransform = transform.Find("LWall");
        rightWallTransform = transform.Find("RWall");
        floorTransform = transform.Find("Floor");
        fillTransform = transform.Find("Fill");

        if (roofTransform == null || leftWallTransform == null || rightWallTransform == null || floorTransform == null || fillTransform == null)
        {
            GameLogger.LogError("BUILDING", $"Building {gameObject.name} is missing required child objects (Roof, LWall, RWall, Floor, Fill)");
        }
    }

    public void ResizeBuilding(int width, int height, float groundY)
    {
        if (roofTransform == null) CacheChildReferences();

        float halfWidth = width * 0.5f;

        // Position floor at ground level
        if (floorTransform != null)
        {
            floorTransform.position = new Vector3(transform.position.x, groundY, transform.position.z);
            floorTransform.localScale = new Vector3(width, 1, 1);
        }

        // Position roof at the top
        if (roofTransform != null)
        {
            roofTransform.position = new Vector3(transform.position.x, groundY + height, transform.position.z);
            roofTransform.localScale = new Vector3(width + 1f, 1, 1);
        }

        // Position and resize left wall
        if (leftWallTransform != null)
        {
            leftWallTransform.position = new Vector3(transform.position.x - halfWidth, groundY + (height * 0.5f) + 0.5f, transform.position.z);
            leftWallTransform.localScale = new Vector3(height, 1, 1);
        }

        // Position and resize right wall  
        if (rightWallTransform != null)
        {
            rightWallTransform.position = new Vector3(transform.position.x + halfWidth, groundY + (height * 0.5f) + 0.5f, transform.position.z);
            rightWallTransform.localScale = new Vector3(height, 1, 1);
        }

        if (fillTransform != null)
        {
            fillTransform.position = new Vector3(transform.position.x, groundY + (height * 0.5f), transform.position.z);
            fillTransform.localScale = new Vector3(width, height, 1);
        }

    }

}
