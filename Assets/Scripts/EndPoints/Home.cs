using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Home : MonoBehaviour
{
    [SerializeField] private float delay = 0.1f;
    [SerializeField] private SpriteRenderer cover;
    [SerializeField] private TilemapRenderer coverTilemap;

    void Start()
    {
        StartCoroutine(DisableCover());
        StartCoroutine(DisableCoverTilemap());
    }

    void Update()
    {
        
    }

    private IEnumerator DisableCover() {
        yield return new WaitForSeconds(delay);
        cover.enabled = false;
    }

    private IEnumerator DisableCoverTilemap() {
        yield return new WaitForSeconds(delay);
        coverTilemap.enabled = false;
    }
}
