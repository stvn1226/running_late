using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EditorTools
{
    /// <summary>
    /// Editor tool to setup Wide Building theme sets (Set1: Apartments, Set2: Clinic, Set3: Mall).
    /// Creates Set folders, duplicates template prefabs, creates BuildingThemeSet ScriptableObjects,
    /// and assigns them to WideBuildingConfig.asset.
    ///
    /// Run via: Tools > Buildings > Setup Wide Building Sets
    /// </summary>
    public static class WideBuildingBuilder
    {
        private const string BASE_DIR = "Assets/Prefabs/Floors/Soft/New/Wide Building/New_Buildings";
        private const string CONFIG_PATH = "Assets/Prefabs/Floors/Soft/New/Wide Building/WideBuildingConfig.asset";

        private const string SRC_GROUND = BASE_DIR + "/DoubleWide_GroundFloor.prefab";
        private const string SRC_MID    = BASE_DIR + "/DoubleWide_MidFloor.prefab";
        private const string SRC_ROOF   = BASE_DIR + "/DoubleWide_Roof.prefab";

        private static readonly (string setFolder, string themeId, string description)[] SETS = 
        {
            ("Set1", "Set1", "Apartment Set"),
            ("Set2", "Set2", "Hospital / Clinic Set"),
            ("Set3", "Set3", "Mall Set")
        };

        [MenuItem("Tools/Buildings/Setup Wide Building Sets")]
        public static void SetupWideBuildingSets()
        {
            // 1. Verify source prefabs exist
            if (!File.Exists(SRC_GROUND) || !File.Exists(SRC_MID) || !File.Exists(SRC_ROOF))
            {
                Debug.LogError($"[WideBuildingBuilder] Base template prefabs not found in {BASE_DIR}!");
                return;
            }

            List<BuildingThemeSet> createdThemes = new List<BuildingThemeSet>();

            foreach (var (setFolder, themeId, description) in SETS)
            {
                string setDirPath = $"{BASE_DIR}/{setFolder}";
                EnsureDir(setDirPath);

                string dstGround = $"{setDirPath}/DoubleWide_GroundFloor.prefab";
                string dstMid    = $"{setDirPath}/DoubleWide_MidFloor.prefab";
                string dstRoof   = $"{setDirPath}/DoubleWide_Roof.prefab";
                string assetPath = $"{setDirPath}/{setFolder}.asset";

                // Copy prefabs if they don't already exist
                CopyPrefabIfMissing(SRC_GROUND, dstGround);
                CopyPrefabIfMissing(SRC_MID, dstMid);
                CopyPrefabIfMissing(SRC_ROOF, dstRoof);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                // Load prefabs
                GameObject groundGo = AssetDatabase.LoadAssetAtPath<GameObject>(dstGround);
                GameObject midGo    = AssetDatabase.LoadAssetAtPath<GameObject>(dstMid);
                GameObject roofGo   = AssetDatabase.LoadAssetAtPath<GameObject>(dstRoof);

                // Load or create BuildingThemeSet
                BuildingThemeSet themeSet = AssetDatabase.LoadAssetAtPath<BuildingThemeSet>(assetPath);
                if (themeSet == null)
                {
                    themeSet = ScriptableObject.CreateInstance<BuildingThemeSet>();
                    AssetDatabase.CreateAsset(themeSet, assetPath);
                    Debug.Log($"[WideBuildingBuilder] Created {setFolder}.asset ({description})");
                }

                themeSet.themeId = themeId;
                themeSet.minFloorsOverride = 0;
                themeSet.groundPrefabs = new GameObject[] { groundGo };
                themeSet.floorPrefabs  = new GameObject[] { midGo };
                themeSet.roofPrefabs   = new GameObject[] { roofGo };
                themeSet.roofObstaclePrefabs = new GameObject[0];

                EditorUtility.SetDirty(themeSet);
                createdThemes.Add(themeSet);
            }

            // 2. Wire into WideBuildingConfig.asset
            FloorPlan config = AssetDatabase.LoadAssetAtPath<FloorPlan>(CONFIG_PATH);
            if (config != null)
            {
                config.allowedThemes = createdThemes.ToArray();
                EditorUtility.SetDirty(config);
                Debug.Log($"[WideBuildingBuilder] Assigned {createdThemes.Count} themes to {CONFIG_PATH}");
            }
            else
            {
                Debug.LogWarning($"[WideBuildingBuilder] WideBuildingConfig not found at {CONFIG_PATH}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WideBuildingBuilder] ✅ Wide Building sets setup completed successfully!");
        }

        private static void CopyPrefabIfMissing(string src, string dst)
        {
            if (!File.Exists(dst))
            {
                AssetDatabase.CopyAsset(src, dst);
                Debug.Log($"[WideBuildingBuilder] Copied {src} -> {dst}");
            }
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }
}
