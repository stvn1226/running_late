using UnityEngine;
using UnityEngine.Pool;

public class GhostTrail : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private bool enableTint = true;
    [SerializeField] private float spawnInterval = 0.1f;
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.5f);
    
    private ObjectPool<SpriteRenderer> ghostPool;
    private float spawnTimer;
    private bool isEmitting = false;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        ghostPool = new ObjectPool<SpriteRenderer>(
            createFunc: () => {
                GameObject obj = new GameObject("GhostSprite");
                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                
                var fader = obj.AddComponent<GhostFader>();
                fader.Initialize(this, fadeSpeed);
                
                return sr;
            },
            actionOnGet: (sr) => sr.gameObject.SetActive(true),
            actionOnRelease: (sr) => sr.gameObject.SetActive(false),
            actionOnDestroy: (sr) => Destroy(sr.gameObject),
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 20
        );
    }

    public void EnableTrail(bool enable)
    {
        isEmitting = enable;
        spawnTimer = 0f;
    }

    private void Update()
    {
        if (!isEmitting || targetRenderer == null) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnGhost();
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnGhost()
    {
        SpriteRenderer ghost = ghostPool.Get();
        ghost.transform.position = targetRenderer.transform.position;
        ghost.transform.rotation = targetRenderer.transform.rotation;
        ghost.transform.localScale = targetRenderer.transform.lossyScale;
        
        ghost.sprite = targetRenderer.sprite;
        ghost.flipX = targetRenderer.flipX;
        ghost.flipY = targetRenderer.flipY;
        
        if (enableTint)
        {
            if (ghostMaterial != null) ghost.sharedMaterial = ghostMaterial;
            ghost.color = ghostColor;
        }
        else
        {
            ghost.sharedMaterial = targetRenderer.sharedMaterial;
            Color originalColor = targetRenderer.color;
            originalColor.a = ghostColor.a; // Use the alpha from ghostColor as starting alpha
            ghost.color = originalColor;
        }
        
        ghost.sortingLayerID = targetRenderer.sortingLayerID;
        ghost.sortingOrder = targetRenderer.sortingOrder - 1;
        
        ghost.GetComponent<GhostFader>().StartFading();
    }

    public void ReturnGhost(SpriteRenderer ghost)
    {
        ghostPool.Release(ghost);
    }
}

public class GhostFader : MonoBehaviour
{
    private GhostTrail poolOwner;
    private SpriteRenderer sr;
    private float fadeSpeed;
    private bool isFading;

    public void Initialize(GhostTrail owner, float speed)
    {
        poolOwner = owner;
        fadeSpeed = speed;
        sr = GetComponent<SpriteRenderer>();
    }

    public void StartFading()
    {
        isFading = true;
    }

    private void Update()
    {
        if (!isFading) return;

        Color c = sr.color;
        c.a -= fadeSpeed * Time.deltaTime;
        sr.color = c;

        if (c.a <= 0f)
        {
            isFading = false;
            poolOwner.ReturnGhost(sr);
        }
    }
}
