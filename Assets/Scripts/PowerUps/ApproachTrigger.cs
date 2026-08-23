using UnityEngine;

public class ApproachTrigger : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            PlayerStates states = collision.GetComponentInParent<PlayerStates>();
            if (states == null) states = collision.GetComponent<PlayerStates>();
            if (states != null)
            {
                states.isNearPowerUp = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            PlayerStates states = collision.GetComponentInParent<PlayerStates>();
            if (states == null) states = collision.GetComponent<PlayerStates>();
            if (states != null)
            {
                states.isNearPowerUp = false;
            }
        }
    }
}
