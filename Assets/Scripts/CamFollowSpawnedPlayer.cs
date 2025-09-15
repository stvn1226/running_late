using Unity.Cinemachine;
using UnityEngine;
using System.Collections;

public class CamFollowSpawnedPlayer : MonoBehaviour
{
    private CinemachineCamera cinemachineCamera;
    
    private void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
    }
    
    void Start()
    {
        // Try to find and follow the player, with retry logic for scene reloads
        StartCoroutine(FindAndFollowPlayer());
    }
    
    private System.Collections.IEnumerator FindAndFollowPlayer()
    {
        // Wait a few frames for SpawnManager to initialize and spawn player
        yield return null;
        yield return null;
        
        // Try to get the player with multiple attempts
        GameObject player = null;
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
            cinemachineCamera.Target.TrackingTarget = player.transform;
            Debug.Log("Camera now following player");
        }
        else
        {
            Debug.LogWarning("CamFollowSpawnedPlayer: Could not find player to follow");
        }
    }
}
