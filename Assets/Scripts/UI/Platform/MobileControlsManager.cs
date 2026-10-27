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
        private System.Collections.Generic.List<GameObject> mobileButtonsContainers = new System.Collections.Generic.List<GameObject>();

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
            Instance.ResolveButtonsContainers();
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
            ResolveButtonsContainers();
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
            mobileButtonsContainers.Clear();
            ResolveButtonsContainers();
            SetControlsVisible(ShouldShowMobileControls);
        }

        public static GameObject FindMobileButtons()
        {
            var list = FindAllMobileButtons();
            return list.Count > 0 ? list[0] : null;
        }

        public static System.Collections.Generic.List<GameObject> FindAllMobileButtons()
        {
            var result = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var transforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in transforms)
                    {
                        if (t.name.StartsWith("MobileButtons"))
                        {
                            if (!result.Contains(t.gameObject))
                            {
                                result.Add(t.gameObject);
                            }
                        }
                    }
                }
            }
            return result;
        }

        public void ResolveButtonsContainers()
        {
            mobileButtonsContainers.RemoveAll(item => item == null);
            if (mobileButtonsContainers.Count == 0)
            {
                mobileButtonsContainers = FindAllMobileButtons();
            }
        }

        public void SetControlsVisible(bool visible)
        {
            ResolveButtonsContainers();
            bool active = visible && ShouldShowMobileControls;
            foreach (var container in mobileButtonsContainers)
            {
                if (container != null)
                {
                    container.SetActive(active);
                }
            }
            GameLogger.Log("MOBILE", $"MobileButtons containers ({mobileButtonsContainers.Count}) active: {active}");
        }
    }
}

