using UnityEngine;
using UnityEngine.UI;

public class ProgressTracking : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    private Transform endPoint;
    private Transform spawnPoint;
    private Transform player;
    private float levelLength;
    private float playerPositionX;
    private float playerProgress;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AssignTransforms();
        if (endPoint != null || spawnPoint != null || player != null) levelLength = Mathf.Abs(endPoint.position.x - spawnPoint.position.x);
    }

    // Update is called once per frame
    void Update()
    {
        if (endPoint == null || spawnPoint == null || player == null)
        {
            AssignTransforms();
            if (endPoint != null || spawnPoint != null || player != null) levelLength = Mathf.Abs(endPoint.position.x - spawnPoint.position.x);
            return;
        }
        playerPositionX = Mathf.Abs(endPoint.position.x - player.position.x);
        playerProgress = 1 - (playerPositionX / levelLength);
        progressBar.fillAmount = playerProgress;
    }

    private void AssignTransforms()
    {
        endPoint = GameObject.FindGameObjectWithTag("Finish").transform;
        spawnPoint = GameObject.FindGameObjectWithTag("Respawn").transform;
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
}
