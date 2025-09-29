using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public abstract class Vehicle : MonoBehaviour
{
    private Rigidbody2D rb;
    private Camera mainCamera;
    [SerializeField] private float speed = 5f;
    private bool isRunning;
    private bool hasRan;
    public Collider2D[] hitbox = new Collider2D[2];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        isRunning = false;
        hasRan = false;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (Mathf.Abs(transform.position.x - mainCamera.transform.position.x) > 20f)
        {
            isRunning = false;
        }
        else
        {
            isRunning = true;
            hasRan = true;
        }

        if (hasRan && !isRunning && Mathf.Abs(transform.position.x - mainCamera.transform.position.x) > 40f)
        {
            Destroy(gameObject);
        }

        if (!isRunning) return;

        rb.linearVelocityX = -speed;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isRunning) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            // Handle collision with player
            Debug.Log("Vehicle collided with Player");
            collision.transform.parent.gameObject.GetComponent<PlayerMovement>().StaggerPlayer(new Vector2(-1, 1));
            StartCoroutine(DisableHitboxTemp(2f));

        }
    }

    protected IEnumerator DisableHitboxTemp(float duration)
    {
        foreach (Collider2D c in hitbox)
        {
            c.enabled = false;
        }
        yield return new WaitForSeconds(duration);
        foreach (Collider2D c in hitbox)
        {
            c.enabled = true;
        }
    }
}
