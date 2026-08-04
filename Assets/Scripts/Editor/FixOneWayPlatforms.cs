using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FixOneWayPlatforms : EditorWindow
{
    [MenuItem("Tools/Fix OneWayPlatforms (Convert Box to Edge)")]
    public static void ShowWindow()
    {
        GetWindow<FixOneWayPlatforms>("Fix OneWayPlatforms");
    }

    private void OnGUI()
    {
        GUILayout.Label("Converts BoxCollider2D/PolygonCollider2D to EdgeCollider2D on OneWayPlatforms", EditorStyles.boldLabel);

        if (GUILayout.Button("Run Conversion on Prefabs"))
        {
            RunConversion();
        }
    }

    public static void RunConversion()
    {
        string[] searchPaths = new string[] { "Assets/Prefabs/Floors", "Assets/Prefabs/Modules" };
        string[] guids = AssetDatabase.FindAssets("t:Prefab", searchPaths);
        
        int convertedCount = 0;
        int prefabCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool modifiedPrefab = false;

            PlatformEffector2D[] effectors = prefab.GetComponentsInChildren<PlatformEffector2D>(true);
            
            foreach (var effector in effectors)
            {
                if (!effector.useOneWay) continue;
                
                BoxCollider2D box = effector.gameObject.GetComponent<BoxCollider2D>();
                PolygonCollider2D poly = effector.gameObject.GetComponent<PolygonCollider2D>();
                
                if (box == null && poly == null) continue;

                if (effector.surfaceArc <= 90f) continue;
                
                EdgeCollider2D existingEdge = effector.gameObject.GetComponent<EdgeCollider2D>();
                if (existingEdge != null) continue;

                Vector2[] edgePoints = null;
                bool isTrigger = false;
                bool usedByEffector = false;
                PhysicsMaterial2D sharedMaterial = null;
                Vector2 offset = Vector2.zero;

                if (box != null)
                {
                    Vector2 size = box.size;
                    Vector2 boxOffset = box.offset;
                    Vector2 topLeft = new Vector2(boxOffset.x - (size.x / 2f), boxOffset.y + (size.y / 2f));
                    Vector2 topRight = new Vector2(boxOffset.x + (size.x / 2f), boxOffset.y + (size.y / 2f));
                    edgePoints = new Vector2[] { topLeft, topRight };
                    isTrigger = box.isTrigger;
                    usedByEffector = box.usedByEffector;
                    sharedMaterial = box.sharedMaterial;
                    offset = Vector2.zero;
                    DestroyImmediate(box, true);
                }
                else if (poly != null)
                {
                    edgePoints = ExtractTopSurface(poly.points);
                    isTrigger = poly.isTrigger;
                    usedByEffector = poly.usedByEffector;
                    sharedMaterial = poly.sharedMaterial;
                    offset = poly.offset;
                    DestroyImmediate(poly, true);
                }

                EdgeCollider2D edge = effector.gameObject.AddComponent<EdgeCollider2D>();
                edge.points = edgePoints;
                
                if (edgePoints.Length > 1)
                {
                    edge.useAdjacentStartPoint = true;
                    edge.useAdjacentEndPoint = true;
                    Vector2 startDir = (edgePoints[0] - edgePoints[1]).normalized;
                    Vector2 endDir = (edgePoints[edgePoints.Length - 1] - edgePoints[edgePoints.Length - 2]).normalized;
                    edge.adjacentStartPoint = edgePoints[0] + startDir;
                    edge.adjacentEndPoint = edgePoints[edgePoints.Length - 1] + endDir;
                }
                
                edge.isTrigger = isTrigger;
                edge.usedByEffector = usedByEffector;
                edge.sharedMaterial = sharedMaterial;
                edge.offset = offset;

                modifiedPrefab = true;
                convertedCount++;
            }

            if (modifiedPrefab)
            {
                EditorUtility.SetDirty(prefab);
                prefabCount++;
            }
        }

        if (convertedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[FixOneWayPlatforms] Conversion complete! Converted {convertedCount} Colliders across {prefabCount} prefabs.");
        }
        else
        {
            Debug.Log("[FixOneWayPlatforms] No Box/Polygon Colliders found that need conversion.");
        }
    }

    private static Vector2[] ExtractTopSurface(Vector2[] poly)
    {
        int minXIdx = 0;
        int maxXIdx = 0;
        for (int i = 1; i < poly.Length; i++)
        {
            if (poly[i].x < poly[minXIdx].x) minXIdx = i;
            if (poly[i].x > poly[maxXIdx].x) maxXIdx = i;
        }

        List<Vector2> path1 = new List<Vector2>();
        int curr = minXIdx;
        while (true)
        {
            path1.Add(poly[curr]);
            if (curr == maxXIdx) break;
            curr = (curr + 1) % poly.Length;
        }

        List<Vector2> path2 = new List<Vector2>();
        curr = minXIdx;
        while (true)
        {
            path2.Add(poly[curr]);
            if (curr == maxXIdx) break;
            curr = (curr - 1 + poly.Length) % poly.Length;
        }

        float avgY1 = 0; foreach (var p in path1) avgY1 += p.y; avgY1 /= path1.Count;
        float avgY2 = 0; foreach (var p in path2) avgY2 += p.y; avgY2 /= path2.Count;

        List<Vector2> topPath = (avgY1 > avgY2) ? path1 : path2;

        if (topPath[0].x > topPath[topPath.Count - 1].x)
        {
            topPath.Reverse();
        }

        return topPath.ToArray();
    }
}
