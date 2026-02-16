using UnityEngine;

/// <summary>
/// Bridges the player's state to the Animator Controller.
/// Reads speed, crouch, push, hiding, jump, and fall states → sets Animator parameters.
/// Attach to the Player GameObject (auto-finds Animator in children).
///
/// Parameters set:
///   Speed (float)      — horizontal velocity
///   Grounded (bool)    — on the ground
///   Jump (bool)        — jumping up
///   FreeFall (bool)    — falling down
///   IsCrouching (bool) — crouch state
///   IsPushing (bool)   — pushing cart
///   IsHiding (bool)    — hiding state
///   PickUp (trigger)   — picking up item
///   TakeItem (trigger) — taking item
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    private CharacterController cc;
    private PlayerStateMachine stateMachine;
    private PlayerCrouch crouch;

    // Animator parameter hashes (cached for performance)
    private static readonly int SpeedHash      = Animator.StringToHash("Speed");
    private static readonly int GroundedHash   = Animator.StringToHash("Grounded");
    private static readonly int JumpHash       = Animator.StringToHash("Jump");
    private static readonly int FreeFallHash   = Animator.StringToHash("FreeFall");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsPushingHash  = Animator.StringToHash("IsPushing");
    private static readonly int IsHidingHash   = Animator.StringToHash("IsHiding");
    private static readonly int PickUpHash     = Animator.StringToHash("PickUp");
    private static readonly int TakeItemHash   = Animator.StringToHash("TakeItem");

    // Smoothed speed for animation blending
    private float smoothedSpeed;

    // Jump / fall detection
    private bool wasGrounded;
    private float jumpCooldown;     // Prevents re-triggering jump in same frame
    private float landingTimer;     // Brief lock after landing

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        cc = GetComponent<CharacterController>();
        stateMachine = GetComponent<PlayerStateMachine>();
        crouch = GetComponent<PlayerCrouch>();

        if (animator == null)
        {
            Debug.LogWarning("[PLAYER ANIM] No Animator found in children — animations won't play.");
            enabled = false;
            return;
        }

        // CRITICAL: Disable root motion so the character doesn't drift away
        animator.applyRootMotion = false;
        animator.stabilizeFeet = true;
        animator.feetPivotActive = 1f;

        wasGrounded = true;
        Debug.Log("[PLAYER ANIM] ✅ Animation controller ready! Root motion OFF, feet stabilized.");
    }

    private void Update()
    {
        if (animator == null) return;

        // ══════════════════════════════════════
        //  SPEED
        // ══════════════════════════════════════
        float rawSpeed = 0f;
        if (cc != null)
        {
            Vector3 vel = cc.velocity;
            vel.y = 0;
            rawSpeed = vel.magnitude;
        }

        // Snap to 0 when effectively stopped
        if (rawSpeed < 0.1f)
            smoothedSpeed = 0f;
        else
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawSpeed, Time.deltaTime * 15f);

        animator.SetFloat(SpeedHash, smoothedSpeed);

        // ══════════════════════════════════════
        //  GROUNDED / JUMP / FALL
        // ══════════════════════════════════════
        bool grounded = cc != null && cc.isGrounded;
        animator.SetBool(GroundedHash, grounded);

        // Cooldown tick
        if (jumpCooldown > 0f) jumpCooldown -= Time.deltaTime;
        if (landingTimer > 0f) landingTimer -= Time.deltaTime;

        // Detect jump: was grounded, now not, velocity going up
        if (wasGrounded && !grounded && cc.velocity.y > 0.1f && jumpCooldown <= 0f)
        {
            animator.SetBool(JumpHash, true);
            animator.SetBool(FreeFallHash, false);
            jumpCooldown = 0.3f;
        }

        // Detect freefall: not grounded, velocity going down (or been in air for a while)
        if (!grounded && cc.velocity.y <= 0f)
        {
            animator.SetBool(JumpHash, false);
            animator.SetBool(FreeFallHash, true);
        }

        // Detect landing: was not grounded, now grounded
        if (!wasGrounded && grounded)
        {
            animator.SetBool(JumpHash, false);
            animator.SetBool(FreeFallHash, false);
            landingTimer = 0.3f; // Brief landing lock
        }

        // Clear jump/fall when solidly grounded
        if (grounded && jumpCooldown <= 0f)
        {
            animator.SetBool(JumpHash, false);
            animator.SetBool(FreeFallHash, false);
        }

        wasGrounded = grounded;

        // ══════════════════════════════════════
        //  CROUCH
        // ══════════════════════════════════════
        bool crouching = crouch != null && crouch.IsCrouching;
        animator.SetBool(IsCrouchingHash, crouching);

        // ══════════════════════════════════════
        //  PUSH CART
        // ══════════════════════════════════════
        bool pushing = false;
        if (stateMachine != null)
            pushing = stateMachine.CurrentState == PlayerStateMachine.PlayerState.PushingCart;
        animator.SetBool(IsPushingHash, pushing);

        // ══════════════════════════════════════
        //  HIDING
        // ══════════════════════════════════════
        bool hiding = false;
        if (stateMachine != null)
            hiding = stateMachine.CurrentState == PlayerStateMachine.PlayerState.Hiding;
        animator.SetBool(IsHidingHash, hiding);
    }

    // ══════════════════════════════════════
    //  PUBLIC API — Trigger animations from other scripts
    // ══════════════════════════════════════

    /// <summary>Play the Pick Up Item animation (e.g., from InventorySystem).</summary>
    public void TriggerPickUp()
    {
        if (animator != null)
            animator.SetTrigger(PickUpHash);
    }

    /// <summary>Play the Taking Item animation (e.g., from InventorySystem).</summary>
    public void TriggerTakeItem()
    {
        if (animator != null)
            animator.SetTrigger(TakeItemHash);
    }
}
