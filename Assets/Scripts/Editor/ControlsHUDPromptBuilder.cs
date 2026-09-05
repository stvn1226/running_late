using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UI.GamePanels;

namespace EditorTools
{
    public static class ControlsHUDPromptBuilder
    {
        private const string PREFAB_PATH = "Assets/Prefabs/UI/ControlsHUDPrompt.prefab";

        [InitializeOnLoadMethod]
        private static void AutoEnsurePrefab()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(PREFAB_PATH))
                {
                    CreateOrUpdatePrefab();
                }
            };
        }

        [MenuItem("Tools/Controls HUD/Create Controls HUD Prefab")]
        public static void CreateOrUpdatePrefab()
        {
            // 1. Ensure target directory exists
            string dir = Path.GetDirectoryName(PREFAB_PATH);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0) uiLayer = 5;

            // 2. Create Root Object
            GameObject root = new GameObject("ControlsHUDPrompt");
            root.layer = uiLayer;
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-30f, -30f);
            rootRect.sizeDelta = new Vector2(260f, 220f);

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

            VerticalLayoutGroup vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperRight;
            vlg.spacing = 8f;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = root.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ControlsHUDPrompt promptManager = root.AddComponent<ControlsHUDPrompt>();
            promptManager.LoadKenneyDefaultPrompts();

            // 3. Find standard TMP Font
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Roboto-Bold SDF.asset");
            if (font == null)
            {
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }

            // 4. Create Row Items
            string[] actionNames = new string[] { "Jump", "Slide", "Kick", "Drop Down", "Pause" };

            foreach (string action in actionNames)
            {
                GameObject rowObj = CreateRow(root.transform, action, font, uiLayer);
                rowObj.name = $"Row_{action}";
            }

            // 5. Link rows to manager
            promptManager.RefreshPrompts();

            // 6. Save Prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PREFAB_PATH, InteractionMode.AutomatedAction);
            GameObject.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ControlsHUDPromptBuilder] Successfully created Controls HUD Prefab at: {PREFAB_PATH}");
            Selection.activeObject = prefab;
        }

        [MenuItem("Tools/Controls HUD/Add Controls HUD to Active Canvas")]
        public static void AddToActiveCanvas()
        {
            Canvas canvas = null;
            if (Selection.activeGameObject != null)
            {
                canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                canvas = Object.FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                Debug.LogError("[ControlsHUDPromptBuilder] No Canvas found in active scene. Please open a scene with a UI Canvas.");
                return;
            }

            // Remove any previous or duplicate instances so only one exists cleanly
            ControlsHUDPrompt[] existingList = canvas.GetComponentsInChildren<ControlsHUDPrompt>(true);
            if (existingList != null && existingList.Length > 0)
            {
                for (int i = 0; i < existingList.Length; i++)
                {
                    Undo.DestroyObjectImmediate(existingList[i].gameObject);
                }
            }

            // Load prefab or create if missing
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                CreateOrUpdatePrefab();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            }

            if (prefab == null)
            {
                Debug.LogError("[ControlsHUDPromptBuilder] Could not find or create prefab.");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            instance.name = "ControlsHUDPrompt";

            RectTransform rt = instance.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-30f, -30f);

            Undo.RegisterCreatedObjectUndo(instance, "Add Controls HUD Prompt");
            Selection.activeGameObject = instance;
            Debug.Log("[ControlsHUDPromptBuilder] ControlsHUDPrompt added to Canvas!");
        }

        private static GameObject CreateRow(Transform parent, string actionName, TMP_FontAsset font, int uiLayer)
        {
            GameObject row = new GameObject($"Row_{actionName}");
            row.layer = uiLayer;
            row.transform.SetParent(parent, false);

            RectTransform rowRt = row.AddComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(240f, 36f);

            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.spacing = 8f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Action Label
            GameObject labelObj = new GameObject("ActionLabel");
            labelObj.layer = uiLayer;
            labelObj.transform.SetParent(row.transform, false);
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.sizeDelta = new Vector2(140f, 32f);

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = actionName;
            labelTmp.fontSize = 20f;
            labelTmp.alignment = TextAlignmentOptions.MidlineRight;
            labelTmp.color = Color.white;
            if (font != null) labelTmp.font = font;

            // Primary Icon
            GameObject primaryObj = new GameObject("PrimaryIcon");
            primaryObj.layer = uiLayer;
            primaryObj.transform.SetParent(row.transform, false);
            RectTransform primaryRt = primaryObj.AddComponent<RectTransform>();
            primaryRt.sizeDelta = new Vector2(34f, 34f);
            Image primaryImg = primaryObj.AddComponent<Image>();
            primaryImg.preserveAspect = true;

            // Separator Text
            GameObject sepObj = new GameObject("Separator");
            sepObj.layer = uiLayer;
            sepObj.transform.SetParent(row.transform, false);
            RectTransform sepRt = sepObj.AddComponent<RectTransform>();
            sepRt.sizeDelta = new Vector2(14f, 32f);
            TextMeshProUGUI sepTmp = sepObj.AddComponent<TextMeshProUGUI>();
            sepTmp.text = "+";
            sepTmp.fontSize = 16f;
            sepTmp.alignment = TextAlignmentOptions.Center;
            sepTmp.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
            if (font != null) sepTmp.font = font;
            sepObj.SetActive(false);

            // Secondary Icon
            GameObject secObj = new GameObject("SecondaryIcon");
            secObj.layer = uiLayer;
            secObj.transform.SetParent(row.transform, false);
            RectTransform secRt = secObj.AddComponent<RectTransform>();
            secRt.sizeDelta = new Vector2(34f, 34f);
            Image secImg = secObj.AddComponent<Image>();
            secImg.preserveAspect = true;
            secObj.SetActive(false);

            // Row Component Setup
            ControlPromptRowUI rowComp = row.AddComponent<ControlPromptRowUI>();
            rowComp.ActionKey = actionName;
            SerializedObject so = new SerializedObject(rowComp);
            so.FindProperty("actionKey").stringValue = actionName;
            so.FindProperty("actionText").objectReferenceValue = labelTmp;
            so.FindProperty("primaryIcon").objectReferenceValue = primaryImg;
            so.FindProperty("secondaryIcon").objectReferenceValue = secImg;
            so.FindProperty("separatorText").objectReferenceValue = sepTmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            return row;
        }
    }
}
