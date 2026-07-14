using System.Collections;
using UnityEngine;

public class LayerSwitcherSlope : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("Punto base de la rampa (Pista Inferior)")]
    public Transform bottomPoint; 
    [Tooltip("Punto más alto de la rampa (Vereda Superior)")]
    public Transform topPoint;    

    [Header("Configuración de Viaje")]
    [Tooltip("Tiempo en segundos que tarda en subir/bajar la rampa")]
    public float travelDuration = 0.35f;
    [Tooltip("Velocidad que se le da al jugador tras terminar de subir/bajar para que no se quede quieto")]
    public float exitVelocityX = 5f;

    // Triggers detection
    private bool isPlayerAtBottom = false;
    private bool isPlayerAtTop = false;
    private PlayerStates playerInTrigger;
    private bool isTraveling = false;

    // Los colliders hijos que actúan como Triggers en la base y cima
    public Collider2D bottomTrigger;
    public Collider2D topTrigger;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerStates ps = collision.GetComponentInParent<PlayerStates>();
            if (ps != null)
            {
                playerInTrigger = ps;
                
                if (bottomTrigger != null && collision.IsTouching(bottomTrigger))
                {
                    isPlayerAtBottom = true;
                }
                else if (topTrigger != null && collision.IsTouching(topTrigger))
                {
                    isPlayerAtTop = true;
                }
                else
                {
                    // Fallback si no asignaron triggers específicos: usar distancias
                    float distToBottom = Vector2.Distance(collision.transform.position, bottomPoint.position);
                    float distToTop = Vector2.Distance(collision.transform.position, topPoint.position);

                    if (distToBottom < distToTop) isPlayerAtBottom = true;
                    else isPlayerAtTop = true;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerAtBottom = false;
            isPlayerAtTop = false;
            playerInTrigger = null;
        }
    }

    private void Update()
    {
        if (playerInTrigger != null && !isTraveling)
        {
            // INPUT ARRIBA (Subir la rampa desde la pista inferior)
            // Se asume que el jugador presiona W o Flecha Arriba (moveInput.y > 0.5f)
            if (isPlayerAtBottom && playerInTrigger.moveInput.y > 0.5f)
            {
                StartCoroutine(TravelRoutine(playerInTrigger, topPoint.position));
            }
            
            // INPUT ABAJO (Bajar la rampa desde la vereda superior)
            // Se asume que el jugador presiona S, Flecha Abajo o el botón de DropDown
            else if (isPlayerAtTop && (playerInTrigger.dropDownHeld || playerInTrigger.moveInput.y < -0.5f))
            {
                StartCoroutine(TravelRoutine(playerInTrigger, bottomPoint.position));
            }
        }
    }

    private IEnumerator TravelRoutine(PlayerStates player, Vector3 destination)
    {
        isTraveling = true;
        
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        PlayerPhysics pp = player.GetComponent<PlayerPhysics>();
        
        // Bloquear físicas y resetear velocidades
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.simulated = false; 
        }

        Vector3 startPos = player.transform.position;
        float elapsed = 0f;

        // Animar el viaje (escalera / rampa)
        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelDuration;
            // Easing SmoothStep para que se sienta natural
            t = t * t * (3f - 2f * t);
            player.transform.position = Vector3.Lerp(startPos, destination, t);
            yield return null;
        }

        player.transform.position = destination;

        // Restaurar físicas y dar pequeño impulso
        if (rb != null)
        {
            rb.simulated = true;
            if (pp != null && pp.config != null)
            {
                rb.gravityScale = pp.config.fallGravityScale;
            }
            else
            {
                rb.gravityScale = 1f;
            }
            
            // Impulso hacia la derecha para que continúe corriendo
            rb.linearVelocity = new Vector2(exitVelocityX, 0f);
        }

        isTraveling = false;
        isPlayerAtBottom = false;
        isPlayerAtTop = false;
    }
}
