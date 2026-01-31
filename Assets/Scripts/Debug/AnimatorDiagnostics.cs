using UnityEngine;

public class AnimatorDiagnostics : MonoBehaviour
{
    private Animator animator;
    private PlayerStates states;
    private Rigidbody2D rb;
    
    [Header("Diagnostic Settings")]
    [SerializeField] private bool enableDiagnostics = true;
    [SerializeField] private bool logEveryFrame = false;
    [SerializeField] private KeyCode diagnosticKey = KeyCode.F1;
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        states = GetComponent<PlayerStates>();
        rb = GetComponent<Rigidbody2D>();
    }
    
    private void Update()
    {
        if (enableDiagnostics && Input.GetKeyDown(diagnosticKey))
        {
            PrintFullDiagnostics();
        }
        
        if (logEveryFrame && states != null && states.isKicking)
        {
            LogCurrentAnimatorState();
        }
    }
    
    private void PrintFullDiagnostics()
    {
        if (animator == null)
        {
            Debug.LogError("[ANIMATOR DIAG] No Animator found!");
            return;
        }
        
        Debug.Log("=== ANIMATOR DIAGNOSTICS ===");
        
        Debug.Log($"<color=cyan>CURRENT STATE:</color>");
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        
        Debug.Log($"  State Hash: {currentState.fullPathHash}");
        Debug.Log($"  Normalized Time: {currentState.normalizedTime:F2}");
        Debug.Log($"  Length: {currentState.length:F2}s");
        Debug.Log($"  Speed: {currentState.speed}");
        Debug.Log($"  Loop: {currentState.loop}");
        
        if (clips.Length > 0)
        {
            Debug.Log($"  Current Clip: {clips[0].clip.name}");
        }
        
        Debug.Log($"\n<color=cyan>NEXT STATE:</color>");
        AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
        if (animator.IsInTransition(0))
        {
            AnimatorTransitionInfo transitionInfo = animator.GetAnimatorTransitionInfo(0);
            Debug.Log($"  IN TRANSITION!");
            Debug.Log($"  Normalized Time: {transitionInfo.normalizedTime:F2}");
            Debug.Log($"  Duration: {transitionInfo.duration:F2}s");
            Debug.Log($"  Next State Hash: {nextState.fullPathHash}");
        }
        else
        {
            Debug.Log($"  Not in transition");
        }
        
        Debug.Log($"\n<color=cyan>PARAMETERS:</color>");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            string value = "";
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    value = animator.GetFloat(param.nameHash).ToString("F2");
                    break;
                case AnimatorControllerParameterType.Int:
                    value = animator.GetInteger(param.nameHash).ToString();
                    break;
                case AnimatorControllerParameterType.Bool:
                    value = animator.GetBool(param.nameHash).ToString();
                    break;
                case AnimatorControllerParameterType.Trigger:
                    value = animator.GetBool(param.nameHash) ? "SET" : "NOT SET";
                    break;
            }
            Debug.Log($"  {param.name} ({param.type}): {value}");
        }
        
        if (states != null)
        {
            Debug.Log($"\n<color=cyan>PLAYER STATES:</color>");
            Debug.Log($"  isKicking: {states.isKicking}");
            Debug.Log($"  isGrounded: {states.isGrounded}");
            Debug.Log($"  isJumping: {states.isJumping}");
            Debug.Log($"  isSliding: {states.isSliding}");
            Debug.Log($"  isDiving: {states.isDiving}");
            Debug.Log($"  isRolling: {states.isRolling}");
            Debug.Log($"  isWallRunning: {states.isWallRunning}");
        }
        
        if (rb != null)
        {
            Debug.Log($"\n<color=cyan>PHYSICS:</color>");
            Debug.Log($"  Velocity: {rb.linearVelocity}");
            Debug.Log($"  Speed: {rb.linearVelocity.magnitude:F2}");
        }
        
        Debug.Log("=== END DIAGNOSTICS ===\n");
    }
    
    private void LogCurrentAnimatorState()
    {
        if (animator == null) return;
        
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        string clipName = clips.Length > 0 ? clips[0].clip.name : "None";
        
        Debug.Log($"[ANIM DIAG] Frame {Time.frameCount} - Clip: {clipName} | isKicking: {states?.isKicking} | isGrounded: {states?.isGrounded}");
    }
}
