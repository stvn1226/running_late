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

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            BuildKeyMap();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Shows the keyboard and highlights the given key names using the provided outline material.
        /// Key names: "a", "d", "s", "w", "f", "q", "c", "v", "b", "space", "shift", "ctrl", etc.
        /// </summary>
        public void HighlightKeys(Material outlineMat, params string[] keyNames)
        {
            ClearHighlights();
            SetVisible(true);

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
        }

        /// <summary>Shows or hides the keyboard via CanvasGroup alpha (never blocks raycasts).</summary>
        public void SetVisible(bool visible)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        // ── Key Map ───────────────────────────────────────────────────────

        private void BuildKeyMap()
        {
            keyImageMap.Clear();
            Transform root = keyboardRoot != null ? keyboardRoot : transform;

            // Walk every VirtualKeyboardButton; their GameObject name is "Key-X"
            var buttons = root.GetComponentsInChildren<VirtualKeyboardButton>(true);
            foreach (var btn in buttons)
            {
                string goName = btn.gameObject.name; // e.g. "Key-a", "Key-space"
                if (!goName.StartsWith("Key-")) continue;

                string keyPart = goName.Substring(4).ToLowerInvariant();
                if (keyImageMap.ContainsKey(keyPart)) continue;

                // Prefer the Image directly on the button GO; fallback to first child Image
                Image img = btn.GetComponent<Image>() ?? btn.GetComponentInChildren<Image>(true);
                if (img != null)
                    keyImageMap[keyPart] = img;
            }

            // Fallback: plain Images whose parent GameObject starts with "Key-"
            if (keyImageMap.Count == 0)
            {
                foreach (var img in root.GetComponentsInChildren<Image>(true))
                {
                    string gn = img.transform.parent != null
                        ? img.transform.parent.name
                        : img.gameObject.name;
                    if (!gn.StartsWith("Key-")) continue;
                    string kp = gn.Substring(4).ToLowerInvariant();
                    if (!keyImageMap.ContainsKey(kp))
                        keyImageMap[kp] = img;
                }
            }
        }

        private Image FindKeyImage(string normalizedKey)
        {
            Transform root = keyboardRoot != null ? keyboardRoot : transform;
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
            BuildKeyMap();
            Debug.Log($"[VirtualKeyboardOverlay] {keyImageMap.Count} keys mapped:");
            foreach (var kv in keyImageMap)
                Debug.Log($"  '{kv.Key}' -> {kv.Value?.gameObject.name}");
        }
#endif
    }
}
