using UnityEngine;

public class Toast : PowerUp
{
    [Header("Alternative Power-Ups")]
    [Tooltip("Lista de power-ups alternativos si el jugador todavía conserva la tostada.")]
    public GameObject[] alternativePowerUps;

    private bool hasResolved = false;

    private void Update()
    {
        if (hasResolved) return;

        if (alternativePowerUps != null && alternativePowerUps.Length > 0)
        {
            PlayerStates states = FindFirstObjectByType<PlayerStates>();
            if (states != null)
            {
                float dist = Mathf.Abs(states.transform.position.x - transform.position.x);
                if (dist < 15f)
                {
                    if (states.hasToast)
                    {
                        TransformToAlternative();
                    }
                    else
                    {
                        hasResolved = true; // El jugador no tiene tostada -> se queda como Toast!
                    }
                }
            }
        }
    }

    private void TransformToAlternative()
    {
        if (hasResolved) return;
        hasResolved = true;

        if (alternativePowerUps != null && alternativePowerUps.Length > 0)
        {
            GameObject picked = alternativePowerUps[Random.Range(0, alternativePowerUps.Length)];
            if (picked != null)
            {
                Instantiate(picked, transform.position, transform.rotation, transform.parent);
            }
        }

        Destroy(gameObject);
    }

    protected override void ApplyEffect(GameObject player)
    {
        PlayerStates playerStates = player.GetComponentInParent<PlayerStates>();
        if (playerStates != null)
        {
            playerStates.InvokeToast();
        }
    }
}
