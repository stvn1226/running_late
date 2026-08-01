using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generates the Challenge Card PNG by compositing a gameplay screenshot
/// with the challenge information overlay panel.
///
/// ── Architecture ─────────────────────────────────────────────────────────────
///   Input:  ChallengeData + Texture2D (captured by ChallengeCardCaptureService)
///   Output: PNG file on disk, optional Steam screenshot, optional clipboard copy
///
///   The card layout is:
///     ┌────────────────────┐  ← outputHeight
///     │  Gameplay (top)    │  ← ~73% — center-cropped to 16:9
///     ├────────────────────┤
///     │  Info Overlay (btm)│  ← ~27% — rendered from ChallengeCardOverlay prefab
///     └────────────────────┘
///
/// ── Aspect Ratio Handling ────────────────────────────────────────────────────
///   The gameplay capture may be any aspect ratio. The card output is always 16:9.
///   Strategy: Center crop.
///     - 16:9 source:  no crop, full frame used.
///     - 16:10 source (Steam Deck 1280x800): crops equal amounts from top and bottom.
///     - Ultra-wide source: crops equal amounts from left and right.
///   The crop never deforms the image.
///
/// ── Inspector Setup Required ─────────────────────────────────────────────────
///   1. Assign 'config' (ChallengeConfig ScriptableObject).
///   2. Assign 'overlayCamera':
///        - A Camera component in the scene dedicated to overlay rendering.
///        - clearFlags: Solid Color (background matches overlayBackgroundColor).
///        - cullingMask: Only the layer used by the ChallengeCardOverlay Canvas.
///        - Projection: Orthographic.
///        - Do NOT render the main game scene layers.
///   3. Set ChallengeConfig.challengeCardOverlayPrefab to the prefab with
///      ChallengeCardOverlay and a Canvas (renderMode = ScreenSpaceCamera).
/// </summary>
public class ChallengeCardGenerator : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ChallengeConfig config;

    // ─── State ────────────────────────────────────────────────────────────────────

    public ChallengeCardState CurrentState { get; private set; } = ChallengeCardState.Idle;
    public string LastSavedPath { get; private set; }

    // ─── Events ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when generation completes.
    /// success: true if the PNG was written to disk successfully.
    /// message: absolute path on success, error description on failure.
    /// </summary>
    public event Action<bool, string> OnGenerationComplete;

    // ─── Internal ─────────────────────────────────────────────────────────────────

    private Coroutine _activeCoroutine;

    // ─── Public API ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts the generation pipeline. Ignored if already running.
    /// Listen to OnGenerationComplete for the result.
    /// </summary>
    public void RequestGeneration(ChallengeData data, Texture2D gameplayCapture)
    {
        if (CurrentState == ChallengeCardState.Generating)
        {
            GameLogger.LogWarning("CHALLENGE_GENERATOR", "Generation already in progress — request ignored.");
            return;
        }

        if (config == null)
        {
            GameLogger.LogError("CHALLENGE_GENERATOR", "ChallengeConfig is not assigned.");
            return;
        }

        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        _activeCoroutine = StartCoroutine(GenerateRoutine(data, gameplayCapture));
    }

    /// <summary>Resets state to Idle. Call this if you want to re-enable the Create button after a failure.</summary>
    public void ResetState()
    {
        CurrentState  = ChallengeCardState.Idle;
        LastSavedPath = null;
    }

    // ─── Generation Pipeline ──────────────────────────────────────────────────────

    private IEnumerator GenerateRoutine(ChallengeData data, Texture2D gameplayCapture)
    {
        CurrentState = ChallengeCardState.Generating;
        ChallengeAnalyticsEvents.TrackCardRequested(data);

        int outW     = config.outputWidth;
        int outH     = config.outputHeight;
        int overlayH = Mathf.RoundToInt(outH * config.overlayHeightFraction);
        int gameplayH = outH - overlayH;

        // ── Step 1: Create a temporary camera and setup the overlay ───────────────

        GameObject overlayObj = null;
        RenderTexture overlayRT = null;
        Camera tempCam = null;

        if (config.challengeCardOverlayPrefab != null)
        {
            try
            {
                // Create a temporary camera for rendering the overlay
                GameObject camObj = new GameObject("TempChallengeCamera");
                tempCam = camObj.AddComponent<Camera>();
                tempCam.cameraType = CameraType.Preview;
                tempCam.orthographic = true;
                tempCam.clearFlags = CameraClearFlags.SolidColor;
                tempCam.backgroundColor = config.overlayBackgroundColor;
                tempCam.cullingMask = 1 << 5; // 'UI' layer by default
                tempCam.enabled = false;

                // Instantiate off-screen
                overlayObj = Instantiate(config.challengeCardOverlayPrefab);
                
                // Move it far away so it doesn't accidentally flash on screen if it was WorldSpace
                overlayObj.transform.position = new Vector3(9999f, 9999f, 9999f);

                // Force the canvas to be ScreenSpaceCamera and use our temp camera
                var canvas = overlayObj.GetComponentInChildren<Canvas>();
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = tempCam;
                    canvas.planeDistance = 10f; // Ensure it's in front of the camera
                    
                    // Force the canvas layer to UI just in case
                    canvas.gameObject.layer = 5; 
                }

                // Populate the text fields
                var overlayCtrl = overlayObj.GetComponentInChildren<ChallengeCardOverlay>();
                if (overlayCtrl != null)
                {
                    overlayCtrl.Populate(data, config);
                }
            }
            catch (Exception e)
            {
                GameLogger.LogError("CHALLENGE_GENERATOR", $"Overlay prefab setup failed: {e.Message}");
                CleanupOverlay(ref overlayObj, ref overlayRT, ref tempCam);
                Fail(data, "overlay_setup", "prefab_instantiation_failed");
                yield break;
            }
        }
        else
        {
            GameLogger.LogWarning("CHALLENGE_GENERATOR",
                "No overlay prefab assigned. Card will be generated without the info panel.");
        }

        // Wait for Canvas layout to complete before rendering
        yield return null;
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        // ── Step 2: Render overlay to RenderTexture ───────────────────────────────

        Texture2D overlayTex = null;

        if (overlayObj != null && tempCam != null)
        {
            try
            {
                overlayRT = new RenderTexture(outW, overlayH, 0, RenderTextureFormat.ARGB32);
                tempCam.targetTexture = overlayRT;
                tempCam.Render();
                tempCam.targetTexture = null;

                RenderTexture.active = overlayRT;
                overlayTex = new Texture2D(outW, overlayH, TextureFormat.RGBA32, false);
                overlayTex.ReadPixels(new Rect(0, 0, outW, overlayH), 0, 0);
                overlayTex.Apply();
                RenderTexture.active = null;
            }
            catch (Exception e)
            {
                GameLogger.LogError("CHALLENGE_GENERATOR", $"Overlay render failed: {e.Message}");
                CleanupOverlay(ref overlayObj, ref overlayRT, ref tempCam);
                if (overlayTex != null) { Destroy(overlayTex); overlayTex = null; }
                Fail(data, "overlay_render", "render_texture_failed");
                yield break;
            }
        }

        CleanupOverlay(ref overlayObj, ref overlayRT, ref tempCam);

        // ── Step 3: Scale and crop gameplay to top portion ────────────────────────

        Texture2D gameplayPortion = null;

        if (gameplayCapture != null)
        {
            try
            {
                gameplayPortion = ScaleAndCropToAspect(gameplayCapture, outW, gameplayH);
            }
            catch (Exception e)
            {
                GameLogger.LogError("CHALLENGE_GENERATOR", $"Gameplay scale/crop failed: {e.Message}");
                if (overlayTex != null) Destroy(overlayTex);
                Fail(data, "gameplay_scale", "texture_crop_failed");
                yield break;
            }
        }

        // ── Step 4: Compose the final Texture2D ───────────────────────────────────

        Texture2D finalTex = null;

        try
        {
            finalTex = Compose(gameplayPortion, overlayTex, outW, outH, gameplayH, overlayH,
                config.overlayBackgroundColor);
        }
        catch (Exception e)
        {
            GameLogger.LogError("CHALLENGE_GENERATOR", $"Composition failed: {e.Message}");
            if (gameplayPortion != null) Destroy(gameplayPortion);
            if (overlayTex     != null) Destroy(overlayTex);
            Fail(data, "composition", "pixel_composition_failed");
            yield break;
        }
        finally
        {
            if (gameplayPortion != null) Destroy(gameplayPortion);
            if (overlayTex     != null) Destroy(overlayTex);
        }

        // ── Step 5: Encode to PNG ─────────────────────────────────────────────────

        byte[] pngBytes = null;

        try
        {
            pngBytes = finalTex.EncodeToPNG();
        }
        catch (Exception e)
        {
            GameLogger.LogError("CHALLENGE_GENERATOR", $"PNG encoding failed: {e.Message}");
            Fail(data, "encode", "png_encode_exception");
            Destroy(finalTex);
            yield break;
        }
        finally
        {
            if (finalTex != null) Destroy(finalTex);
        }

        if (pngBytes == null || pngBytes.Length == 0)
        {
            Fail(data, "encode", "png_bytes_empty");
            yield break;
        }

        // ── Step 6: Save to disk ──────────────────────────────────────────────────

        string directory = ChallengeCardStorageService.ResolveOutputDirectory(config, out bool usedFallback);

        if (string.IsNullOrEmpty(directory))
        {
            Fail(data, "storage_resolve", "no_writable_directory");
            yield break;
        }

        string baseName  = config.BuildSafeFileName(data);
        string filePath  = ChallengeCardStorageService.BuildSafeFilePath(directory, baseName);
        bool   saved     = ChallengeCardStorageService.SavePng(pngBytes, filePath, out string savedPath);

        if (!saved)
        {
            Fail(data, "storage_write", "file_write_failed");
            yield break;
        }

        // ── Step 7: Success ───────────────────────────────────────────────────────

        LastSavedPath = savedPath;
        CurrentState  = ChallengeCardState.Success;

        ChallengeAnalyticsEvents.TrackCardCreated(data, outW, outH, SteamManager.IsInitialized, usedFallback);

        // Auto-copy share text if configured
        if (config.autoCopyTextOnCardCreate)
            ChallengeShareService.CopyShareText(data);

        // Attempt Steam screenshot upload (non-critical, never blocks success)
        if (config.attemptSteamScreenshot)
            ChallengeShareService.TryAddToSteam(savedPath, outW, outH);

        OnGenerationComplete?.Invoke(true, savedPath);
    }

    // ─── Image Processing ─────────────────────────────────────────────────────────

    /// <summary>
    /// Center-crops the source texture to the target aspect ratio, then scales to target dimensions.
    ///
    /// 16:9  source → no crop.
    /// 16:10 source (Steam Deck) → crops top and bottom equally to produce 16:9.
    /// Ultra-wide source → crops left and right equally.
    ///
    /// Uses GPU blitting (Graphics.Blit with UV scale/offset) — preserves quality.
    /// </summary>
    private Texture2D ScaleAndCropToAspect(Texture2D source, int targetW, int targetH)
    {
        float srcAspect = (float)source.width / source.height;
        float dstAspect = (float)targetW / targetH;

        // UV scale and offset for center-crop (Blit convention: scale shrinks the sampled region)
        float scaleU  = 1f, scaleV  = 1f;
        float offsetU = 0f, offsetV = 0f;

        if (srcAspect > dstAspect + 0.001f)
        {
            // Source is wider than target: crop horizontally (keep full height, trim sides)
            scaleU  = dstAspect / srcAspect;
            offsetU = (1f - scaleU) / 2f;
        }
        else if (srcAspect < dstAspect - 0.001f)
        {
            // Source is taller than target (e.g. 16:10 on Steam Deck): crop vertically, keep full width
            scaleV  = srcAspect / dstAspect;
            offsetV = (1f - scaleV) / 2f;
        }
        // Equal aspect: scaleU/V remain 1, no crop

        RenderTexture rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt, new Vector2(scaleU, scaleV), new Vector2(offsetU, offsetV));

        RenderTexture.active = rt;
        Texture2D result = new Texture2D(targetW, targetH, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
        result.Apply();
        RenderTexture.active = null;

        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    /// <summary>
    /// Combines gameplay (top) and overlay (bottom) into a single Texture2D.
    /// Unity's Texture2D uses a bottom-left origin, so:
    ///   - Overlay pixels → SetPixels at y = 0 (bottom)
    ///   - Gameplay pixels → SetPixels at y = overlayH (above overlay)
    /// </summary>
    private Texture2D Compose(
        Texture2D gameplay, Texture2D overlay,
        int outW, int outH, int gameplayH, int overlayH,
        Color fallbackOverlayColor)
    {
        Texture2D result = new Texture2D(outW, outH, TextureFormat.RGB24, false);

        // Base fill (visible only if both gameplay and overlay are missing)
        Color[] base_ = new Color[outW * outH];
        Array.Fill(base_, Color.black);
        result.SetPixels(base_);

        // Top: gameplay
        if (gameplay != null)
        {
            result.SetPixels(0, overlayH, outW, gameplayH, gameplay.GetPixels());
        }

        // Bottom: overlay (alpha-blended over fallback color to produce opaque RGB)
        if (overlay != null)
        {
            Color[] src     = overlay.GetPixels();
            Color[] blended = new Color[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                float a = src[i].a;
                blended[i] = new Color(
                    Mathf.LerpUnclamped(fallbackOverlayColor.r, src[i].r, a),
                    Mathf.LerpUnclamped(fallbackOverlayColor.g, src[i].g, a),
                    Mathf.LerpUnclamped(fallbackOverlayColor.b, src[i].b, a),
                    1f);
            }
            result.SetPixels(0, 0, outW, overlayH, blended);
        }
        else
        {
            // No overlay rendered: fill bottom band with the configured background color
            Color[] solid = new Color[outW * overlayH];
            Array.Fill(solid, new Color(
                fallbackOverlayColor.r, fallbackOverlayColor.g, fallbackOverlayColor.b, 1f));
            result.SetPixels(0, 0, outW, overlayH, solid);
        }

        result.Apply();
        return result;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────

    private void Fail(ChallengeData data, string stage, string category)
    {
        CurrentState = ChallengeCardState.Failed;
        ChallengeAnalyticsEvents.TrackCardFailed(stage, category);
        OnGenerationComplete?.Invoke(false, $"Failed at stage: {stage} ({category})");
    }

    private void CleanupOverlay(ref GameObject obj, ref RenderTexture rt, ref Camera tempCam)
    {
        if (obj != null) { Destroy(obj); obj = null; }
        if (rt  != null) { rt.Release(); rt  = null; }
        if (tempCam != null) { Destroy(tempCam.gameObject); tempCam = null; }

        RenderTexture.active = null;
    }
}

/// <summary>
/// Tracks the current state of the ChallengeCardGenerator.
/// Used by VictoryPanel to manage button interactability and UI feedback.
/// </summary>
public enum ChallengeCardState
{
    Idle,
    Generating,
    Success,
    Failed
}
