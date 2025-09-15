using System.Collections;
using UnityEngine;

public class Home : MonoBehaviour
{
    [SerializeField] private float delay = 0.1f;
    [SerializeField] private SpriteRenderer cover;

    void Start()
    {
        StartCoroutine(DisableCover());
    }

    void Update()
    {
        
    }

    private IEnumerator DisableCover() {
        yield return new WaitForSeconds(delay);
        cover.enabled = false;
    }
}
