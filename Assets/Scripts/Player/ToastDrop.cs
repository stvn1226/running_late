using UnityEngine;

/// <summary>
/// Adjuntar este script al GameObject "Toast" hijo del player.
/// El GameObject debe tener: SpriteRenderer (color marrón), Rigidbody2D, y un Collider2D.
/// El GameObject inicia desactivado (SetActive false en el prefab).
/// </summary>
public class ToastDrop : MonoBehaviour
{
    [Header("Physics Settings")]
    [Tooltip("Fuerza horizontal del lanzamiento.")]
    [SerializeField] private float launchForceX = 3f;
    [Tooltip("Fuerza vertical del lanzamiento.")]
    [SerializeField] private float launchForceY = 5f;
    [Tooltip("Variación aleatoria horizontal (+/-).")]
    [SerializeField] private float randomSpread = 1.5f;
    [Tooltip("Segundos antes de que la tostada se destruya sola.")]
    [SerializeField] private float lifetime = 4f;

    /// <summary>
    /// Desparenta y lanza esta tostada en el mundo con físicas.
    /// </summary>
    public void Drop(bool facingRight)
    {
        // Guardia de seguridad absoluta: si está por error en el Player, no hacer nada
        if (GetComponent<PlayerStates>() != null || GetComponent<PlayerMovement>() != null)
        {
            Debug.LogError("[ToastDrop] ERROR: El componente ToastDrop está en la raíz del Player. Debe estar en el GameObject hijo 'Toast'.");
            return;
        }

        transform.SetParent(null);
        gameObject.SetActive(true);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            float dirX = facingRight ? -1f : 1f;
            float spreadX = Random.Range(-randomSpread, randomSpread);
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2((launchForceX + spreadX) * dirX, launchForceY), ForceMode2D.Impulse);
        }

        Destroy(gameObject, lifetime);
    }
}
