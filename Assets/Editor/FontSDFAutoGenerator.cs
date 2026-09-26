// Assets/Editor/FontSDFAutoGenerator.cs
// Automatically regenerates missing TMP SDF font assets when Unity opens.
// This ensures Windows machines that don't have the local .asset files
// (which are gitignored) get valid font assets without any manual steps.
//
// How it works:
// - .meta files (GUIDs) ARE tracked in git → prefab references stay valid
// - .asset files are NOT tracked (local-generated) → might be missing on fresh pull
// - This script detects missing assets and recreates them from the .ttf source
// - Uses AtlasPopulationMode.Dynamic: atlas starts empty and fills characters
//   on-demand at runtime → correct for Japanese/CJK without a massive pre-baked atlas

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;

[InitializeOnLoad]
public static class FontSDFAutoGenerator
{
    // (ttfPath, sdfAssetPath)
    // The .meta for each sdfAssetPath must exist in git so the GUID is preserved.
    private static readonly (string ttf, string sdf)[] FontPairs = new[]
    {
        // NotoSansJP — Variable weight
        ("Assets/Fonts/NotoSansJP-VariableFont_wght.ttf",
         "Assets/Fonts/NotoSansJP-VariableFont_wght SDF.asset"),

        // NotoSansJP — Static weights
        ("Assets/Fonts/static/NotoSansJP-Black.ttf",       "Assets/Fonts/static/NotoSansJP-Black SDF.asset"),
        ("Assets/Fonts/static/NotoSansJP-Bold.ttf",        "Assets/Fonts/static/NotoSansJP-Bold SDF.asset"),
        ("Assets/Fonts/static/NotoSansJP-ExtraBold.ttf",   "Assets/Fonts/static/NotoSansJP-ExtraBold SDF.asset"),
        ("Assets/Fonts/static/NotoSansJP-ExtraLight.ttf",  "Assets/Fonts/static/NotoSansJP-ExtraLight SDF.asset"),
        ("Assets/Fonts/static/NotoSansJP-Light.ttf",       "Assets/Fonts/static/NotoSansJP-Light SDF.asset"),
        ("Assets/Fonts/static/NotoSansJP-Medium.ttf",      "Assets/Fonts/static/NotoSansJP-Medium SDF.asset"),
        ("Assets/Fonts/static/NotoSansJP-Regular.ttf",     "Assets/Fonts/static/NotoSansJP-Regular SDF.asset"),
        ("Assets/Fonts/static/NotoSansJP-SemiBold.ttf",    "Assets/Fonts/static/NotoSansJP-SemiBold SDF.asset"),
        ("Assets/Fonts/static/NotoSansJP-Thin.ttf",        "Assets/Fonts/static/NotoSansJP-Thin SDF.asset"),

        // Roboto
        ("Assets/Fonts/Roboto-Black.ttf",       "Assets/Fonts/Roboto-Black SDF.asset"),
        ("Assets/Fonts/Roboto-Bold.ttf",        "Assets/Fonts/Roboto-Bold SDF.asset"),
        ("Assets/Fonts/Roboto-ExtraBold.ttf",   "Assets/Fonts/Roboto-ExtraBold SDF.asset"),
        ("Assets/Fonts/Roboto-ExtraLight.ttf",  "Assets/Fonts/Roboto-ExtraLight SDF.asset"),
        ("Assets/Fonts/Roboto-Light.ttf",       "Assets/Fonts/Roboto-Light SDF.asset"),
        ("Assets/Fonts/Roboto-Medium.ttf",      "Assets/Fonts/Roboto-Medium SDF.asset"),
        ("Assets/Fonts/Roboto-Regular.ttf",     "Assets/Fonts/Roboto-Regular SDF.asset"),
        ("Assets/Fonts/Roboto-SemiBold.ttf",    "Assets/Fonts/Roboto-SemiBold SDF.asset"),
        ("Assets/Fonts/Roboto-Thin.ttf",        "Assets/Fonts/Roboto-Thin SDF.asset"),

        // GN-KillGothic
        ("Assets/Sprites/Art/UI/Font/GN-KillGothic-U-KanaNA.ttf",
         "Assets/Sprites/Art/UI/Font/GN-KillGothic-U-KanaNA SDF.asset"),
        ("Assets/Sprites/Art/UI/Font/GN-KillGothic-U-KanaNA.ttf",
         "Assets/Sprites/Art/UI/Font/GN-KillGothic-U-KanaNA SDF 1.asset"),

        // Might Makes Right BB
        ("Assets/Sprites/Art/UI/Font/Might Makes Right/Might Makes Right BB.ttf",
         "Assets/Sprites/Art/UI/Font/Might Makes Right BB SDF.asset"),
        ("Assets/Sprites/Art/UI/Font/Might Makes Right/Might Makes Right BB.ttf",
         "Assets/Sprites/Art/UI/Font/Might Makes Right BB SDF 1.asset"),
        ("Assets/Sprites/Art/UI/Font/Might Makes Right/Might Makes Right BB.ttf",
         "Assets/Sprites/Art/UI/Font/Might Makes Right/Might Makes Right BB SDF.asset"),
        ("Assets/Sprites/Art/UI/Font/Might Makes Right/Might Makes Right BB.ttf",
         "Assets/Sprites/Art/UI/Font/Might Makes Right/Might Makes Right BB SDF 1.asset"),
        ("Assets/Sprites/Art/UI/Font/Might Makes Right/Might Makes Right BB.ttf",
         "Assets/Sprites/Art/UI/Font/Might Makes Right/Might Makes Right BB SDF 2.asset"),
    };

    static FontSDFAutoGenerator()
    {
        // Delay so the AssetDatabase is fully loaded before we try to create assets
        EditorApplication.delayCall += CheckAndGenerateMissingFonts;
    }

    private static void CheckAndGenerateMissingFonts()
    {
        bool anyGenerated = false;

        foreach (var (ttfPath, sdfPath) in FontPairs)
        {
            if (File.Exists(sdfPath)) continue;

            if (!File.Exists(ttfPath))
            {
                Debug.LogWarning($"[FontSDFAutoGenerator] Source TTF missing, skipping: {ttfPath}");
                continue;
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"[FontSDFAutoGenerator] Could not load font: {ttfPath}");
                continue;
            }

            // Dynamic mode: atlas starts empty, characters rasterized on-demand at runtime.
            // Correct for Japanese/CJK — a static atlas would need 20,000+ pre-baked glyphs.
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true
            );

            if (fontAsset == null)
            {
                Debug.LogWarning($"[FontSDFAutoGenerator] Failed to create SDF asset for: {ttfPath}");
                continue;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(sdfPath);

            // CreateAsset picks up the existing .meta GUID automatically,
            // so all prefab/material references stay valid without any manual fix.
            AssetDatabase.CreateAsset(fontAsset, sdfPath);
            Debug.Log($"[FontSDFAutoGenerator] Generated: {sdfPath}");
            anyGenerated = true;
        }

        if (anyGenerated)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FontSDFAutoGenerator] Done. All missing font SDF assets regenerated.");
        }
    }
}
#endif
