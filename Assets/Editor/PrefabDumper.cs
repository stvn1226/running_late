using UnityEngine;
using UnityEditor;

public class PrefabDumper : MonoBehaviour
{
    [MenuItem("Tools/Dump Sidewalk Padding")]
    public static void Dump()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameObject Sidewalk");
        foreach(var guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Slope")) {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var mb = prefab.GetComponent<ModuleBuilder>();
                if (mb != null && mb.padding != null) {
                    Debug.Log("Found padding: " + mb.padding.name + " in " + prefab.name);
                    foreach(Transform child in mb.padding.transform) {
                        Debug.Log("  Child: " + child.name);
                    }
                    break;
                }
            }
        }
    }
}
