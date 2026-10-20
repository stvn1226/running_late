using UnityEngine;

namespace UI.Platform
{
    /// <summary>
    /// Manages the runtime visibility of mobile on-screen buttons (MobileButtons).
    /// Automatically activates on mobile devices (or in Editor when mobile simulation is enabled)
    /// and hides when pause/end menus are active.
    /// </summary>
    public class MobileControlsManager : MonoBehaviour
    {
        public static MobileControlsManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Root GameObject of the on-screen buttons prefab in the scene.")]
        [SerializeField] private GameObject mobileButtonsContainer;

        [Tooltip("If true, forces the mobile buttons to be visible in the Unity Editor.")]
        [SerializeField] private bool forceInEditor = false;

        public bool ShouldShowMobileControls
        {
            get
            {
                if (Application.isMobilePlatform) return true;
#if UNITY_EDITOR
                if (forceInEditor) return true;
                var player = Object.FindFirstObjectByType<PlayerControllerModular>();
                if (player != null && player.IsMobileMode) return true;
#endif
                return false;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResolveButtonsContainer();
            SetControlsVisible(ShouldShowMobileControls);
        }

        private void ResolveButtonsContainer()
        {
            if (mobileButtonsContainer != null) return;

            // Auto-locate MobileButtons in scene even if inactive
            GameObject found = GameObject.Find("MobileButtons");
            if (found != null)
            {
                mobileButtonsContainer = found;
                return;
            }

            var allButtons = Resources.FindObjectsOfTypeAll<MobileActionButton>();
            foreach (var btn in allButtons)
            {
                if (btn.gameObject.scene.isLoaded)
                {
                    Transform t = btn.transform;
                    while (t.parent != null && !t.parent.name.Contains("Canvas") && t.parent.GetComponent<Canvas>() == null)
                    {
                        t = t.parent;
                    }
                    mobileButtonsContainer = t.gameObject;
                    break;
                }
            }
        }

        public void SetControlsVisible(bool visible)
        {
            if (mobileButtonsContainer != null)
            {
                mobileButtonsContainer.SetActive(visible && ShouldShowMobileControls);
            }
        }
    }
}
