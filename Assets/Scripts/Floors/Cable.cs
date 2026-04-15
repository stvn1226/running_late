using Unity.VisualScripting;
using UnityEngine;

public class Cable : MonoBehaviour
{
    private EdgeCollider2D edgeCollider;
    private bool ignoringPlayerCollision = false;
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
        var states = collision.gameObject.GetComponentInParent<PlayerStates>();
        if (states == null) return;

        // Don't hijack wallrun
        if (states.isWallRunning) return;

        // Player is already sliding on a DIFFERENT cable — pass through this one physically
        if (states.isSlidingOnCable && states.currentCable != edgeCollider)
        {
            Physics2D.IgnoreCollision(collision.collider, edgeCollider, true);
            ignoringPlayerCollision = true;
            return;
        }

        states.StartCableSlide(edgeCollider);
        CableManager.instance.DisableOtherCables(true, edgeCollider);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var states = collision.gameObject.GetComponentInParent<PlayerStates>();
        if (states == null) return;

        if (states.isWallRunning) return;

        // Already sliding on a different cable — pass through
        if (states.isSlidingOnCable && states.currentCable != edgeCollider)
        {
            Physics2D.IgnoreCollision(collision, edgeCollider, true);
            ignoringPlayerCollision = true;
            return;
        }

        states.StartCableSlide(edgeCollider);
        CableManager.instance.DisableOtherCables(true, edgeCollider);
    }

    //private void OnCollisionStay2D(Collision2D collision)
    //{
    //    collision.gameObject.GetComponentInParent<PlayerMovement>()?.HandleCableSlideMovement();
    //}

    private void OnCollisionExit2D(Collision2D collision)
    {
        // If we were ignoring this collision, restore it and do nothing else
        if (ignoringPlayerCollision)
        {
            Physics2D.IgnoreCollision(collision.collider, edgeCollider, false);
            ignoringPlayerCollision = false;
            return;
        }

        collision.gameObject.GetComponentInParent<PlayerStates>()?.EndCableSlide();
        CableManager.instance.DisableOtherCables(false, edgeCollider);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (ignoringPlayerCollision)
        {
            Physics2D.IgnoreCollision(collision, edgeCollider, false);
            ignoringPlayerCollision = false;
            return;
        }

        collision.gameObject.GetComponentInParent<PlayerStates>()?.EndCableSlide();
        CableManager.instance.DisableOtherCables(false, edgeCollider);
    }
}



