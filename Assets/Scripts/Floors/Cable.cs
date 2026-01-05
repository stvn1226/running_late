using UnityEngine;

public class Cable : MonoBehaviour
{
    private EdgeCollider2D edgeCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        collision.gameObject.GetComponentInParent<PlayerStates>()?.StartCableSlide(edgeCollider);
    }

    //private void OnCollisionStay2D(Collision2D collision)
    //{
    //    collision.gameObject.GetComponentInParent<PlayerMovement>()?.HandleCableSlideMovement();
    //}

    private void OnCollisionExit2D(Collision2D collision)
    {
        collision.gameObject.GetComponentInParent<PlayerStates>()?.EndCableSlide();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        collision.gameObject.GetComponentInParent<PlayerStates>()?.EndCableSlide();
    }
}


