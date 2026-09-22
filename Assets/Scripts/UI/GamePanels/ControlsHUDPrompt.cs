using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.GamePanels
{
    [Serializable]
    public class ControlPromptEntry
    {
        [Tooltip("Action label displayed on HUD (e.g. 'Jump', 'Slide', 'Kick')")]
        public string actionName;

        [Header("Keyboard / Mouse")]
        public Sprite keyboardPrimary;
        public Sprite keyboardSecondary;

        [Header("Gamepad")]
        public Sprite gamepadPrimary;
        public Sprite gamepadSecondary;

        public void GetSprites(ControllerType controllerType, out Sprite primary, out Sprite secondary)
        {
            if (controllerType == ControllerType.Keyboard)
            {
                primary = keyboardPrimary;
                secondary = keyboardSecondary;
            }
            else
            {
                // Any Gamepad device (Xbox, Generic, PlayStation, Switch, SteamDeck)
                primary = gamepadPrimary;
                secondary = gamepadSecondary;
            }
        }
    }

    /// <summary>
    /// Displays a persistent, dynamic top-right HUD control prompt list (Monster Hunter style).
    /// Automatically updates button icons when the player switches between Keyboard and Gamepad.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ControlsHUDPrompt : MonoBehaviour
    {
        public static ControlsHUDPrompt Instance { get; private set; }

        [Header("Prompts Configuration")]
        [SerializeField] private List<ControlPromptEntry> promptEntries = new List<ControlPromptEntry>();

        [Header("UI References")]
        [Tooltip("Container Transform with a VerticalLayoutGroup holding prompt rows.")]
        [SerializeField] private Transform rowsContainer;
        [Tooltip("Prefab or template for a prompt row. If null, pre-existing children will be used.")]
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Display Settings")]
        [SerializeField] private bool showOnStart = true;
        [SerializeField] private bool hideOnMobile = true;

        private readonly List<ControlPromptRowUI> spawnedRows = new List<ControlPromptRowUI>();
        private ControllerType currentType = ControllerType.Keyboard;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (rowsContainer == null)
            {
                rowsContainer = transform;
            }

            InitializeRows();
        }

        private void Start()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            if (hideOnMobile)
            {
                SetVisible(false);
                return;
            }
#endif
            SetVisible(showOnStart);
        }

        private void OnEnable()
        {
            if (ControllerTypeDetector.Instance != null)
            {
                ControllerTypeDetector.Instance.OnControllerChanged += OnControllerTypeChanged;
                currentType = ControllerTypeDetector.Instance.CurrentControllerType;
            }
            else
            {
                currentType = ControllerType.Keyboard;
            }

            RefreshPrompts();
        }

        private void OnDisable()
        {
            if (ControllerTypeDetector.Instance != null)
            {
                ControllerTypeDetector.Instance.OnControllerChanged -= OnControllerTypeChanged;
            }
        }

        private void OnControllerTypeChanged(ControllerType newType)
        {
            if (currentType == newType) return;
            currentType = newType;
            RefreshPrompts();
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = visible;
                canvasGroup.interactable = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        public void ToggleVisible()
        {
            if (canvasGroup != null)
            {
                SetVisible(canvasGroup.alpha <= 0.05f);
            }
            else
            {
                SetVisible(!gameObject.activeSelf);
            }
        }

        private void InitializeRows()
        {
            spawnedRows.Clear();

            // Check if rows are already child components
            ControlPromptRowUI[] existingRows = rowsContainer.GetComponentsInChildren<ControlPromptRowUI>(true);
            if (existingRows != null && existingRows.Length > 0)
            {
                spawnedRows.AddRange(existingRows);
                return;
            }

            if (rowTemplate == null) return;

            rowTemplate.SetActive(false);
            for (int i = 0; i < promptEntries.Count; i++)
            {
                GameObject newRowObj = Instantiate(rowTemplate, rowsContainer);
                newRowObj.name = $"PromptRow_{promptEntries[i].actionName}";
                newRowObj.SetActive(true);

                ControlPromptRowUI rowUI = newRowObj.GetComponent<ControlPromptRowUI>();
                if (rowUI == null)
                {
                    rowUI = newRowObj.AddComponent<ControlPromptRowUI>();
                }
                spawnedRows.Add(rowUI);
            }
        }

        public void RefreshPrompts()
        {
            if (rowsContainer == null) rowsContainer = transform;

            if (spawnedRows.Count == 0)
            {
                ControlPromptRowUI[] existingRows = rowsContainer.GetComponentsInChildren<ControlPromptRowUI>(true);
                if (existingRows != null && existingRows.Length > 0)
                {
                    spawnedRows.AddRange(existingRows);
                }
            }

            for (int i = 0; i < promptEntries.Count; i++)
            {
                var entry = promptEntries[i];
                entry.GetSprites(currentType, out Sprite primary, out Sprite secondary);

                // Match row by ActionKey or GameObject name, fallback to index
                ControlPromptRowUI targetRow = spawnedRows.Find(r => r != null && 
                    ((!string.IsNullOrEmpty(r.ActionKey) && string.Equals(r.ActionKey, entry.actionName, StringComparison.OrdinalIgnoreCase)) ||
                     r.gameObject.name.EndsWith(entry.actionName, StringComparison.OrdinalIgnoreCase)));

                if (targetRow == null && i < spawnedRows.Count)
                {
                    targetRow = spawnedRows[i];
                }

                if (targetRow != null)
                {
                    targetRow.Setup(entry.actionName, primary, secondary);
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Load Kenney Default Prompts")]
        public void LoadKenneyDefaultPrompts()
        {
            promptEntries.Clear();

            // 1. Jump: Space / Gamepad A
            promptEntries.Add(new ControlPromptEntry
            {
                actionName = "Jump",
                keyboardPrimary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Keyboard & Mouse/keyboard_space.png"),
                gamepadPrimary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Xbox/xbox_button_color_a.png")
            });

            // 2. Slide: S / Gamepad B
            promptEntries.Add(new ControlPromptEntry
            {
                actionName = "Slide",
                keyboardPrimary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Keyboard & Mouse/keyboard_s.png"),
                gamepadPrimary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Xbox/xbox_button_color_b.png")
            });

            // 3. Kick: Q / Gamepad X
            promptEntries.Add(new ControlPromptEntry
            {
                actionName = "Kick",
                keyboardPrimary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Keyboard & Mouse/keyboard_q.png"),
                gamepadPrimary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Xbox/xbox_button_color_x.png")
            });

            // 4. Drop Down: Down + Space / Stick Down + Gamepad A
            promptEntries.Add(new ControlPromptEntry
            {
                actionName = "Drop Down",
                keyboardPrimary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Keyboard & Mouse/keyboard_arrow_down.png"),
                keyboardSecondary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Keyboard & Mouse/keyboard_space.png"),
                gamepadPrimary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Xbox/xbox_stick_l_down.png"),
                gamepadSecondary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Xbox/xbox_button_color_a.png")
            });

            // 5. Pause: Esc / Gamepad Start
            promptEntries.Add(new ControlPromptEntry
            {
                actionName = "Pause",
                keyboardPrimary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Keyboard & Mouse/keyboard_escape.png"),
                gamepadPrimary = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Kenney_Input/Xbox/xbox_button_start.png")
            });

            EditorUtility.SetDirty(this);
            Debug.Log("[ControlsHUDPrompt] Loaded default prompt entries for Keyboard and Gamepad!");
        }
#endif
    }
}
