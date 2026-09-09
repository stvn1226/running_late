using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEditor.U2D;
using System.IO;
using System.Collections.Generic;

public class SpriteAtlasAutoCreator : EditorWindow
{
    [System.Serializable]
    public class AtlasGroupConfig
    {
        public string atlasName;
        public string folderPath;
        public bool isSelected = true;
        public int maxTextureSize = 2048;
    }

    private Vector2 scrollPosition;
    private string atlasOutputPath = "Assets/Atlases";
    private bool enableRotation = false; // Kept false for 2D sprites to prevent visual distortion
    private bool enableTightPacking = true;
    private int padding = 4;

    private List<AtlasGroupConfig> predefinedGroups = new List<AtlasGroupConfig>();

    [MenuItem("Tools/Optimization/Sprite Atlas Auto Creator")]
    public static void ShowWindow()
    {
        GetWindow<SpriteAtlasAutoCreator>("Atlas Creator");
    }

    [MenuItem("Tools/Optimization/Generate Running Late Atlases")]
    public static void QuickGenerateAll()
    {
        SpriteAtlasAutoCreator window = CreateInstance<SpriteAtlasAutoCreator>();
        window.InitializeDefaultGroups();
        window.CreateSelectedAtlases();
        DestroyImmediate(window);
    }

    private void OnEnable()
    {
        InitializeDefaultGroups();
    }

    private void InitializeDefaultGroups()
    {
        predefinedGroups.Clear();

        AddGroupIfFolderExists("Atlas_Environment", "Assets/Sprites/Environment");
        AddGroupIfFolderExists("Atlas_Obstacles", "Assets/Sprites/Obstacles");
        AddGroupIfFolderExists("Atlas_Pickups", "Assets/Sprites/Pickups");
        AddGroupIfFolderExists("Atlas_Player", "Assets/Sprites/Player");
        AddGroupIfFolderExists("Atlas_PlayerExpressions", "Assets/Sprites/PlayerExpressions");
        AddGroupIfFolderExists("Atlas_KenneyInput", "Assets/Sprites/Kenney_Input");
        AddGroupIfFolderExists("Atlas_Tiles", "Assets/tiles");
    }

    private void AddGroupIfFolderExists(string atlasName, string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            predefinedGroups.Add(new AtlasGroupConfig
            {
                atlasName = atlasName,
                folderPath = assetPath,
                isSelected = true,
                maxTextureSize = 2048
            });
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Running Late - Sprite Atlas Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Packs loose sprite textures into Sprite Atlases to drastically reduce Draw Calls and batch sprites during rendering.", MessageType.Info);
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
        atlasOutputPath = EditorGUILayout.TextField("Atlas Output Path:", atlasOutputPath);
        enableTightPacking = EditorGUILayout.Toggle("Tight Packing:", enableTightPacking);
        enableRotation = EditorGUILayout.Toggle("Allow Rotation:", enableRotation);
        padding = EditorGUILayout.IntSlider("Padding (px):", padding, 2, 8);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Atlas Target Folders", EditorStyles.boldLabel);

        if (GUILayout.Button("Reset / Rescan Target Folders"))
        {
            InitializeDefaultGroups();
        }

        EditorGUILayout.Space(5);

        for (int i = 0; i < predefinedGroups.Count; i++)
        {
            AtlasGroupConfig group = predefinedGroups[i];
            EditorGUILayout.BeginHorizontal("box");
            group.isSelected = EditorGUILayout.Toggle(group.isSelected, GUILayout.Width(25));
            EditorGUILayout.LabelField(group.atlasName, EditorStyles.boldLabel, GUILayout.Width(180));
            EditorGUILayout.LabelField(group.folderPath);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
        if (GUILayout.Button("Create Selected Sprite Atlases", GUILayout.Height(36)))
        {
            CreateSelectedAtlases();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    public void CreateSelectedAtlases()
    {
        if (!Directory.Exists(atlasOutputPath))
        {
            Directory.CreateDirectory(atlasOutputPath);
        }

        int createdCount = 0;

        foreach (var group in predefinedGroups)
        {
            if (!group.isSelected) continue;

            Object folderAsset = AssetDatabase.LoadAssetAtPath<Object>(group.folderPath);
            if (folderAsset == null)
            {
                Debug.LogWarning($"[AtlasCreator] Skipping {group.atlasName}: Folder '{group.folderPath}' could not be loaded.");
                continue;
            }

            string atlasFilePath = $"{atlasOutputPath}/{group.atlasName}.spriteatlas";

            if (File.Exists(atlasFilePath))
            {
                AssetDatabase.DeleteAsset(atlasFilePath);
            }

            SpriteAtlas atlas = new SpriteAtlas();

            // Packing settings (disable rotation for clean 2D sprites)
            SpriteAtlasPackingSettings packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = enableRotation,
                enableTightPacking = enableTightPacking,
                enableAlphaDilation = false,
                padding = padding
            };
            atlas.SetPackingSettings(packingSettings);

            // Texture settings
            SpriteAtlasTextureSettings textureSettings = new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            };
            atlas.SetTextureSettings(textureSettings);

            // Default platform settings
            TextureImporterPlatformSettings defaultSettings = new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform",
                maxTextureSize = group.maxTextureSize,
                textureCompression = TextureImporterCompression.Compressed,
                crunchedCompression = false,
                compressionQuality = 50,
                overridden = true
            };
            atlas.SetPlatformSettings(defaultSettings);

            // Add source folder
            atlas.Add(new Object[] { folderAsset });

            // Save asset
            AssetDatabase.CreateAsset(atlas, atlasFilePath);
            createdCount++;
            Debug.Log($"[AtlasCreator] Successfully generated: {atlasFilePath} from {group.folderPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AtlasCreator] Completed. Generated {createdCount} Sprite Atlases in '{atlasOutputPath}'.");
        EditorUtility.DisplayDialog("Sprite Atlas Generation", $"Successfully created {createdCount} Sprite Atlases in '{atlasOutputPath}'.", "OK");
    }
}