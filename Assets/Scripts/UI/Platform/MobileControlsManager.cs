using UnityEngine;
using UnityEngine.SceneManagement;

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
#if UNITY_ANDROID || UNITY_IOS
                return true;
#else
                if (Application.isMobilePlatform) return true;
                if (forceInEditor) return true;
                var player = Object.FindFirstObjectByType<PlayerControllerModular>();
                if (player != null && (player.IsMobileMode || player.ForceMobileModeEditor)) return true;
                return false;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var go = new GameObject("MobileControlsManager");
                Instance = go.AddComponent<MobileControlsManager>();
                DontDestroyOnLoad(go);
            }
            Instance.ResolveButtonsContainer();
            Instance.SetControlsVisible(Instance.ShouldShowMobileControls);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
            ResolveButtonsContainer();
            SetControlsVisible(ShouldShowMobileControls);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            mobileButtonsContainer = null;
            ResolveButtonsContainer();
            SetControlsVisible(ShouldShowMobileControls);
        }

        public static GameObject FindMobileButtons()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var transforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in transforms)
                    {
                        if (t.name == "MobileButtons")
                            return t.gameObject;
                    }
                }
            }
            return null;
        }

        public void ResolveButtonsContainer()
        {
            if (mobileButtonsContainer != null && mobileButtonsContainer.scene.isLoaded) return;

            mobileButtonsContainer = FindMobileButtons();
        }

        public void SetControlsVisible(bool visible)
        {
            ResolveButtonsContainer();
            if (mobileButtonsContainer != null)
            {
                bool active = visible && ShouldShowMobileControls;
                mobileButtonsContainer.SetActive(active);
                GameLogger.Log("MOBILE", $"MobileButtons active: {active}");
            }
        }
    }
}

