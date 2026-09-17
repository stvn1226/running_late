using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class UpdateLeaderboardRowPrefab
{
    private const string ROW_PREFAB_PATH = "Assets/Prefabs/UI/LeaderboardRowUI.prefab";

    static UpdateLeaderboardRowPrefab()
    {
        EditorApplication.delayCall += EnsureRowPrefabElements;
    }

    [MenuItem("Tools/Leaderboard/Ensure Row Prefab Elements (Seed + Play)")]
    public static void EnsureRowPrefabElements()
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == ROW_PREFAB_PATH)
        {
            ApplyToRoot(stage.prefabContentsRoot);
            EditorSceneManager.MarkSceneDirty(stage.scene);
            Debug.Log("[Leaderboard] Successfully updated open PrefabStage for LeaderboardRowUI.");
            return;
        }

        if (!System.IO.File.Exists(ROW_PREFAB_PATH))
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(ROW_PREFAB_PATH);
        if (root == null) return;

        bool modified = ApplyToRoot(root);

        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(root, ROW_PREFAB_PATH);
            Debug.Log($"[Leaderboard] Successfully updated {ROW_PREFAB_PATH} with SeedLabel and PlayButton directly in prefab.");
        }

        PrefabUtility.UnloadPrefabContents(root);
    }

    private static bool ApplyToRoot(GameObject root)
    {
        if (root == null) return false;

        bool modified = false;
        LeaderboardRowUI rowUI = root.GetComponent<LeaderboardRowUI>();

        Transform seedT = root.transform.Find("SeedLabel");
        Transform playT = root.transform.Find("PlayButton");

        // 1. Ensure SeedLabel
        TextMeshProUGUI seedLabel = seedT != null ? seedT.GetComponent<TextMeshProUGUI>() : null;
        if (seedLabel == null)
        {
            Transform timeT = root.transform.Find("TimeLabel");
            TextMeshProUGUI timeTMP = timeT != null ? timeT.GetComponent<TextMeshProUGUI>() : null;

            GameObject seedGO = new GameObject("SeedLabel");
            seedGO.layer = root.layer;
            seedGO.transform.SetParent(root.transform, false);

            RectTransform rt = seedGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(90f, 48f);

            seedLabel = seedGO.AddComponent<TextMeshProUGUI>();
            if (timeTMP != null && timeTMP.font != null)
            {
                seedLabel.font = timeTMP.font;
                seedLabel.fontSharedMaterial = timeTMP.fontSharedMaterial;
                seedLabel.fontSize = timeTMP.fontSize;
            }
            else
            {
                seedLabel.fontSize = 20f;
            }

            seedLabel.text = "123456";
            seedLabel.alignment = TextAlignmentOptions.MidlineRight;
            seedLabel.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            seedLabel.raycastTarget = false;

            modified = true;
        }

        // 2. Ensure PlayButton
        Button playBtn = playT != null ? playT.GetComponent<Button>() : null;
        if (playBtn == null)
        {
            Sprite playIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Art/UI/Icons/Ico3.png");

            GameObject btnGO = new GameObject("PlayButton");
            btnGO.layer = root.layer;
            btnGO.transform.SetParent(root.transform, false);

            RectTransform btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(40f, 40f);

            LayoutElement le = btnGO.AddComponent<LayoutElement>();
            le.minWidth = 40f;
            le.preferredWidth = 40f;
            le.minHeight = 40f;
            le.preferredHeight = 40f;

            Image img = btnGO.AddComponent<Image>();
            if (playIcon != null)
            {
                img.sprite = playIcon;
                img.color = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.2f, 0.75f, 0.35f, 1f);
            }
            img.raycastTarget = true;

            playBtn = btnGO.AddComponent<Button>();
            playBtn.targetGraphic = img;

            modified = true;
        }

        // 3. Link references in LeaderboardRowUI serialized object
        if (rowUI != null)
        {
            SerializedObject so = new SerializedObject(rowUI);
            SerializedProperty spSeed = so.FindProperty("seedLabel");
            SerializedProperty spPlay = so.FindProperty("playButton");

            if (spSeed != null && (spSeed.objectReferenceValue == null || spSeed.objectReferenceValue != seedLabel))
            {
                spSeed.objectReferenceValue = seedLabel;
                modified = true;
            }

            if (spPlay != null && (spPlay.objectReferenceValue == null || spPlay.objectReferenceValue != playBtn))
            {
                spPlay.objectReferenceValue = playBtn;
                modified = true;
            }

            if (modified)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        return modified;
    }
}
