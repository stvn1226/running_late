using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.GamePanels
{
    /// <summary>
    /// Pure-visual QWERTY keyboard for UI overlays.
    /// NO input handling, NO buttons, NO functionality.
    /// Right-click the component in the Inspector → "Build QWERTY Layout" to generate keys as children.
    /// Call HighlightKeys / ClearHighlights at runtime to apply or remove an outline material.
    /// </summary>
    [ExecuteAlways]
    public class KeyboardDisplay : MonoBehaviour
    {
        [Header("Key Size")]
        [SerializeField] private float keyWidth  = 58f;
        [SerializeField] private float keyHeight = 52f;
        [SerializeField] private float spacing   = 5f;

        [Header("Key Visuals")]
        [SerializeField] private Sprite      keySprite;
        [SerializeField] private Color       keyNormalColor = new Color(0.18f, 0.18f, 0.22f, 1f);
        [SerializeField] private Color       keyTextColor   = Color.white;
        [SerializeField] private float       keyTextSize    = 14f;
        [SerializeField] private TMP_FontAsset font;

        // ── Runtime highlight state ───────────────────────────────────────
        private readonly Dictionary<string, Image> _keyMap =
            new Dictionary<string, Image>(System.StringComparer.OrdinalIgnoreCase);
        private readonly List<Image> _highlighted = new List<Image>();
        private readonly List<Image> _pressed = new List<Image>();

        // ── QWERTY layout definition ──────────────────────────────────────
        // Each string is the label shown on the key AND the lookup name.
        private static readonly string[][] Rows =
        {
            new[] { "`","1","2","3","4","5","6","7","8","9","0","-","=","DEL"   },
            new[] { "TAB","Q","W","E","R","T","Y","U","I","O","P","[","]","\\"  },
            new[] { "CAPS","A","S","D","F","G","H","J","K","L",";","'","ENTER"  },
            new[] { "SHIFT","Z","X","C","V","B","N","M",",",".","/" ,"SHIFT↑"  },
            new[] { "CTRL","ALT","SPACE","ALT↑","CTRL↑" },
        };

        // Special keys get wider than a standard key (multiplier of keyWidth)
        private static readonly Dictionary<string, float> WidthMult =
            new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "DEL",    1.6f }, { "TAB",   1.4f }, { "CAPS",  1.6f },
            { "ENTER",  1.8f }, { "SHIFT", 2.2f }, { "SHIFT↑",2.2f },
            { "SPACE",  5.5f }, { "CTRL",  1.4f }, { "CTRL↑", 1.4f },
            { "ALT",    1.2f }, { "ALT↑",  1.2f },
        };

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Applies outlineMat to the Image of each named key.
        /// Key names are case-insensitive: "a", "SPACE", "Shift", "ctrl", etc.
        /// </summary>
        public void HighlightKeys(Material outlineMat, params string[] keyNames)
        {
            EnsureKeyMap();
            ClearHighlights();
            foreach (var key in keyNames)
            {
                if (!_keyMap.TryGetValue(key, out Image img) || img == null) continue;
                if (outlineMat != null) img.material = outlineMat;
                img.color = Color.white;
                _highlighted.Add(img);
            }
        }

        /// <summary>
        /// Removes the outline from a key and lowers its alpha when pressed by the player.
        /// </summary>
        public void RemoveKeyHighlight(string key)
        {
            EnsureKeyMap();
            if (!_keyMap.TryGetValue(key, out Image img) || img == null) return;
            if (_highlighted.Remove(img))
            {
                img.material = null;
                img.color = new Color(keyNormalColor.r, keyNormalColor.g, keyNormalColor.b, 0.35f);
                if (!_pressed.Contains(img)) _pressed.Add(img);
            }
        }

        /// <summary>Number of keys currently highlighted.</summary>
        public int RemainingHighlightCount => _highlighted.Count;

        /// <summary>Removes all highlight outlines and resets all key colours to normal full opacity.</summary>
        public void ClearHighlights()
        {
            foreach (var img in _highlighted)
            {
                if (img == null) continue;
                img.material = null;
                img.color    = keyNormalColor;
            }
            foreach (var img in _pressed)
            {
                if (img == null) continue;
                img.material = null;
                img.color    = keyNormalColor;
            }
            _highlighted.Clear();
            _pressed.Clear();
        }

        // ── Layout Builder ────────────────────────────────────────────────

        [ContextMenu("Build QWERTY Layout")]
        public void BuildLayout()
        {
            // Destroy existing children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(transform.GetChild(i).gameObject);
                else
#endif
                    Destroy(transform.GetChild(i).gameObject);
            }
            _keyMap.Clear();
            _highlighted.Clear();

            float totalH = Rows.Length * (keyHeight + spacing) - spacing;
            float startY = totalH * 0.5f - keyHeight * 0.5f;

            for (int r = 0; r < Rows.Length; r++)
            {
                string[] row = Rows[r];

                // Total row pixel width (to center it)
                float rowW = -spacing;
                foreach (var lbl in row)
                    rowW += KeyW(lbl) + spacing;

                float x = -rowW * 0.5f;
                float y = startY - r * (keyHeight + spacing);

                foreach (var label in row)
                {
                    float w = KeyW(label);
                    CreateKey(label, new Vector2(x + w * 0.5f, y), new Vector2(w, keyHeight));
                    x += w + spacing;
                }
            }
        }

        private void CreateKey(string label, Vector2 position, Vector2 size)
        {
            // Root GO
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);

            var rt       = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            // Background (pure Image, no Button)
            var img   = go.AddComponent<Image>();
            img.color = keyNormalColor;
            if (keySprite != null) { img.sprite = keySprite; img.type = Image.Type.Sliced; }

            // Label
            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);

            var lblRT    = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero;
            lblRT.offsetMax = Vector2.zero;

            var tmp       = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = DisplayLabel(label);
            tmp.fontSize  = keyTextSize;
            tmp.color     = keyTextColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            if (font != null) tmp.font = font;

            // Register lookup names (case-insensitive via the dictionary comparer)
            string mapKey = label.ToUpperInvariant();
            if (!_keyMap.ContainsKey(mapKey)) _keyMap[mapKey] = img;
            RegisterAliases(mapKey, img);
        }

        // Show a cleaner label on the key face
        private static string DisplayLabel(string raw) => raw switch
        {
            "DEL"    => "⌫",
            "TAB"    => "⇥",
            "CAPS"   => "⇪",
            "ENTER"  => "⏎",
            "SHIFT"  => "⇧",
            "SHIFT↑" => "⇧",
            "SPACE"  => "SPACE",
            "CTRL"   => "Ctrl",
            "CTRL↑"  => "Ctrl",
            "ALT"    => "Alt",
            "ALT↑"   => "Alt",
            _        => raw,
        };

        private void RegisterAliases(string key, Image img)
        {
            switch (key)
            {
                case "DEL":    _keyMap["BACKSPACE"]  = img; break;
                case "SHIFT":
                case "SHIFT↑": _keyMap["LEFTSHIFT"]  = img; _keyMap["RIGHTSHIFT"] = img; break;
                case "CTRL":
                case "CTRL↑":  _keyMap["LEFTCTRL"]   = img; _keyMap["RIGHTCTRL"]  = img; break;
                case "ALT":
                case "ALT↑":   _keyMap["LEFTALT"]    = img; _keyMap["RIGHTALT"]   = img; break;
                case "ENTER":  _keyMap["RETURN"]     = img; break;
                case "SPACE":  _keyMap["SPACEBAR"]   = img; break;
                case "CAPS":   _keyMap["CAPSLOCK"]   = img; break;
            }
        }

        private float KeyW(string label) =>
            WidthMult.TryGetValue(label, out float m) ? m * keyWidth : keyWidth;

        // Rebuilds the lookup map from existing children after domain reload
        private void EnsureKeyMap()
        {
            if (_keyMap.Count > 0) return;
            foreach (Transform child in transform)
            {
                var img = child.GetComponent<Image>();
                if (img == null) continue;
                string k = child.name.ToUpperInvariant();
                if (!_keyMap.ContainsKey(k)) _keyMap[k] = img;
                RegisterAliases(k, img);
            }
        }

        private void OnEnable() => EnsureKeyMap();
    }
}
