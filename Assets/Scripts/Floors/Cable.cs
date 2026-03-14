using UnityEngine;

public class Cable : MonoBehaviour
{
    private EdgeCollider2D edgeCollider;
    //public bool isBeingUsed = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        collision.gameObject.GetComponentInParent<PlayerStates>()?.StartCableSlide(edgeCollider);
        CableManager.instance.DisableOtherCables(true, edgeCollider);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        collision.gameObject.GetComponentInParent<PlayerStates>()?.StartCableSlide(edgeCollider);
        CableManager.instance.DisableOtherCables(true, edgeCollider);
    }

    //private void OnCollisionStay2D(Collision2D collision)
    //{
    //    collision.gameObject.GetComponentInParent<PlayerMovement>()?.HandleCableSlideMovement();
    //}

    private void OnCollisionExit2D(Collision2D collision)
    {
        collision.gameObject.GetComponentInParent<PlayerStates>()?.EndCableSlide();
        CableManager.instance.DisableOtherCables(false, edgeCollider);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        collision.gameObject.GetComponentInParent<PlayerStates>()?.EndCableSlide();
        CableManager.instance.DisableOtherCables(false, edgeCollider);
    }
}


