using System.Collections;
using UnityEngine;

/// <summary>
/// Captures a clean gameplay frame at the moment of victory, before any UI panels appear.
///
/// Lifecycle:
///   1. Goal.TriggerVictory() starts the VictorySequence coroutine.
///   2. VictorySequence calls CaptureGameplayFrame() FIRST (before the victory delay).
///   3. CaptureGameplayFrame() uses WaitForEndOfFrame to get a fully rendered, UI-free frame.
///   4. The captured Texture2D is stored and later retrieved by ChallengeCardGenerator.
///   5. The texture is released when it is no longer needed or when a new capture replaces it.
///
/// Memory notes:
///   - Only one frame is retained in memory at a time.
///   - If the capture exceeds maxRetainedWidth, it is downscaled immediately.
///   - Explicitly call ReleaseCapture() when the card has been generated, or on scene change.
///
/// Place this component on a GameObject in the gameplay scene.
/// It does NOT use DontDestroyOnLoad — it lives and dies with the game scene.
/// </summary>
public class ChallengeCardCaptureService : MonoBehaviour
{
    [Header("Memory Management")]
    [Tooltip("If the captured frame is wider than this, it is downscaled immediately to save memory.")]
    [SerializeField] private int maxRetainedWidth = 1920;

    // ─── Singleton ────────────────────────────────────────────────────────────────

    public static ChallengeCardCaptureService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        ReleaseCapture();
        if (Instance == this) Instance = null;
    }

    // ─── Public API ───────────────────────────────────────────────────────────────

    /// <summary>The most recently captured frame. Null if no capture has been taken yet.</summary>
    public Texture2D CapturedFrame => _capturedFrame;

    /// <summary>True if a valid capture is currently held in memory.</summary>
    public bool HasCapture => _capturedFrame != null;

    private Texture2D _capturedFrame;

    /// <summary>
    /// Captures the current rendered frame as a Texture2D.
    /// MUST be started as a coroutine — uses WaitForEndOfFrame internally.
    /// Call this BEFORE showing any UI overlay (VictoryPanel, results screen, etc.).
    /// </summary>
    public IEnumerator CaptureGameplayFrame()
    {
        // WaitForEndOfFrame ensures the GPU has finished rendering the current frame.
        // At this point, Time.timeScale may already be paused but the frame render is complete.
        yield return new WaitForEndOfFrame();

        // Release any previous capture before allocating a new one
        ReleaseCapture();

        _capturedFrame = ScreenCapture.CaptureScreenshotAsTexture(1);

        if (_capturedFrame == null)
        {
            GameLogger.LogWarning("CHALLENGE_CAPTURE", "CaptureScreenshotAsTexture returned null.");
            yield break;
        }

        // Downscale immediately if too large (memory management, especially on Steam Deck)
        if (_capturedFrame.width > maxRetainedWidth)
        {
            int scaledH = Mathf.RoundToInt(
                _capturedFrame.height * ((float)maxRetainedWidth / _capturedFrame.width));

            Texture2D scaled = DownscaleTexture(_capturedFrame, maxRetainedWidth, scaledH);
            Destroy(_capturedFrame);
            _capturedFrame = scaled;

            GameLogger.Log("CHALLENGE_CAPTURE",
                $"Frame downscaled to {_capturedFrame.width}x{_capturedFrame.height} for memory efficiency.");
        }
        else
        {
            GameLogger.Log("CHALLENGE_CAPTURE",
                $"Frame captured: {_capturedFrame.width}x{_capturedFrame.height}");
        }
    }

    /// <summary>
    /// Explicitly releases the captured texture from GPU/CPU memory.
    /// Call after the Challenge Card has been generated, or when leaving the game scene.
    /// </summary>
    public void ReleaseCapture()
    {
        if (_capturedFrame != null)
        {
            Destroy(_capturedFrame);
            _capturedFrame = null;
            GameLogger.Log("CHALLENGE_CAPTURE", "Captured frame released from memory.");
        }
    }

    // ─── Internal ─────────────────────────────────────────────────────────────────

    /// <summary>Downscales a texture using GPU blitting. Does not modify the source.</summary>
    private Texture2D DownscaleTexture(Texture2D source, int targetW, int targetH)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);

        RenderTexture.active = rt;
        Texture2D result = new Texture2D(targetW, targetH, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
        result.Apply();
        RenderTexture.active = null;

        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}
