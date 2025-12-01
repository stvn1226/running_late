using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Home : MonoBehaviour
{
    [SerializeField] private float delay = 0.1f;
    [SerializeField] private SpriteRenderer cover;
    [SerializeField] private TilemapRenderer coverTilemap;
    
    [Header("Countdown Integration")]
    [SerializeField] private bool waitForCountdown = true;

    private void Start()
    {
        if (waitForCountdown && StartCountdownController.Instance != null)
        {
            StartCountdownController.Instance.OnCountdownComplete += OnCountdownComplete;
            GameLogger.Log("HOME", "Waiting for countdown to complete before disabling covers");
        }
        else
        {
            StartDisablingCovers();
        }
    }
    
    private void OnCountdownComplete()
    {
        GameLogger.Log("HOME", "Countdown complete - starting to disable covers");
        
        if (StartCountdownController.Instance != null)
        {
            StartCountdownController.Instance.OnCountdownComplete -= OnCountdownComplete;
        }
        
        StartDisablingCovers();
    }
    
    private void StartDisablingCovers()
    {
        StartCoroutine(DisableCover());
        StartCoroutine(DisableCoverTilemap());
    }

    private IEnumerator DisableCover()
    {
        yield return new WaitForSeconds(delay);
        
        if (cover != null)
        {
            cover.enabled = false;
            GameLogger.Log("HOME", "Cover sprite disabled");
        }
    }

    private IEnumerator DisableCoverTilemap()
    {
        yield return new WaitForSeconds(delay);
        
        if (coverTilemap != null)
        {
            coverTilemap.enabled = false;
            GameLogger.Log("HOME", "Cover tilemap disabled");
        }
    }
    
    private void OnDestroy()
    {
        if (StartCountdownController.Instance != null)
        {
            StartCountdownController.Instance.OnCountdownComplete -= OnCountdownComplete;
        }
    }
}