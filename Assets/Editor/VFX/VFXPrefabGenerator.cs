#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor utility to auto-generate VFX Particle System prefabs.
/// Run via: Tools -> VFX -> Generate All VFX Prefabs
/// </summary>
public static class VFXPrefabGenerator
{
    private const string PREFAB_OUTPUT_PATH = "Assets/Art/VFX/Particles";

    [MenuItem("Tools/VFX/Generate All VFX Prefabs")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(PREFAB_OUTPUT_PATH);
        AssetDatabase.Refresh();

        CreateRunDust();
        CreateLandDust();
        CreateJumpDust();
        CreateDoubleJumpDust();
        CreateToastBreak();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[VFXPrefabGenerator] ✅ All VFX prefabs generated at: " + PREFAB_OUTPUT_PATH);
        EditorUtility.DisplayDialog(
            "VFX Generator",
            "✅ 5 particle prefabs created in:\n" + PREFAB_OUTPUT_PATH +
            "\n\nRemember to assign them to your VFXManager in the scene!",
            "OK"
        );
    }

    // -----------------------------------------------------------------------
    // RunDust — small horizontal dust puffs, emitted continuously while running
    // -----------------------------------------------------------------------
    private static void CreateRunDust()
    {
        GameObject go = new GameObject("RunDust");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        // Main module
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.Callback;
        main.duration = 0.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.78f, 0.65f, 0.6f),
            new Color(0.7f, 0.65f, 0.55f, 0.3f)
        );
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 15;

        // Emission — small burst
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 4, 6) });

        // Shape — small flat cone at ground level
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.05f;
        shape.rotation = new Vector3(-90f, 0f, 0f); // Pointing sideways

        // Velocity over lifetime — drift outward
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        vel.y = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        vel.space = ParticleSystemSimulationSpace.World;

        // Size over lifetime — shrink to nothing
        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, shrinkCurve);

        // Color over lifetime — fade out
        var colorOL = ps.colorOverLifetime;
        colorOL.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOL.color = new ParticleSystem.MinMaxGradient(g);

        // Renderer — use default sprite
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.sortingOrder = 10;
        psr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");

        SavePrefab(go, "RunDust");
    }

    // -----------------------------------------------------------------------
    // LandDust — bigger horizontal burst when player touches the ground
    // -----------------------------------------------------------------------
    private static void CreateLandDust()
    {
        GameObject go = new GameObject("LandDust");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.Callback;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.78f, 0.65f, 0.8f),
            new Color(0.6f, 0.55f, 0.45f, 0.4f)
        );
        main.gravityModifier = 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 16) });

        // Shape — wide flat disc, spreads sideways on landing
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-3f, 3f);
        vel.y = new ParticleSystem.MinMaxCurve(0.2f, 1.0f);
        vel.space = ParticleSystemSimulationSpace.World;

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var colorOL = ps.colorOverLifetime;
        colorOL.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOL.color = new ParticleSystem.MinMaxGradient(g);

        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.sortingOrder = 10;

        SavePrefab(go, "LandDust");
    }

    // -----------------------------------------------------------------------
    // JumpDust — quick downward burst when player lifts off
    // -----------------------------------------------------------------------
    private static void CreateJumpDust()
    {
        GameObject go = new GameObject("JumpDust");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.Callback;
        main.duration = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.78f, 0.65f, 0.9f),
            new Color(0.7f, 0.65f, 0.55f, 0.5f)
        );
        main.gravityModifier = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 12;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 10) });

        // Shape — hemisphere facing down
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.2f;
        shape.rotation = new Vector3(180f, 0f, 0f);

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var colorOL = ps.colorOverLifetime;
        colorOL.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOL.color = new ParticleSystem.MinMaxGradient(g);

        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.sortingOrder = 10;

        SavePrefab(go, "JumpDust");
    }

    // -----------------------------------------------------------------------
    // DoubleJumpDust — energy ring burst, distinct from normal jump
    // -----------------------------------------------------------------------
    private static void CreateDoubleJumpDust()
    {
        GameObject go = new GameObject("DoubleJumpDust");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.Callback;
        main.duration = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        // Electric/energy teal color to distinguish from normal jump
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.3f, 0.9f, 1.0f, 1.0f),
            new Color(0.1f, 0.6f, 0.9f, 0.7f)
        );
        main.gravityModifier = -0.1f; // Slight upward float for energy feel
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12, 18) });

        // Shape — ring shape: circle edge so particles fly outward in a ring
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.25f;
        shape.radiusThickness = 0f; // 0 = only emit from edge (ring)
        shape.rotation = new Vector3(90f, 0f, 0f);

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        // Fade from bright teal to transparent
        var colorOL = ps.colorOverLifetime;
        colorOL.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.5f, 1f, 1f), 0f),
                new GradientColorKey(new Color(0.1f, 0.5f, 1f), 1f)
            },
            new[] { new GradientAlphaKey(1.0f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOL.color = new ParticleSystem.MinMaxGradient(g);

        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.sortingOrder = 11;
        psr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");

        SavePrefab(go, "DoubleJumpDust");
    }

    // -----------------------------------------------------------------------
    // ToastBreak — toast crumb explosion when player takes first hit
    // -----------------------------------------------------------------------
    private static void CreateToastBreak()
    {
        GameObject go = new GameObject("ToastBreak");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.Callback;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 6.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startRotation3D = false;
        // Warm toasted bread color palette
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.95f, 0.75f, 0.3f, 1.0f),  // golden toast
            new Color(0.7f, 0.45f, 0.15f, 1.0f)    // darker crust
        );
        main.gravityModifier = 1.5f; // Falls fast like bread crumbs
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 14) });

        // Shape — full sphere, explodes in all directions
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Rotation over lifetime — spin the crumbs for realism
        var rotOL = ps.rotationOverLifetime;
        rotOL.enabled = true;
        rotOL.z = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var colorOL = ps.colorOverLifetime;
        colorOL.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f),
                new GradientColorKey(new Color(0.6f, 0.35f, 0.1f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOL.color = new ParticleSystem.MinMaxGradient(g);

        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.sortingOrder = 12;
        psr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");

        SavePrefab(go, "ToastBreak");
    }

    // -----------------------------------------------------------------------
    // Helper — saves the given GameObject as a prefab and destroys temp GO
    // -----------------------------------------------------------------------
    private static void SavePrefab(GameObject go, string prefabName)
    {
        string path = $"{PREFAB_OUTPUT_PATH}/{prefabName}.prefab";
        bool success;
        PrefabUtility.SaveAsPrefabAsset(go, path, out success);
        Object.DestroyImmediate(go);

        if (success)
            Debug.Log($"[VFXPrefabGenerator] ✅ Created: {path}");
        else
            Debug.LogError($"[VFXPrefabGenerator] ❌ Failed to create: {path}");
    }
}
#endif
