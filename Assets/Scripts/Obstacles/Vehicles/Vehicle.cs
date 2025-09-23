using UnityEngine;

public abstract class Vehicle : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float speed = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        rb.linearVelocityX = -speed;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Handle collision with player
            Debug.Log("Vehicle collided with Player");
            collision.transform.parent.gameObject.GetComponent<PlayerMovement>().StaggerPlayer(new Vector2(-1, 1));
        }
    }
}
