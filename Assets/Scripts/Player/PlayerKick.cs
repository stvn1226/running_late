using UnityEngine;

public class PlayerKick : MonoBehaviour
{
    private BoxCollider2D kickBox;
    //private PlayerStates states;

    void Start()
    {
        kickBox = GetComponent<BoxCollider2D>();
        //states = GetComponentInParent<PlayerStates>();
        EndKick(); // Ensure kick hitbox is disabled at start
    }

    //void Update()
    //{
        
    //}

    public void PerformKick(float kickDuration)
    {
        Invoke("StartKick", 0.1f); // Slight delay to sync with animation
        Invoke("EndKick", kickDuration + 0.1f); // Disable kick hitbox after kick duration
    }

    private void StartKick()
    {
        if (kickBox != null)
        {
            kickBox.enabled = true;
        }
    }

    private void EndKick()
    {
        if (kickBox != null) {
            kickBox.enabled = false;
            //states.isKicking = false;
        }
    }
}
