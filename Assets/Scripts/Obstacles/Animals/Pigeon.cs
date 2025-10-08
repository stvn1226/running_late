using UnityEngine;

public class Pigeon : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 3f;
    
    [Header("Flight Settings")]
    [SerializeField] private float flightDuration = 3f;
    [SerializeField] private float curveHeight = 4f;
    [SerializeField] private float leftTargetDistance = 10f;
    [SerializeField] private float flightSpeed = 8f;
    [SerializeField] private float continuousFlightSpeed = 10f;
    
    [Header("Curve Settings")]
    [SerializeField] private AnimationCurve flightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Animation")]
    [SerializeField] private string velocityParameterName = "velocity";
    
    private enum PigeonState
    {
        Idle,
        Flying,
        ContinuousFlight
    }
    
    // Components
    private Transform playerTransform;
    private Rigidbody2D rigidBody;
    private Animator pigeonAnimator;
    
    // State
    private PigeonState currentState = PigeonState.Idle;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float flightProgress = 0f;
    
    // Constants
    private const float MAX_FLIGHT_SPEED = 20f;
    private const float MIN_DISTANCE_THRESHOLD = 0.1f;
    
    void Start()
    {
        InitializeComponents();
        InitializeFlightPath();
    }
    
    void Update()
    {
        UpdatePlayerReference();
        UpdateBehavior();
        UpdateAnimator();
    }
    
    /// <summary>
    /// Initialize required components and validate setup
    /// </summary>
    private void InitializeComponents()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        pigeonAnimator = GetComponent<Animator>();
        startPosition = transform.position;
        
        if (rigidBody == null)
            Debug.LogError("Pigeon requires Rigidbody2D component", this);
    }
    
    /// <summary>
    /// Calculate target position for flight path
    /// </summary>
    private void InitializeFlightPath()
    {
        targetPosition = new Vector3(
            startPosition.x - leftTargetDistance, 
            startPosition.y, 
            startPosition.z
        );
    }
    
    /// <summary>
    /// Find and assign player reference if not already assigned
    /// </summary>
    private void UpdatePlayerReference()
    {
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }
    }
    
    /// <summary>
    /// Update pigeon behavior based on current state
    /// </summary>
    private void UpdateBehavior()
    {
        switch (currentState)
        {
            case PigeonState.Idle:
                if (playerTransform != null && IsPlayerInRange())
                {
                    StartFlight();
                }
                break;
                
            case PigeonState.Flying:
                PerformCurvedFlight();
                break;
                
            case PigeonState.ContinuousFlight:
                PerformContinuousFlight();
                break;
        }
    }
    
    /// <summary>
    /// Check if player is within detection radius
    /// </summary>
    private bool IsPlayerInRange()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        return distanceToPlayer <= detectionRadius;
    }
    
    /// <summary>
    /// Initialize flight sequence when player is detected
    /// </summary>
    private void StartFlight()
    {
        currentState = PigeonState.Flying;
        flightProgress = 0f;
        
        if (rigidBody != null)
        {
            rigidBody.gravityScale = 0f;
            rigidBody.linearDamping = 0f;
        }
    }
    
    /// <summary>
    /// Handle curved flight movement using flight curve
    /// </summary>
    private void PerformCurvedFlight()
    {
        if (rigidBody == null) return;
        
        flightProgress += Time.deltaTime / flightDuration;
        
        // Transition early to avoid erratic movement at the very end
        if (flightProgress >= 0.95f)
        {
            TransitionToContinuousFlight();
            return;
        }
        
        Vector3 desiredPosition = CalculateDesiredPosition();
        Vector3 direction = desiredPosition - transform.position;
        
        // Use consistent flight speed regardless of curve complexity
        if (direction.magnitude > MIN_DISTANCE_THRESHOLD)
        {
            rigidBody.linearVelocity = direction.normalized * flightSpeed;
        }
        else
        {
            // If very close to target, move directly to continuous flight
            TransitionToContinuousFlight();
        }
    }
    
    /// <summary>
    /// Calculate desired position along flight curve
    /// </summary>
    private Vector3 CalculateDesiredPosition()
    {
        Vector3 basePosition = Vector3.Lerp(startPosition, targetPosition, flightProgress);
        
        // Create an upward arc that starts low and peaks in the middle
        float curveMultiplier = flightCurve.Evaluate(flightProgress);
        float heightOffset = curveHeight * curveMultiplier;
        
        return new Vector3(basePosition.x, basePosition.y + heightOffset, basePosition.z);
    }
    
    /// <summary>
    /// Transition to continuous straight flight
    /// </summary>
    private void TransitionToContinuousFlight()
    {
        currentState = PigeonState.ContinuousFlight;
        rigidBody.linearVelocity = new Vector2(-continuousFlightSpeed, 0f);
    }
    
    /// <summary>
    /// Maintain continuous straight flight to the left
    /// </summary>
    private void PerformContinuousFlight()
    {
        if (rigidBody != null)
        {
            rigidBody.linearVelocity = new Vector2(-continuousFlightSpeed, 0f);
        }
    }
    
    /// <summary>
    /// Update animator velocity parameter based on current movement
    /// </summary>
    private void UpdateAnimator()
    {
        if (pigeonAnimator != null && rigidBody != null)
        {
            float currentVelocityMagnitude = rigidBody.linearVelocity.magnitude;
            pigeonAnimator.SetFloat(velocityParameterName, currentVelocityMagnitude);
        }
    }
    
    /// <summary>
    /// Draw gizmos for debugging in Scene view
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Flight path preview
        if (Application.isPlaying && currentState != PigeonState.Idle)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startPosition, targetPosition);
            
            // Show curve points
            Gizmos.color = Color.blue;
            for (float t = 0; t <= 1; t += 0.1f)
            {
                Vector3 point = Vector3.Lerp(startPosition, targetPosition, t);
                point.y += curveHeight * flightCurve.Evaluate(t);
                Gizmos.DrawWireSphere(point, 0.2f);
            }
        }
    }
}
