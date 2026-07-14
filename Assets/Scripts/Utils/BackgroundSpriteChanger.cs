using UnityEngine;

public class BackgroundSpriteChanger : MonoBehaviour
{
    [SerializeField] private Sprite[] backgroundSprites;
    private SpriteRenderer spriteRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = backgroundSprites[Random.Range(0, backgroundSprites.Length)];
    }
}
