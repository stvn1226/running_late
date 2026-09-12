using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;

namespace EditorTools
{
    /// <summary>
    /// Builds the Leaderboard prefabs and wires them into the active scene.
    ///
    /// Run from: Tools / Leaderboard / Build Leaderboard Prefabs & Wire Scene
    ///
    /// What it does:
    ///   1. Creates Assets/Prefabs/UI/LeaderboardRowUI.prefab
    ///   2. Creates Assets/Prefabs/UI/LeaderboardPanel.prefab
    ///   3. In the active scene, finds the main Menu Canvas, adds the
    ///      LeaderboardPanel GameObject, wires LeaderboardPanel + LeaderboardRowUI
    ///      on the MainMenuController and MainMenuPanel components.
    /// </summary>
    public static class LeaderboardBuilder
    {
        private const string ROW_PREFAB_PATH   = "Assets/Prefabs/UI/LeaderboardRowUI.prefab";
        private const string PANEL_PREFAB_PATH = "Assets/Prefabs/UI/LeaderboardPanel.prefab";

        // ── Fonts ──────────────────────────────────────────────────────────────
        private static TMP_FontAsset LoadFont()
        {
            TMP_FontAsset f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Roboto-Bold SDF.asset");
            if (f == null)
                f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            return f;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  STEP 1 — Row Prefab
        // ══════════════════════════════════════════════════════════════════════
        [MenuItem("Tools/Leaderboard/1 - Create Row Prefab")]
        public static LeaderboardRowUI CreateRowPrefab()
        {
            EnsureDir(ROW_PREFAB_PATH);
            int layer = UiLayer();
            TMP_FontAsset font = LoadFont();

            // Root
            GameObject root = new GameObject("LeaderboardRowUI");
            root.layer = layer;
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(600f, 48f);

            // Background image (for local-player highlight)
            Image bg = root.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0f);     // transparent by default

            // Horizontal layout
            HorizontalLayoutGroup hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 12f;
            hlg.padding = new RectOffset(12, 12, 0, 0);
            hlg.childControlWidth  = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            // Rank label  (60px wide)
            TextMeshProUGUI rankLbl   = MakeLabel(root.transform, "RankLabel",   font, layer, 60f, 48f, TextAlignmentOptions.MidlineLeft);
            rankLbl.text  = "#1";
            rankLbl.fontSize = 22f;
            rankLbl.fontStyle = FontStyles.Bold;

            // Name label  (expand)
            TextMeshProUGUI nameLbl   = MakeLabel(root.transform, "NameLabel",   font, layer, 400f, 48f, TextAlignmentOptions.MidlineLeft);
            nameLbl.text  = "PlayerName";
            LayoutElement nameLE = nameLbl.gameObject.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1f;

            // Time label  (120px wide)
            TextMeshProUGUI timeLbl   = MakeLabel(root.transform, "TimeLabel",   font, layer, 120f, 48f, TextAlignmentOptions.MidlineRight);
            timeLbl.text  = "00:00.00";
            timeLbl.fontSize = 20f;

            // Wire component
            LeaderboardRowUI rowComp = root.AddComponent<LeaderboardRowUI>();
            SerializedObject so = new SerializedObject(rowComp);
            so.FindProperty("rankLabel").objectReferenceValue       = rankLbl;
            so.FindProperty("playerNameLabel").objectReferenceValue = nameLbl;
            so.FindProperty("timeLabel").objectReferenceValue       = timeLbl;
            so.FindProperty("rowBackground").objectReferenceValue   = bg;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ROW_PREFAB_PATH);
            Object.DestroyImmediate(root);

            LeaderboardRowUI prefabComp = saved.GetComponent<LeaderboardRowUI>();
            Debug.Log($"[LeaderboardBuilder] Row prefab saved → {ROW_PREFAB_PATH}");
            return prefabComp;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  STEP 2 — Panel Prefab
        // ══════════════════════════════════════════════════════════════════════
        [MenuItem("Tools/Leaderboard/2 - Create Panel Prefab")]
        public static void CreatePanelPrefab()
        {
            EnsureDir(PANEL_PREFAB_PATH);
            int layer = UiLayer();
            TMP_FontAsset font = LoadFont();

            // ── Root ───────────────────────────────────────────────────────────
            GameObject root = new GameObject("LeaderboardPanel");
            root.layer = layer;
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;

            // Dark semi-transparent background
            Image bgImg = root.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

            // ── panelRoot (same as root for this panel) ────────────────────────
            // We store a reference to root itself as panelRoot via BaseMainMenuPanel.

            // ── Title ──────────────────────────────────────────────────────────
            TextMeshProUGUI titleLbl = MakeLabel(root.transform, "TitleLabel", font, layer, 600f, 70f, TextAlignmentOptions.Center);
            titleLbl.text = "LEADERBOARD";
            titleLbl.fontSize = 38f;
            titleLbl.fontStyle = FontStyles.Bold;
            titleLbl.color = Color.white;
            RectTransform titleRT = titleLbl.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot     = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -30f);
            titleRT.sizeDelta = new Vector2(0f, 70f);

            // ── Tab bar ────────────────────────────────────────────────────────
            GameObject tabBar = new GameObject("TabBar");
            tabBar.layer = layer;
            RectTransform tabRT = tabBar.AddComponent<RectTransform>();
            tabRT.SetParent(root.transform, false);
            tabRT.anchorMin = new Vector2(0.5f, 1f);
            tabRT.anchorMax = new Vector2(0.5f, 1f);
            tabRT.pivot     = new Vector2(0.5f, 1f);
            tabRT.anchoredPosition = new Vector2(0f, -110f);
            tabRT.sizeDelta = new Vector2(340f, 50f);
            HorizontalLayoutGroup tabHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
            tabHlg.spacing = 20f;
            tabHlg.childControlWidth  = false;
            tabHlg.childControlHeight = false;
            tabHlg.childForceExpandWidth  = false;
            tabHlg.childForceExpandHeight = false;
            tabHlg.childAlignment = TextAnchor.MiddleCenter;

            Button tabGlobal  = MakeButton(tabBar.transform, "TabGlobal",  "Global",  font, layer, 160f, 50f);
            Button tabFriends = MakeButton(tabBar.transform, "TabFriends", "Amigos",  font, layer, 160f, 50f);

            // ── Row Container (ScrollRect) ─────────────────────────────────────
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.layer = layer;
            RectTransform vpRT = viewport.AddComponent<RectTransform>();
            vpRT.SetParent(root.transform, false);
            vpRT.anchorMin = new Vector2(0.1f, 0.12f);
            vpRT.anchorMax = new Vector2(0.9f, 0.82f);
            vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0,0,0,0); // invisible mask
            viewport.AddComponent<RectMask2D>();

            // Content (actual row container)
            GameObject content = new GameObject("Content");
            content.layer = layer;
            RectTransform contentRT = content.AddComponent<RectTransform>();
            contentRT.SetParent(viewport.transform, false);
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childControlWidth  = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ScrollRect on viewport
            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.content   = contentRT;
            scroll.viewport  = vpRT;
            scroll.horizontal = false;
            scroll.vertical   = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // ── Loading overlay ────────────────────────────────────────────────
            GameObject loadingGO = new GameObject("LoadingOverlay");
            loadingGO.layer = layer;
            RectTransform loadRT = loadingGO.AddComponent<RectTransform>();
            loadRT.SetParent(root.transform, false);
            loadRT.anchorMin = new Vector2(0f, 0.12f);
            loadRT.anchorMax = new Vector2(1f, 0.82f);
            loadRT.offsetMin = loadRT.offsetMax = Vector2.zero;
            TextMeshProUGUI loadLbl = MakeLabel(loadingGO.transform, "LoadingLabel", font, layer, 400f, 60f, TextAlignmentOptions.Center);
            loadLbl.text = "Loading...";
            loadLbl.fontSize = 24f;
            loadLbl.color = new Color(1f,1f,1f,0.6f);
            RectTransform loadLblRT = loadLbl.GetComponent<RectTransform>();
            loadLblRT.anchorMin = Vector2.zero;
            loadLblRT.anchorMax = Vector2.one;
            loadLblRT.offsetMin = loadLblRT.offsetMax = Vector2.zero;
            loadingGO.SetActive(false);

            // ── Empty label ────────────────────────────────────────────────────
            TextMeshProUGUI emptyLbl = MakeLabel(root.transform, "EmptyLabel", font, layer, 400f, 60f, TextAlignmentOptions.Center);
            emptyLbl.text = "No records yet";
            emptyLbl.fontSize = 22f;
            emptyLbl.color = new Color(1f,1f,1f,0.5f);
            RectTransform emptyRT = emptyLbl.GetComponent<RectTransform>();
            emptyRT.anchorMin = new Vector2(0f, 0.4f);
            emptyRT.anchorMax = new Vector2(1f, 0.6f);
            emptyRT.offsetMin = emptyRT.offsetMax = Vector2.zero;
            emptyLbl.gameObject.SetActive(false);

            // ── Back button ────────────────────────────────────────────────────
            Button backBtn = MakeButton(root.transform, "BackButton", "← Back", font, layer, 160f, 50f);
            RectTransform backRT = backBtn.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.5f, 0f);
            backRT.anchorMax = new Vector2(0.5f, 0f);
            backRT.pivot     = new Vector2(0.5f, 0f);
            backRT.anchoredPosition = new Vector2(0f, 20f);
            backRT.sizeDelta = new Vector2(160f, 50f);

            // ── Wire LeaderboardPanel component ────────────────────────────────
            LeaderboardPanel panelComp = root.AddComponent<LeaderboardPanel>();
            SerializedObject so = new SerializedObject(panelComp);
            so.FindProperty("panelRoot").objectReferenceValue     = root;
            so.FindProperty("rowContainer").objectReferenceValue  = contentRT;
            so.FindProperty("tabGlobal").objectReferenceValue     = tabGlobal;
            so.FindProperty("tabFriends").objectReferenceValue    = tabFriends;
            so.FindProperty("backButton").objectReferenceValue    = backBtn;
            so.FindProperty("loadingOverlay").objectReferenceValue = loadingGO;
            so.FindProperty("emptyLabel").objectReferenceValue    = emptyLbl;
            so.FindProperty("titleLabel").objectReferenceValue    = titleLbl;
            so.FindProperty("defaultSelectedButton").objectReferenceValue = tabGlobal;

            // Row prefab reference
            LeaderboardRowUI rowPrefabAsset = AssetDatabase.LoadAssetAtPath<LeaderboardRowUI>(ROW_PREFAB_PATH);
            if (rowPrefabAsset == null)
                rowPrefabAsset = CreateRowPrefab();
            so.FindProperty("rowPrefab").objectReferenceValue = rowPrefabAsset;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save
            PrefabUtility.SaveAsPrefabAsset(root, PANEL_PREFAB_PATH);
            Object.DestroyImmediate(root);
            Debug.Log($"[LeaderboardBuilder] Panel prefab saved → {PANEL_PREFAB_PATH}");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  STEP 3 — Wire into active scene
        // ══════════════════════════════════════════════════════════════════════
        [MenuItem("Tools/Leaderboard/3 - Wire into Active Scene")]
        public static void WireIntoScene()
        {
            // Ensure prefabs exist
            if (!File.Exists(ROW_PREFAB_PATH))   CreateRowPrefab();
            if (!File.Exists(PANEL_PREFAB_PATH))  CreatePanelPrefab();

            // Find MainMenuController in active scene
            MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>();
            if (controller == null)
            {
                Debug.LogError("[LeaderboardBuilder] MainMenuController not found in active scene. Open the main menu scene first.");
                return;
            }

            // Find existing LeaderboardPanel or instantiate one
            LeaderboardPanel existing = Object.FindFirstObjectByType<LeaderboardPanel>();
            LeaderboardPanel panelInstance;
            if (existing != null)
            {
                panelInstance = existing;
                Debug.Log("[LeaderboardBuilder] Reusing existing LeaderboardPanel in scene.");
            }
            else
            {
                GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PANEL_PREFAB_PATH);
                // Find the canvas used by MainMenuController
                Canvas canvas = controller.GetComponentInChildren<Canvas>(true);
                if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvas.transform);
                inst.name = "LeaderboardPanel";
                RectTransform rt = inst.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                panelInstance = inst.GetComponent<LeaderboardPanel>();
            }

            // Wire LeaderboardPanel into MainMenuController
            SerializedObject soCtrl = new SerializedObject(controller);
            soCtrl.FindProperty("leaderboardPanel").objectReferenceValue = panelInstance;
            soCtrl.ApplyModifiedProperties();

            // Wire leaderboardButton in MainMenuPanel
            MainMenuPanel menuPanel = Object.FindFirstObjectByType<MainMenuPanel>();
            if (menuPanel != null)
            {
                // Try to find a Button named "LeaderboardButton" or "Leaderboard" in the panel
                Button[] buttons = menuPanel.GetComponentsInChildren<Button>(true);
                Button leaderboardBtn = null;
                foreach (var btn in buttons)
                {
                    string n = btn.gameObject.name.ToLower();
                    if (n.Contains("leaderboard")) { leaderboardBtn = btn; break; }
                }

                if (leaderboardBtn != null)
                {
                    SerializedObject soPanel = new SerializedObject(menuPanel);
                    soPanel.FindProperty("leaderboardButton").objectReferenceValue = leaderboardBtn;
                    if (leaderboardBtn.transform.parent != null && 
                        leaderboardBtn.transform.parent.name.ToLower().Contains("leaderboard"))
                    {
                        soPanel.FindProperty("leaderboardContainer").objectReferenceValue = leaderboardBtn.transform.parent.gameObject;
                    }
                    soPanel.ApplyModifiedProperties();
                    Debug.Log($"[LeaderboardBuilder] leaderboardButton wired → {leaderboardBtn.name}");
                }
                else
                {
                    Debug.LogWarning("[LeaderboardBuilder] No Button with 'leaderboard' in its name found under MainMenuPanel. Add the button manually and re-run, or assign it in the Inspector.");
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[LeaderboardBuilder] ✅ Scene wired. Save the scene to persist changes.");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MASTER — runs all 3 steps
        // ══════════════════════════════════════════════════════════════════════
        [MenuItem("Tools/Leaderboard/Build Leaderboard Prefabs & Wire Scene")]
        public static void BuildAll()
        {
            CreateRowPrefab();
            CreatePanelPrefab();
            WireIntoScene();
        }

        [MenuItem("Tools/Leaderboard/Toggle Leaderboard Feature (Enable-Disable)")]
        public static void ToggleLeaderboardActive()
        {
            MainMenuPanel menuPanel = Object.FindFirstObjectByType<MainMenuPanel>();
            if (menuPanel != null)
            {
                SerializedObject so = new SerializedObject(menuPanel);
                SerializedProperty prop = so.FindProperty("showLeaderboard");
                if (prop != null)
                {
                    prop.boolValue = !prop.boolValue;
                    so.ApplyModifiedProperties();
                    menuPanel.UpdateStep1Navigation();
                    Debug.Log($"[LeaderboardBuilder] showLeaderboard is now: {prop.boolValue}");
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
            }
            else
            {
                Debug.LogWarning("[LeaderboardBuilder] MainMenuPanel not found in active scene.");
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static int UiLayer() { int l = LayerMask.NameToLayer("UI"); return l >= 0 ? l : 5; }

        private static void EnsureDir(string assetPath)
        {
            string dir = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private static TextMeshProUGUI MakeLabel(Transform parent, string name, TMP_FontAsset font,
            int layer, float w, float h, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name);
            go.layer = layer;
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(w, h);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.fontSize = 20f;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static Button MakeButton(Transform parent, string name, string label,
            TMP_FontAsset font, int layer, float w, float h)
        {
            GameObject go = new GameObject(name);
            go.layer = layer;
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(w, h);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.18f, 0.28f, 1f);
            Button btn = go.AddComponent<Button>();

            // Label child
            TextMeshProUGUI lbl = MakeLabel(go.transform, "Label", font, layer, w, h, TextAlignmentOptions.Center);
            lbl.text = label;
            lbl.fontSize = 18f;
            RectTransform lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

            return btn;
        }
    }
}
