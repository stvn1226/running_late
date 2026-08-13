using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [System.Serializable]
    public class VFXPoolConfig
    {
        public string vfxId;
        public ParticleSystem prefab;
        [Tooltip("Sorting Layer name to use. Must match a Sorting Layer in your project (e.g. 'Player', 'Foreground').")]
        public string sortingLayerName = "Default";
        [Tooltip("Sorting Order within the layer. Use a high value to render on top of sprites.")]
        public int sortingOrder = 10;
        public int defaultCapacity = 10;
        public int maxSize = 50;
    }

    [SerializeField] private List<VFXPoolConfig> poolConfigs = new List<VFXPoolConfig>();
    [SerializeField] private bool debugMode = false;

    private Dictionary<string, ObjectPool<ParticleSystem>> pools = new Dictionary<string, ObjectPool<ParticleSystem>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();

            if (debugMode)
                GameLogger.Log("VFXManager", $"Initialized with {poolConfigs.Count} pool configs. DontDestroyOnLoad set.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        foreach (var config in poolConfigs)
        {
            if (config.prefab == null)
            {
                GameLogger.LogWarning("VFXManager", $"Pool config '{config.vfxId}' has no prefab assigned! Skipping.");
                continue;
            }

            // Capture config locally to avoid closure issues in the lambda
            var capturedConfig = config;

            var pool = new ObjectPool<ParticleSystem>(
                createFunc: () =>
                {
                    ParticleSystem ps = Instantiate(capturedConfig.prefab, transform);

                    // Force stop action to Callback so the pool recycles it
                    var main = ps.main;
                    main.stopAction = ParticleSystemStopAction.Callback;

                    // Apply sorting layer so particles render above game sprites
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingLayerName = capturedConfig.sortingLayerName;
                        renderer.sortingOrder = capturedConfig.sortingOrder;
                    }

                    var returner = ps.gameObject.GetComponent<ReturnToPool>() 
                                   ?? ps.gameObject.AddComponent<ReturnToPool>();
                    returner.Initialize(capturedConfig.vfxId);

                    return ps;
                },
                actionOnGet: (ps) =>
                {
                    ps.gameObject.SetActive(true);
                    ps.Clear();
                },
                actionOnRelease: (ps) =>
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.gameObject.SetActive(false);
                },
                actionOnDestroy: (ps) => Destroy(ps.gameObject),
                collectionCheck: false,
                defaultCapacity: capturedConfig.defaultCapacity,
                maxSize: capturedConfig.maxSize
            );

            pools[capturedConfig.vfxId] = pool;

            if (debugMode)
                GameLogger.Log("VFXManager", $"Pool created: '{capturedConfig.vfxId}' | Layer: '{capturedConfig.sortingLayerName}' | Order: {capturedConfig.sortingOrder}");
        }
    }

    public ParticleSystem SpawnVFX(string vfxId, Vector3 position, Quaternion rotation = default)
    {
        if (!pools.TryGetValue(vfxId, out var pool))
        {
            GameLogger.LogWarning("VFXManager", $"SpawnVFX: Pool '{vfxId}' not found. Did you add it to VFXManager's Pool Configs?");
            return null;
        }

        ParticleSystem ps = pool.Get();
        ps.transform.position = position;
        ps.transform.rotation = rotation == default ? Quaternion.identity : rotation;
        ps.Play();

        if (debugMode)
            GameLogger.Log("VFXManager", $"SpawnVFX: '{vfxId}' at {position}");

        return ps;
    }

    public void ReturnToPool(string vfxId, ParticleSystem ps)
    {
        if (pools.TryGetValue(vfxId, out var pool))
        {
            pool.Release(ps);
        }
    }
}

[RequireComponent(typeof(ParticleSystem))]
public class ReturnToPool : MonoBehaviour
{
    private string vfxId;
    private ParticleSystem ps;

    public void Initialize(string id)
    {
        vfxId = id;
        ps = GetComponent<ParticleSystem>();
    }

    private void OnParticleSystemStopped()
    {
        if (VFXManager.Instance != null && !string.IsNullOrEmpty(vfxId))
        {
            VFXManager.Instance.ReturnToPool(vfxId, ps);
        }
    }
}
