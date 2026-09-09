using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class UnusedAssetsFinderTool : EditorWindow
{
    [System.Serializable]
    public class UnusedAssetItem
    {
        public string assetPath;
        public string assetName;
        public string extension;
        public long fileSizeBytes;
        public bool isSelected = true;
        public Object assetObject;
    }

    private Vector2 scrollPosition;
    private List<UnusedAssetItem> unusedAssets = new List<UnusedAssetItem>();
    private bool isScanning = false;
    private string searchFilter = "";
    private string selectedTypeFilter = "All";
    private readonly string[] typeFilterOptions = new string[] { "All", "Textures & Sprites", "Materials", "Audio", "Shaders", "Prefabs", "Other" };

    // Search root folders
    private bool scanSprites = true;
    private bool scanTiles = true;
    private bool scanArt = true;
    private bool scanMaterials = true;
    private bool scanShaders = false; // Shaders usually shouldn't be touched unless explicit
    private bool scanImported = false;

    private const string QuarantineFolderName = "Assets/_Quarantine_Unused";

    [MenuItem("Tools/Optimization/Unused Asset Finder")]
    public static void ShowWindow()
    {
        GetWindow<UnusedAssetsFinderTool>("Unused Assets Finder");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Unused Asset Finder & Quarantine Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Analyzes scenes, prefabs, ScriptableObjects, and Resources to find unreferenced assets.\n" +
            "Instead of permanently deleting, you can safely move unreferenced assets to a Quarantine folder to test before deletion.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Folders to Analyze for Unused Files:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        scanSprites = EditorGUILayout.ToggleLeft("Assets/Sprites", scanSprites, GUILayout.Width(130));
        scanTiles = EditorGUILayout.ToggleLeft("Assets/tiles", scanTiles, GUILayout.Width(110));
        scanArt = EditorGUILayout.ToggleLeft("Assets/Art", scanArt, GUILayout.Width(110));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        scanMaterials = EditorGUILayout.ToggleLeft("Assets/Materials", scanMaterials, GUILayout.Width(130));
        scanShaders = EditorGUILayout.ToggleLeft("Assets/Shaders", scanShaders, GUILayout.Width(110));
        scanImported = EditorGUILayout.ToggleLeft("Assets/Imported", scanImported, GUILayout.Width(110));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        if (GUILayout.Button(isScanning ? "Scanning..." : "Scan for Unused Assets", GUILayout.Height(32)))
        {
            ScanForUnusedAssets();
        }
        EditorGUILayout.EndVertical();

        if (unusedAssets.Count > 0)
        {
            EditorGUILayout.Space(10);
            DrawResultsHeader();
            DrawResultsList();
            DrawActionButtons();
        }
        else if (!isScanning)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No unused assets found or scan has not been performed yet.", MessageType.None);
        }
    }

    private void DrawResultsHeader()
    {
        EditorGUILayout.BeginHorizontal("box");
        long totalBytes = unusedAssets.Sum(a => a.fileSizeBytes);
        string sizeString = FormatBytes(totalBytes);
        EditorGUILayout.LabelField($"Found {unusedAssets.Count} Unreferenced Assets (~{sizeString})", EditorStyles.boldLabel);

        if (GUILayout.Button("Select All", GUILayout.Width(80)))
        {
            SetAllSelection(true);
        }
        if (GUILayout.Button("Deselect All", GUILayout.Width(90)))
        {
            SetAllSelection(false);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        searchFilter = EditorGUILayout.TextField("Search:", searchFilter);
        selectedTypeFilter = typeFilterOptions[EditorGUILayout.Popup(System.Array.IndexOf(typeFilterOptions, selectedTypeFilter), typeFilterOptions, GUILayout.Width(160))];
        EditorGUILayout.EndHorizontal();
    }

    private void DrawResultsList()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(350));

        var filtered = unusedAssets.Where(PassesFilter).ToList();

        for (int i = 0; i < filtered.Count; i++)
        {
            UnusedAssetItem item = filtered[i];
            EditorGUILayout.BeginHorizontal("box");

            item.isSelected = EditorGUILayout.Toggle(item.isSelected, GUILayout.Width(25));

            if (item.assetObject != null)
            {
                EditorGUILayout.ObjectField(item.assetObject, typeof(Object), false, GUILayout.Width(180));
            }
            else
            {
                EditorGUILayout.LabelField(item.assetName, GUILayout.Width(180));
            }

            EditorGUILayout.LabelField(item.assetPath, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(FormatBytes(item.fileSizeBytes), GUILayout.Width(70));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawActionButtons()
    {
        int selectedCount = unusedAssets.Count(a => a.isSelected);
        long selectedBytes = unusedAssets.Where(a => a.isSelected).Sum(a => a.fileSizeBytes);

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(1.0f, 0.7f, 0.3f);
        if (GUILayout.Button($"Move Selected ({selectedCount}) to Quarantine Folder", GUILayout.Height(38)))
        {
            if (EditorUtility.DisplayDialog("Confirm Move to Quarantine",
                $"Are you sure you want to move {selectedCount} assets ({FormatBytes(selectedBytes)}) to '{QuarantineFolderName}'?\n\n" +
                "This does NOT delete them. You can test your game and restore them anytime.", "Move to Quarantine", "Cancel"))
            {
                MoveSelectedToQuarantine();
            }
        }
        GUI.backgroundColor = Color.white;

        if (Directory.Exists(QuarantineFolderName))
        {
            if (GUILayout.Button("Restore All from Quarantine", GUILayout.Height(38), GUILayout.Width(180)))
            {
                RestoreAllFromQuarantine();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private bool PassesFilter(UnusedAssetItem item)
    {
        if (!string.IsNullOrEmpty(searchFilter))
        {
            if (!item.assetName.ToLower().Contains(searchFilter.ToLower()) &&
                !item.assetPath.ToLower().Contains(searchFilter.ToLower()))
            {
                return false;
            }
        }

        string ext = item.extension.ToLower();
        switch (selectedTypeFilter)
        {
            case "Textures & Sprites":
                return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".psd";
            case "Materials":
                return ext == ".mat";
            case "Audio":
                return ext == ".mp3" || ext == ".wav" || ext == ".ogg";
            case "Shaders":
                return ext == ".shader" || ext == ".shadervariants";
            case "Prefabs":
                return ext == ".prefab";
            case "Other":
                return ext != ".png" && ext != ".jpg" && ext != ".mat" && ext != ".mp3" && ext != ".wav" && ext != ".shader" && ext != ".prefab";
            default:
                return true;
        }
    }

    private void ScanForUnusedAssets()
    {
        isScanning = true;
        unusedAssets.Clear();

        try
        {
            // 1. Collect all root assets (Scenes, Prefabs, Config ScriptableObjects)
            HashSet<string> rootAssetPaths = new HashSet<string>();

            // All scenes in project
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            foreach (var g in sceneGuids) rootAssetPaths.Add(AssetDatabase.GUIDToAssetPath(g));

            // All prefabs in Assets/Prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            foreach (var g in prefabGuids) rootAssetPaths.Add(AssetDatabase.GUIDToAssetPath(g));

            // All ScriptableObjects in Assets/Configs
            string[] configGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Configs" });
            foreach (var g in configGuids) rootAssetPaths.Add(AssetDatabase.GUIDToAssetPath(g));

            // 2. Resolve all recursive dependencies from roots
            EditorUtility.DisplayProgressBar("Scanning Dependencies", "Resolving references...", 0.3f);
            HashSet<string> referencedAssetPaths = new HashSet<string>();

            foreach (var rootPath in rootAssetPaths)
            {
                string[] deps = AssetDatabase.GetDependencies(rootPath, true);
                foreach (var d in deps) referencedAssetPaths.Add(d);
            }

            // Always mark Resources as referenced
            string[] resourceGuids = AssetDatabase.FindAssets("", new[] { "Assets/Resources" });
            foreach (var g in resourceGuids) referencedAssetPaths.Add(AssetDatabase.GUIDToAssetPath(g));

            // 3. Scan selected target folders
            List<string> targetFolders = new List<string>();
            if (scanSprites && AssetDatabase.IsValidFolder("Assets/Sprites")) targetFolders.Add("Assets/Sprites");
            if (scanTiles && AssetDatabase.IsValidFolder("Assets/tiles")) targetFolders.Add("Assets/tiles");
            if (scanArt && AssetDatabase.IsValidFolder("Assets/Art")) targetFolders.Add("Assets/Art");
            if (scanMaterials && AssetDatabase.IsValidFolder("Assets/Materials")) targetFolders.Add("Assets/Materials");
            if (scanShaders && AssetDatabase.IsValidFolder("Assets/Shaders")) targetFolders.Add("Assets/Shaders");
            if (scanImported && AssetDatabase.IsValidFolder("Assets/Imported")) targetFolders.Add("Assets/Imported");

            EditorUtility.DisplayProgressBar("Scanning Target Folders", "Checking unreferenced files...", 0.7f);

            foreach (var folder in targetFolders)
            {
                string[] allGuids = AssetDatabase.FindAssets("", new[] { folder });
                foreach (var guid in allGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    // Skip directories and script files
                    if (AssetDatabase.IsValidFolder(path) || path.EndsWith(".cs") || path.EndsWith(".asmdef") || path.EndsWith(".unity"))
                        continue;

                    // Skip quarantine folder
                    if (path.StartsWith(QuarantineFolderName))
                        continue;

                    if (!referencedAssetPaths.Contains(path))
                    {
                        FileInfo fi = new FileInfo(path);
                        unusedAssets.Add(new UnusedAssetItem
                        {
                            assetPath = path,
                            assetName = Path.GetFileName(path),
                            extension = Path.GetExtension(path),
                            fileSizeBytes = fi.Exists ? fi.Length : 0,
                            isSelected = true,
                            assetObject = AssetDatabase.LoadAssetAtPath<Object>(path)
                        });
                    }
                }
            }

            unusedAssets = unusedAssets.OrderByDescending(a => a.fileSizeBytes).ToList();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            isScanning = false;
        }
    }

    private void MoveSelectedToQuarantine()
    {
        if (!Directory.Exists(QuarantineFolderName))
        {
            AssetDatabase.CreateFolder("Assets", "_Quarantine_Unused");
        }

        int movedCount = 0;
        var toMove = unusedAssets.Where(a => a.isSelected).ToList();

        for (int i = 0; i < toMove.Count; i++)
        {
            var item = toMove[i];
            EditorUtility.DisplayProgressBar("Moving to Quarantine", item.assetName, (float)i / toMove.Count);

            string relativePath = item.assetPath.Substring("Assets/".Length);
            string destinationPath = $"{QuarantineFolderName}/{relativePath}";
            string destinationDir = Path.GetDirectoryName(destinationPath);

            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
                AssetDatabase.Refresh();
            }

            string error = AssetDatabase.MoveAsset(item.assetPath, destinationPath);
            if (string.IsNullOrEmpty(error))
            {
                movedCount++;
            }
            else
            {
                Debug.LogWarning($"[UnusedAssetsFinder] Failed to move {item.assetPath}: {error}");
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[UnusedAssetsFinder] Moved {movedCount} unreferenced assets to '{QuarantineFolderName}'.");
        EditorUtility.DisplayDialog("Quarantine Complete", $"Moved {movedCount} assets to '{QuarantineFolderName}'.\n\nYou can playtest the game. If anything looks missing, use 'Restore All from Quarantine'.", "OK");

        ScanForUnusedAssets();
    }

    private void RestoreAllFromQuarantine()
    {
        if (!Directory.Exists(QuarantineFolderName)) return;

        string[] guids = AssetDatabase.FindAssets("", new[] { QuarantineFolderName });
        int restoredCount = 0;

        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            if (AssetDatabase.IsValidFolder(path)) continue;

            string relative = path.Substring((QuarantineFolderName + "/").Length);
            string destination = $"Assets/{relative}";
            string destDir = Path.GetDirectoryName(destination);

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
                AssetDatabase.Refresh();
            }

            string error = AssetDatabase.MoveAsset(path, destination);
            if (string.IsNullOrEmpty(error)) restoredCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Restore Complete", $"Restored {restoredCount} assets from quarantine back to original locations.", "OK");
        ScanForUnusedAssets();
    }

    private void SetAllSelection(bool selected)
    {
        foreach (var a in unusedAssets) a.isSelected = selected;
    }

    private string FormatBytes(long bytes)
    {
        if (bytes >= 1024 * 1024) return $"{bytes / (1024f * 1024f):F1} MB";
        if (bytes >= 1024) return $"{bytes / 1024f:F1} KB";
        return $"{bytes} B";
    }
}
