using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.GamePanels
{
    /// <summary>
    /// Adaptor that connects ControlsTutorialOverlay with the VirtualCanvasGroup QWERTY keyboard.
    /// Place this on the same GameObject as the VirtualCanvasGroup root.
    /// Finds key Images by GameObject name and applies/removes the outline material.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class VirtualKeyboardOverlay : MonoBehaviour
    {
        [Header("Keyboard Root")]
        [Tooltip("Root transform of the VirtualCanvasGroup (QWERTY). If null, uses this transform.")]
        [SerializeField] private Transform keyboardRoot;

        private CanvasGroup canvasGroup;
        private readonly Dictionary<string, Image> keyImageMap = new Dictionary<string, Image>();
        private readonly List<Image> currentlyHighlighted = new List<Image>();
        private bool _mapBuilt = false;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            TryBuildMap();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Shows the keyboard and highlights the given key names using the provided outline material.
        /// Key names: "a", "d", "s", "w", "f", "q", "c", "v", "b", "space", "shift", "ctrl", etc.
        /// </summary>
        public void HighlightKeys(Material outlineMat, params string[] keyNames)
        {
            ClearHighlights();
            TryBuildMap(); // lazy build — VirtualKeyboardPanel might not be ready at Start
            ShowKeyboardPanel();

            foreach (var key in keyNames)
            {
                string normalized = NormalizeKeyName(key);

                if (!keyImageMap.TryGetValue(normalized, out Image img) || img == null)
                    img = FindKeyImage(normalized);

                if (img == null) continue;

                keyImageMap[normalized] = img;
                if (outlineMat != null)
                    img.material = outlineMat;
                img.color = Color.white;

                if (!currentlyHighlighted.Contains(img))
                    currentlyHighlighted.Add(img);
            }
        }

        /// <summary>Removes all active outlines and resets key images.</summary>
        public void ClearHighlights()
        {
            foreach (var img in currentlyHighlighted)
            {
                if (img == null) continue;
                img.material = null;
                img.color = Color.white;
            }
            currentlyHighlighted.Clear();
            HideKeyboardPanel();
        }

        /// <summary>Shows or hides the keyboard via CanvasGroup alpha (never blocks raycasts).</summary>
        public void SetVisible(bool visible)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        // ── VirtualKeyboardPanel visibility (display-only mode) ───────────

        /// <summary>
        /// Shows the VirtualKeyboardPanel in display-only mode (alpha=1, non-interactive).
        /// Does nothing if the panel is already open for actual text input.
        /// </summary>
        private void ShowKeyboardPanel()
        {
            var panel = GetVirtualKeyboardPanel();
            if (panel == null) return;
            if (panel.IsOpen) return; // don't interfere with real keyboard usage

            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) return;
            cg.alpha = 1f;
            cg.blocksRaycasts = false; // display-only: never steal input
            cg.interactable = false;
        }

        /// <summary>Hides the VirtualKeyboardPanel if we were the ones showing it.</summary>
        private void HideKeyboardPanel()
        {
            var panel = GetVirtualKeyboardPanel();
            if (panel == null) return;
            if (panel.IsOpen) return; // don't hide it if real keyboard is open

            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) return;
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        private VirtualKeyboardPanel GetVirtualKeyboardPanel()
        {
            if (VirtualKeyboardPanel.Instance != null) return VirtualKeyboardPanel.Instance;
            return FindObjectOfType<VirtualKeyboardPanel>(true);
        }

        // ── Key Map ───────────────────────────────────────────────────────

        /// <summary>Returns the root Transform to search for keys. Priority: explicit field → VirtualKeyboardPanel singleton → null.</summary>
        private Transform GetKeyboardRoot()
        {
            if (keyboardRoot != null) return keyboardRoot;
            if (VirtualKeyboardPanel.Instance != null) return VirtualKeyboardPanel.Instance.transform;
            // Last resort (editor / scene not fully loaded)
            var found = FindObjectOfType<VirtualKeyboardPanel>(true);
            return found != null ? found.transform : null;
        }

        private void TryBuildMap()
        {
            if (_mapBuilt) return;
            BuildKeyMap();
        }

        private void BuildKeyMap()
        {
            keyImageMap.Clear();
            Transform root = GetKeyboardRoot();
            if (root == null)
            {
                Debug.LogWarning("[VirtualKeyboardOverlay] No keyboard root found. Assign keyboardRoot or ensure VirtualKeyboardPanel is in the scene.");
                return;
            }

            // Pass 1: VirtualKeyboardButton components (most reliable)
            var buttons = root.GetComponentsInChildren<VirtualKeyboardButton>(true);
            foreach (var btn in buttons)
            {
                string goName = btn.gameObject.name; // e.g. "Key-a", "Key-space"
                if (!goName.StartsWith("Key-")) continue;

                string keyPart = goName.Substring(4).ToLowerInvariant();
                if (keyImageMap.ContainsKey(keyPart)) continue;

                Image img = btn.GetComponent<Image>() ?? btn.GetComponentInChildren<Image>(true);
                if (img != null)
                    keyImageMap[keyPart] = img;
            }

            // Pass 2: plain GameObjects named "Key-X" (fallback if no VirtualKeyboardButton)
            if (keyImageMap.Count == 0)
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    string gn = t.name;
                    if (!gn.StartsWith("Key-")) continue;
                    string kp = gn.Substring(4).ToLowerInvariant();
                    if (keyImageMap.ContainsKey(kp)) continue;
                    Image img = t.GetComponent<Image>() ?? t.GetComponentInChildren<Image>(true);
                    if (img != null) keyImageMap[kp] = img;
                }
            }

            if (keyImageMap.Count > 0) _mapBuilt = true;
        }

        private Image FindKeyImage(string normalizedKey)
        {
            Transform root = GetKeyboardRoot();
            if (root == null) return null;
            string targetName = "key-" + normalizedKey;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLowerInvariant() != targetName) continue;
                Image img = t.GetComponent<Image>() ?? t.GetComponentInChildren<Image>(true);
                if (img != null) return img;
            }
            return null;
        }

        private static string NormalizeKeyName(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            switch (key.ToLowerInvariant().Trim())
            {
                case "spacebar":                    return "space";
                case "leftshift": case "rightshift": return "shift";
                case "leftctrl":  case "rightctrl":
                case "ctrl":                        return "ctrl";
                case "del":                         return "backspace";
                case "enter": case "return":        return "enter";
                default:                            return key.ToLowerInvariant().Trim();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Rebuild Key Map and Print")]
        private void EditorRebuildMap()
        {
            _mapBuilt = false;
            BuildKeyMap();
            Debug.Log($"[VirtualKeyboardOverlay] {keyImageMap.Count} keys mapped:");
            foreach (var kv in keyImageMap)
                Debug.Log($"  '{kv.Key}' -> {kv.Value?.gameObject.name}");
        }
#endif
    }
}
