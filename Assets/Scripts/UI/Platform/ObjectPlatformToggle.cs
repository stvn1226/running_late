using UnityEngine;

/// <summary>
/// Automatically enables or disables a GameObject based on the current platform/build target.
/// Useful for hiding Touch controls on Desktop or hiding Keyboard prompts on Mobile.
/// </summary>
public class ObjectPlatformToggle : MonoBehaviour
{
    public enum VisibilityMode
    {
        ShowOnMobileOnly,
        ShowOnDesktopOnly,
        AlwaysShow,
        AlwaysHide
    }

    [Header("Visibility Settings")]
    [SerializeField] private VisibilityMode mode = VisibilityMode.AlwaysShow;
    
    [Tooltip("If true, this will check 'Application.isMobilePlatform'. If false, it uses build-target logic.")]
    [SerializeField] private bool useRuntimeDetection = true;

    private void Awake()
    {
        ApplyVisibility();
    }

    [ContextMenu("Apply Visibility")]
    public void ApplyVisibility()
    {
        bool isMobile = IsMobile();

        switch (mode)
        {
            case VisibilityMode.ShowOnMobileOnly:
                gameObject.SetActive(isMobile);
                break;
            case VisibilityMode.ShowOnDesktopOnly:
                gameObject.SetActive(!isMobile);
                break;
            case VisibilityMode.AlwaysShow:
                gameObject.SetActive(true);
                break;
            case VisibilityMode.AlwaysHide:
                gameObject.SetActive(false);
                break;
        }
    }

    private bool IsMobile()
    {
        // 1. Check for Forced Mobile Mode in PlayerController (if available in scene)
        // This is useful for Editor testing on PC.
        var player = FindFirstObjectByType<PlayerControllerModular>();
        if (player != null && (player.ForceMobileModeEditor || player.IsMobileMode))
        {
            return true;
        }

        // 2. Runtime platform check
        if (useRuntimeDetection && Application.isMobilePlatform)
        {
            return true;
        }

        // 3. Simple Build Target check (Preprocessor)
#if UNITY_ANDROID || UNITY_IOS
        return true;
#else
        return false;
#endif
    }
}
