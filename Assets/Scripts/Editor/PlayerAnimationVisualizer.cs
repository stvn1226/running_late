using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PlayerAnimationVisualizer : EditorWindow
{
    private PlayerAnimator playerAnimator;
    private PlayerStates playerStates;
    private Rigidbody2D rb;
    private Animator animator;
    
    private Vector2 scrollPos;
    private bool autoRefresh = true;
    private double lastRefreshTime;
    private const float REFRESH_RATE = 0.1f;
    
    private Dictionary<string, int> animationPlayCount = new Dictionary<string, int>();
    private List<AnimationTransition> recentTransitions = new List<AnimationTransition>();
    private const int MAX_TRANSITIONS = 10;
    
    private GUIStyle headerStyle;
    private GUIStyle activeStateStyle;
    private GUIStyle inactiveStateStyle;
    private GUIStyle transitionStyle;
    private bool stylesInitialized = false;
    
    [MenuItem("Tools/Player Animation Visualizer")]
    public static void ShowWindow()
    {
        PlayerAnimationVisualizer window = GetWindow<PlayerAnimationVisualizer>("Anim Visualizer");
        window.minSize = new Vector2(400, 600);
    }
    
    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        RefreshReferences();
    }
    
    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            RefreshReferences();
            animationPlayCount.Clear();
            recentTransitions.Clear();
        }
    }
    
    private void RefreshReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerAnimator = player.GetComponent<PlayerAnimator>();
            playerStates = player.GetComponent<PlayerStates>();
            rb = player.GetComponent<Rigidbody2D>();
            animator = player.GetComponent<Animator>();
        }
    }
    
    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };
        
        activeStateStyle = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { textColor = new Color(0.3f, 1f, 0.3f) },
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };
        
        inactiveStateStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
            fontSize = 11
        };
        
        transitionStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 10,
            wordWrap = true
        };
        
        stylesInitialized = true;
    }
    
    private void OnGUI()
    {
        InitializeStyles();
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        DrawHeader();
        DrawConnectionStatus();
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to visualize animations", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        
        if (playerAnimator == null || playerStates == null)
        {
            EditorGUILayout.HelpBox("Player components not found. Make sure Player has PlayerAnimator and PlayerStates.", MessageType.Warning);
            if (GUILayout.Button("Refresh References"))
            {
                RefreshReferences();
            }
            EditorGUILayout.EndScrollView();
            return;
        }
        
        GUILayout.Space(10);
        DrawCurrentAnimationState();
        
        GUILayout.Space(10);
        DrawPriorityList();
        
        GUILayout.Space(10);
        DrawStateFlags();
        
        GUILayout.Space(10);
        DrawPhysicsInfo();
        
        GUILayout.Space(10);
        DrawRecentTransitions();
        
        GUILayout.Space(10);
        DrawAnimationStats();
        
        GUILayout.Space(10);
        DrawDebugControls();
        
        EditorGUILayout.EndScrollView();
        
        if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > REFRESH_RATE)
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;
            TrackCurrentAnimation();
            Repaint();
        }
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("🎬 PLAYER ANIMATION VISUALIZER", headerStyle);
        GUILayout.FlexibleSpace();
        autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawConnectionStatus()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        DrawStatusLine("PlayerAnimator", playerAnimator != null);
        DrawStatusLine("PlayerStates", playerStates != null);
        DrawStatusLine("Rigidbody2D", rb != null);
        DrawStatusLine("Animator", animator != null);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawStatusLine(string label, bool connected)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(120));
        
        Color previousColor = GUI.color;
        GUI.color = connected ? Color.green : Color.red;
        GUILayout.Label(connected ? "✓ Connected" : "✗ Missing", EditorStyles.boldLabel);
        GUI.color = previousColor;
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawCurrentAnimationState()
    {
        EditorGUILayout.LabelField("CURRENT ANIMATION", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            
            string currentAnim = clipInfo.Length > 0 ? clipInfo[0].clip.name : "None";
            float normalizedTime = stateInfo.normalizedTime;
            
            EditorGUILayout.LabelField("Animation:", currentAnim, activeStateStyle);
            EditorGUILayout.LabelField("Progress:", $"{normalizedTime:F2} ({normalizedTime * 100:F0}%)");
            EditorGUILayout.LabelField("Hash:", stateInfo.shortNameHash.ToString());
            
            Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(progressRect, normalizedTime % 1f, $"{(normalizedTime % 1f) * 100:F0}%");
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawPriorityList()
    {
        EditorGUILayout.LabelField("ANIMATION PRIORITY LIST", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        DrawPriorityItem(1, "Stagger", playerStates.isStaggered, "HitDamage");
        DrawPriorityItem(2, "Kick", playerStates.isKicking, "KickA/B/C");
        DrawPriorityItem(3, "Dive", playerStates.isDiving, "GroundSlam");
        DrawPriorityItem(4, "Roll", playerStates.isRolling, "Roll");
        DrawPriorityItem(5, "Wall Vault", playerStates.isVaultingWall, "WallJump");
        DrawPriorityItem(6, "Wall Run", playerStates.isWallRunning, "WallClimb/WallSlide");
        DrawPriorityItem(7, "Air States", !playerStates.isGrounded, "JumpRise/Mid/Fall");
        DrawPriorityItem(8, "Landing", false, "Land");
        DrawPriorityItem(9, "Slide", playerStates.isSliding, "Slide");
        DrawPriorityItem(10, "Crouch", playerStates.isCrouching, "Crouch");
        DrawPriorityItem(11, "Run", Mathf.Abs(rb != null ? rb.linearVelocity.x : 0) > 0.5f, "Run");
        DrawPriorityItem(12, "Idle", true, "Idle");
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawPriorityItem(int priority, string name, bool isActive, string animName)
    {
        EditorGUILayout.BeginHorizontal();
        
        Color previousColor = GUI.backgroundColor;
        if (isActive)
        {
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        }
        
        GUILayout.Label($"{priority}.", GUILayout.Width(25));
        GUILayout.Label(name, isActive ? activeStateStyle : inactiveStateStyle, GUILayout.Width(120));
        GUILayout.Label($"→ {animName}", isActive ? activeStateStyle : inactiveStateStyle);
        
        if (isActive)
        {
            GUILayout.Label("⬤ ACTIVE", new GUIStyle(EditorStyles.boldLabel) 
            { 
                normal = { textColor = Color.green } 
            }, GUILayout.Width(80));
        }
        
        GUI.backgroundColor = previousColor;
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawStateFlags()
    {
        EditorGUILayout.LabelField("STATE FLAGS", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        DrawFlag("Grounded", playerStates.isGrounded);
        DrawFlag("Kicking", playerStates.isKicking);
        DrawFlag("Rolling", playerStates.isRolling);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        DrawFlag("Diving", playerStates.isDiving);
        DrawFlag("Wall Running", playerStates.isWallRunning);
        DrawFlag("Vaulting", playerStates.isVaultingWall);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        DrawFlag("Sliding", playerStates.isSliding);
        DrawFlag("Crouching", playerStates.isCrouching);
        DrawFlag("Staggered", playerStates.isStaggered);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("TIMERS", EditorStyles.miniBoldLabel);
        
        DrawTimer("Kick Timer", playerStates.kickTimer);
        DrawTimer("Roll Timer", playerStates.rollTimer);
        DrawTimer("Dive Timer", playerStates.diveTimer);
        DrawTimer("Stagger Timer", playerStates.staggerTimer);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawFlag(string label, bool value)
    {
        Color previousColor = GUI.color;
        GUI.color = value ? Color.green : Color.gray;
        GUILayout.Label(value ? "●" : "○", GUILayout.Width(15));
        GUI.color = previousColor;
        GUILayout.Label(label, GUILayout.Width(100));
    }
    
    private void DrawTimer(string label, float value)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(100));
        
        Rect timerRect = EditorGUILayout.GetControlRect(false, 18);
        float maxTime = 1f;
        float normalizedValue = Mathf.Clamp01(value / maxTime);
        
        Color previousColor = GUI.color;
        GUI.color = value > 0 ? new Color(0.3f, 0.8f, 1f) : Color.gray;
        EditorGUI.ProgressBar(timerRect, normalizedValue, $"{value:F3}s");
        GUI.color = previousColor;
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawPhysicsInfo()
    {
        EditorGUILayout.LabelField("PHYSICS INFO", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (rb != null)
        {
            Vector2 vel = rb.linearVelocity;
            EditorGUILayout.LabelField("Velocity:", $"({vel.x:F2}, {vel.y:F2})");
            EditorGUILayout.LabelField("Speed:", $"{vel.magnitude:F2}");
            EditorGUILayout.LabelField("Velocity X:", $"{vel.x:F2}");
            EditorGUILayout.LabelField("Velocity Y:", $"{vel.y:F2}");
            
            Rect velXRect = EditorGUILayout.GetControlRect(false, 18);
            EditorGUI.ProgressBar(velXRect, Mathf.Clamp01(Mathf.Abs(vel.x) / 10f), $"VelX: {vel.x:F2}");
            
            Rect velYRect = EditorGUILayout.GetControlRect(false, 18);
            Color previousColor = GUI.color;
            GUI.color = vel.y > 0 ? Color.green : Color.red;
            EditorGUI.ProgressBar(velYRect, Mathf.Clamp01(Mathf.Abs(vel.y) / 10f), $"VelY: {vel.y:F2}");
            GUI.color = previousColor;
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawRecentTransitions()
    {
        EditorGUILayout.LabelField("RECENT TRANSITIONS", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (recentTransitions.Count == 0)
        {
            EditorGUILayout.LabelField("No transitions recorded yet", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            for (int i = recentTransitions.Count - 1; i >= 0; i--)
            {
                AnimationTransition transition = recentTransitions[i];
                DrawTransition(transition);
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawTransition(AnimationTransition transition)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        
        GUILayout.Label(transition.timestamp.ToString("HH:mm:ss.fff"), transitionStyle, GUILayout.Width(100));
        
        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 0.8f, 0.3f);
        GUILayout.Label($"{transition.fromState} → {transition.toState}", transitionStyle);
        GUI.color = previousColor;
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawAnimationStats()
    {
        EditorGUILayout.LabelField("ANIMATION STATISTICS", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (animationPlayCount.Count == 0)
        {
            EditorGUILayout.LabelField("No animations played yet", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            foreach (var kvp in animationPlayCount)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(kvp.Key, GUILayout.Width(150));
                GUILayout.Label($"× {kvp.Value}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            }
        }
        
        if (GUILayout.Button("Clear Stats"))
        {
            animationPlayCount.Clear();
            recentTransitions.Clear();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawDebugControls()
    {
        EditorGUILayout.LabelField("DEBUG CONTROLS", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Force Kick"))
        {
            if (playerAnimator != null)
                playerAnimator.ForcePlayAnimation("KickA");
        }
        if (GUILayout.Button("Force Jump"))
        {
            if (playerAnimator != null)
                playerAnimator.ForcePlayAnimation("JumpRise");
        }
        if (GUILayout.Button("Force Roll"))
        {
            if (playerAnimator != null)
                playerAnimator.ForcePlayAnimation("Roll");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Force Idle"))
        {
            if (playerAnimator != null)
                playerAnimator.ForcePlayAnimation("Idle");
        }
        if (GUILayout.Button("Force Run"))
        {
            if (playerAnimator != null)
                playerAnimator.ForcePlayAnimation("Run");
        }
        if (GUILayout.Button("Force Dive"))
        {
            if (playerAnimator != null)
                playerAnimator.ForcePlayAnimation("GroundSlam");
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Refresh References"))
        {
            RefreshReferences();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void TrackCurrentAnimation()
    {
        if (playerAnimator == null) return;
        
        string currentAnim = playerAnimator.GetCurrentStateName();
        if (string.IsNullOrEmpty(currentAnim) || currentAnim == "Unknown") return;
        
        if (!animationPlayCount.ContainsKey(currentAnim))
        {
            animationPlayCount[currentAnim] = 0;
        }
        
        if (recentTransitions.Count == 0 || recentTransitions[recentTransitions.Count - 1].toState != currentAnim)
        {
            string fromState = recentTransitions.Count > 0 ? recentTransitions[recentTransitions.Count - 1].toState : "None";
            
            recentTransitions.Add(new AnimationTransition
            {
                fromState = fromState,
                toState = currentAnim,
                timestamp = System.DateTime.Now
            });
            
            animationPlayCount[currentAnim]++;
            
            if (recentTransitions.Count > MAX_TRANSITIONS)
            {
                recentTransitions.RemoveAt(0);
            }
        }
    }
    
    private struct AnimationTransition
    {
        public string fromState;
        public string toState;
        public System.DateTime timestamp;
    }
}
