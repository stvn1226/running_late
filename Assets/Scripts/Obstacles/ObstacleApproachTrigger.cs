using UnityEngine;

public class ObstacleApproachTrigger : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerStates states = collision.GetComponentInParent<PlayerStates>();
            if (states != null)
            {
                states.isNearObstacle = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerStates states = collision.GetComponentInParent<PlayerStates>();
            if (states != null)
            {
                states.isNearObstacle = false;
            }
        }
    }
}
