using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    private Vector2 startPos;
    private GameObject cam;
    public float parallaxEffect;

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main.gameObject;
        }

        startPos = transform.position;
    }

    void Update()
    {
        Vector2 distance = new Vector2(cam.transform.position.x * parallaxEffect, cam.transform.position.y * parallaxEffect);
        transform.position = startPos + distance;
    }
}
