using Unity.Cinemachine;
using UnityEngine;
using System.Collections;

public class CamFollowSpawnedPlayer : MonoBehaviour
{
    private CinemachineCamera cinemachineCamera;
    private GameObject player;
    
    private void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
    }
    
    void Start()
    {
        // Try to find and follow the player, with retry logic for scene reloads
        StartCoroutine(FindAndFollowPlayer());
    }

    private float currentTime = 0f;
    private float maxTime = .75f;
    private void Update()
    {
        if (player == null && currentTime >= maxTime)
        {
            StartCoroutine(FindAndFollowPlayer());
            currentTime = 0f;
        }
        else
        {
            currentTime += Time.deltaTime;
        }
    }

    private System.Collections.IEnumerator FindAndFollowPlayer()
    {
        // Wait a few frames for SpawnManager to initialize and spawn player
        yield return null;
        yield return null;
        
        // Try to get the player with multiple attempts
        player = null;
        int attempts = 0;
        int maxAttempts = 10;
        
        while (player == null && attempts < maxAttempts)
        {
            if (SpawnManager.Instance != null)
            {
                player = SpawnManager.Instance.GetCurrentPlayer();
            }
            
            if (player == null)
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
        }
        
        // Set the camera target if player was found
        if (player != null && cinemachineCamera != null)
        {
            Transform target = player.GetComponentInChildren<PanCamObject>().transform;
            cinemachineCamera.Target.TrackingTarget = target;
            cinemachineCamera.ForceCameraPosition(target.position, target.rotation);
            Debug.Log("Camera now following player");
        }
        else
        {
            Debug.LogWarning("CamFollowSpawnedPlayer: Could not find player to follow");
        }
    }
}
