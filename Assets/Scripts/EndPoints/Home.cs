using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Home : MonoBehaviour
{
    [SerializeField] private float delay = 0.1f;
    [SerializeField] private SpriteRenderer cover;
    [SerializeField] private SpriteRenderer closedDoor;
    [SerializeField] private GameObject doorToLaunch;
    [SerializeField] private Transform doorLaunchPoint;
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
        closedDoor.enabled = false;
        LaunchDoor();

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
    
    private void LaunchDoor() 
    { 
       if (doorToLaunch != null && doorLaunchPoint != null)
       {
            GameObject launchedDoor = Instantiate(doorToLaunch, doorLaunchPoint.position, doorLaunchPoint.rotation);
            launchedDoor.GetComponent<Rigidbody2D>()?.AddForce(doorLaunchPoint.right * 50f, ForceMode2D.Impulse);
            launchedDoor.GetComponent<Rigidbody2D>()?.AddTorque(-20f, ForceMode2D.Impulse);
            Destroy(launchedDoor, 4f);
            Debug.Log("Door launched at position: " + doorLaunchPoint.position);
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