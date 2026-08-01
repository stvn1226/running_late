using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Cross-platform file storage for Challenge Card PNGs.
/// Resolves output paths without assuming any platform-specific folder structure.
/// Has no dependency on Steam.
///
/// Priority order for output directory:
///   1. Environment.SpecialFolder.MyPictures / {folder} / {subfolder}
///      - Works on Windows (%USERPROFILE%\Pictures) and most Linux desktops (~/Pictures).
///      - On Steam Deck this resolves to ~/Pictures in both Desktop and Gaming Mode.
///   2. Application.persistentDataPath / Challenges
///      - Always writable in all environments Unity supports.
///      - Used if Pictures is unavailable, empty, or not writable.
///
/// All path operations use Path.Combine — no manual separators.
/// </summary>
public static class ChallengeCardStorageService
{
    // ─── Directory Resolution ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the best available output directory for the current platform.
    /// Creates the directory if it does not exist.
    /// Sets <paramref name="usedFallback"/> to true if the primary path was unavailable.
    /// Returns null only if no directory could be made writable (edge case on locked-down systems).
    /// </summary>
    public static string ResolveOutputDirectory(ChallengeConfig config, out bool usedFallback)
    {
        usedFallback = false;

        string primary = TryResolvePrimary(config);
        if (primary != null)
            return primary;

        string fallback = TryResolveFallback();
        if (fallback != null)
        {
            usedFallback = true;
            return fallback;
        }

        GameLogger.LogError("CHALLENGE_STORAGE", "No writable output directory could be resolved.");
        return null;
    }

    // ─── Safe File Naming ─────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a unique file path using the provided base name.
    /// Adds a timestamp suffix if the file already exists to avoid silent overwrites.
    /// The resulting filename is valid on both Windows and Linux.
    /// </summary>
    public static string BuildSafeFilePath(string directory, string baseName)
    {
        string candidate = Path.Combine(directory, baseName + ".png");

        if (!File.Exists(candidate))
            return candidate;

        // Append a short timestamp to avoid overwriting an existing card
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        candidate = Path.Combine(directory, $"{baseName}_{timestamp}.png");

        // Absolute fallback: incrementing counter
        int counter = 1;
        while (File.Exists(candidate) && counter < 100)
        {
            candidate = Path.Combine(directory, $"{baseName}_{timestamp}_{counter:D2}.png");
            counter++;
        }

        return candidate;
    }

    // ─── Write PNG ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes the PNG bytes to disk.
    /// Returns true on success. Sets <paramref name="savedPath"/> to the written file path.
    /// </summary>
    public static bool SavePng(byte[] bytes, string filePath, out string savedPath)
    {
        savedPath = null;
        try
        {
            File.WriteAllBytes(filePath, bytes);
            savedPath = filePath;
            GameLogger.Log("CHALLENGE_STORAGE", $"Challenge Card saved: {Path.GetFileName(filePath)}");
            return true;
        }
        catch (Exception e)
        {
            GameLogger.LogError("CHALLENGE_STORAGE", $"Failed to write PNG: {e.Message}");
            return false;
        }
    }

    // ─── Open Folder (optional, non-critical) ─────────────────────────────────────

    /// <summary>
    /// Attempts to open the output folder in the platform's file manager.
    /// Non-critical: failures are silently logged without interrupting the user experience.
    /// May not function in Steam Deck Gaming Mode — this is expected behaviour.
    /// </summary>
    public static void TryOpenFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            GameLogger.LogWarning("CHALLENGE_STORAGE", "TryOpenFolder: path is invalid or does not exist.");
            return;
        }

        try
        {
#if UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start("explorer.exe",
                $"\"{path.Replace('/', '\\')}\"");
#elif UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", $"\"{path}\"");
#elif UNITY_STANDALONE_LINUX
            // xdg-open works on Steam Deck Desktop Mode and major Linux desktops.
            // It may fail silently in Steam Deck Gaming Mode — that is acceptable.
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName               = "xdg-open",
                Arguments              = $"\"{path}\"",
                UseShellExecute        = false,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            };
            System.Diagnostics.Process.Start(psi);
#else
            GameLogger.Log("CHALLENGE_STORAGE", "TryOpenFolder: unsupported platform.");
#endif
        }
        catch (Exception e)
        {
            // Non-critical — the file is already saved even if the folder cannot be opened.
            GameLogger.LogWarning("CHALLENGE_STORAGE", $"TryOpenFolder failed (non-critical): {e.Message}");
        }
    }

    // ─── Internal ─────────────────────────────────────────────────────────────────

    private static string TryResolvePrimary(ChallengeConfig config)
    {
        try
        {
            string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            // GetFolderPath can return an empty string on some Linux configurations
            if (string.IsNullOrEmpty(pictures) || !Directory.Exists(pictures))
                return null;

            string path = Path.Combine(pictures, config.outputFolderName, config.outputSubfolderName);

            EnsureDirectory(path);

            return CanWriteTo(path) ? path : null;
        }
        catch (Exception e)
        {
            GameLogger.LogWarning("CHALLENGE_STORAGE", $"Primary path unavailable: {e.Message}");
            return null;
        }
    }

    private static string TryResolveFallback()
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "Challenges");
            EnsureDirectory(path);
            return CanWriteTo(path) ? path : null;
        }
        catch (Exception e)
        {
            GameLogger.LogWarning("CHALLENGE_STORAGE", $"Fallback path unavailable: {e.Message}");
            return null;
        }
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    /// <summary>Verifies write access by creating and deleting a temporary test file.</summary>
    private static bool CanWriteTo(string path)
    {
        try
        {
            string testFile = Path.Combine(path, ".rl_write_test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
