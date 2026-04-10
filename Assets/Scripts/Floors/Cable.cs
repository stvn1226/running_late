using Unity.VisualScripting;
using UnityEngine;

public class Cable : MonoBehaviour
{
    private EdgeCollider2D edgeCollider;
    //public bool isBeingUsed = false;
    private LineRenderer lineRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();

        //for (int i = 0; i < lineRenderer.positionCount; i++) 
        //{
        //    lineRenderer.SetPosition(i, new Vector3(lineRenderer.GetPosition(i).x, lineRenderer.GetPosition(i).y, -1f));
        //}
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        collision.gameObject.GetComponentInParent<PlayerStates>()?.StartCableSlide(edgeCollider);
        CableManager.instance.DisableOtherCables(true, edgeCollider);
    }

    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    collision.gameObject.GetComponentInParent<PlayerStates>()?.StartCableSlide(edgeCollider);
    //    CableManager.instance.DisableOtherCables(true, edgeCollider);
    //}

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


