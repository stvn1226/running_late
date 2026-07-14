using UnityEngine;

public class ObstacleApproachTrigger : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponentInParent<PlayerStates>().isNearObstacle = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponentInParent<PlayerStates>().isNearObstacle = false;
        }
    }
}
