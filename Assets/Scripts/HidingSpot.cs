using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hiding spot (e.g. cupboard). Player presses E to enter/exit.
/// While hiding: player is invisible to zombies (can't see or hear),
/// fully frozen, and camera stays on the spot.
///
/// Balance mechanics:
/// - Entry/exit makes noise (attracts nearby zombies)
/// - If a zombie SAW you enter, it will investigate and drag you out
/// </summary>
public class HidingSpot : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private string spotName = "Cupboard";

    [Header("Noise")]
    [SerializeField] private float entryNoiseRadius = 12f;
    [SerializeField] private float exitNoiseRadius = 10f;

    [Header("Zombie Investigation")]
    [SerializeField] private float investigateRange = 1.5f;   // Zombie "at" cupboard
    [SerializeField] private float bangDuration = 0.5f;          // Banging time before drag out
    [SerializeField] private float dragOutCooldown = 5f;       // Can't re-enter for X seconds

    // State
    private bool isOccupied;
    private Transform player;
    private Vector3 playerOriginalPosition;

    // Investigation state
    private ZombieAI investigatingZombie;    // The zombie that saw you enter
    private float bangTimer;
    private bool beingDraggedOut;
    private float cooldownTimer;

    // Noise burst — NoiseSystem and ZombieAI read these
    public static float CupboardNoiseRadius { get; private set; }
    public static Vector3 CupboardNoisePosition { get; private set; }
    private float noiseDecayTimer;

    // Static — so zombies can check globally
    public static bool IsPlayerHiding { get; private set; }

    // References
    private PlayerStateMachine stateMachine;
    private CameraController cameraController;

    private void Update()
    {
        // Decay noise burst
        if (noiseDecayTimer > 0f)
        {
            noiseDecayTimer -= Time.deltaTime;
            if (noiseDecayTimer <= 0f)
                CupboardNoiseRadius = 0f;
        }


        // Handle zombie investigation while hiding
        if (isOccupied && investigatingZombie != null)
        {
            UpdateInvestigation();
        }

        // Input
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        bool interactPressed = false;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame) interactPressed = true;
        if (gamepad != null && gamepad.buttonWest.wasPressedThisFrame) interactPressed = true;

        if (!interactPressed) return;

        if (isOccupied && !beingDraggedOut)
        {
            ExitHiding();
        }
        else if (!isOccupied)
        {
            TryEnterHiding();
        }
    }

    private void TryEnterHiding()
    {
        // Find player
        var cartInteraction = FindFirstObjectByType<CartInteraction>();
        if (cartInteraction == null) return;
        player = cartInteraction.transform;

        // Check distance
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > interactRange) return;

        // Can't hide while pushing cart
        if (cartInteraction.IsAttached)
        {
            Debug.Log($"[HIDE] Let go of the cart first!");
            return;
        }

        // Can't hide while carrying an item
        var pickup = player.GetComponent<ItemPickup>();
        if (pickup != null && pickup.IsCarrying)
        {
            Debug.Log($"[HIDE] Drop your item first!");
            return;
        }

        // Can't hide while controlling RC car
        var rcCar = player.GetComponent<RCCarItem>();
        if (rcCar != null && rcCar.IsControlling)
        {
            Debug.Log($"[HIDE] Can't hide while controlling RC car!");
            return;
        }

        EnterHiding();
    }

    private void EnterHiding()
    {
        isOccupied = true;
        IsPlayerHiding = true;
        beingDraggedOut = false;
        investigatingZombie = null;
        bangTimer = 0f;

        playerOriginalPosition = player.position;
        ZombieAI[] zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
        foreach (var zombie in zombies)
        {
            // Only investigate if zombie was CHASING AND ACTUALLY SAW the player
            if (zombie.State == ZombieAI.ZombieState.Chase && zombie.CanSeePlayer)
            {
                float distToSpot = Vector3.Distance(zombie.transform.position, transform.position);
                if (distToSpot < 25f)
                {
                    investigatingZombie = zombie;
                    zombie.SetInvestigateTarget(transform.position);
                    Debug.Log($"[HIDE] ⚠️ {zombie.name} SAW you enter! It's coming to investigate...");
                    break;
                }
            }
        }

        // Get references
        stateMachine = player.GetComponent<PlayerStateMachine>();
        Camera cam = Camera.main;
        if (cam != null)
            cameraController = cam.GetComponent<CameraController>();

        // Move player to hiding spot (invisible)
        player.position = transform.position + Vector3.up * 0.5f;

        // Freeze player
        if (stateMachine != null)
            stateMachine.TransitionTo(PlayerStateMachine.PlayerState.Hiding);

        // Disable player components
        var tpc = player.GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = false;
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Hide player visual
        SetPlayerVisibility(false);

        // Camera stays looking at cupboard from above
        if (cameraController != null)
            cameraController.SetTarget(transform);

        Debug.Log($"[HIDE] Entered {spotName}! Press E to exit.");
    }

    private void UpdateInvestigation()
    {
        // Safety: zombie was destroyed
        if (investigatingZombie == null || investigatingZombie.gameObject == null)
        {
            investigatingZombie = null;
            return;
        }

        // Just monitor distance — SetInvestigateTarget was already called once on entry
        float dist = Vector3.Distance(investigatingZombie.transform.position, transform.position);
        if (dist < investigateRange)
        {
            bangTimer += Time.deltaTime;

            if (!beingDraggedOut && bangTimer >= bangDuration)
            {
                beingDraggedOut = true;
                Debug.Log($"[HIDE] 💀 {investigatingZombie.name} DRAGGED YOU OUT!");
                ForceExitHiding();
            }
        }
    }

    private void ForceExitHiding()
    {
        // Exit hiding but with penalty
        ExitHiding();

        // The zombie is right there — instant chase
        if (investigatingZombie != null)
        {
            investigatingZombie.ClearInvestigateTarget();
        }
        investigatingZombie = null;
    }

    private void ExitHiding()
    {
        isOccupied = false;
        IsPlayerHiding = false;
        beingDraggedOut = false;

        // Clear investigation
        if (investigatingZombie != null)
        {
            investigatingZombie.ClearInvestigateTarget();
            investigatingZombie = null;
        }

        // Restore player position (slightly in front of cupboard)
        Vector3 exitPos = transform.position + transform.forward * 1.5f;
        exitPos.y = playerOriginalPosition.y;

        // Re-enable CharacterController BEFORE moving (needed for position)
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = true;
            cc.enabled = false;
            player.position = exitPos;
            cc.enabled = true;
        }
        else
        {
            player.position = exitPos;
        }

        // Re-enable player components
        var tpc = player.GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = true;

        // Show player visual
        SetPlayerVisibility(true);

        // Return camera to player
        if (cameraController != null)
            cameraController.SetTarget(player);

        // Unfreeze player
        if (stateMachine != null)
            stateMachine.TransitionTo(PlayerStateMachine.PlayerState.FreeRoam);

        Debug.Log($"[HIDE] Exited {spotName}.");
    }

    private void EmitNoise(float radius)
    {
        CupboardNoiseRadius = radius;
        CupboardNoisePosition = transform.position;
        noiseDecayTimer = 0.8f; // Noise lasts briefly
        Debug.Log($"[HIDE] 🔊 Cupboard noise! Radius: {radius}m");
    }

    private void SetPlayerVisibility(bool visible)
    {
        if (player == null) return;

        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.enabled = visible;
        }
    }

    // ─── HUD ───
    private void OnGUI()
    {
        if (isOccupied)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.7f, 0.9f, 1f) }
            };

            string hudText = $"HIDING in {spotName} [E to exit]";

            // Warning if zombie is investigating
            if (investigatingZombie != null)
            {
                float dist = Vector3.Distance(investigatingZombie.transform.position, transform.position);
                if (dist < investigateRange)
                {
                    // Zombie is banging
                    float timeLeft = bangDuration - bangTimer;
                    style.normal.textColor = Color.red;
                    hudText = $"⚠️ ZOMBIE FOUND YOU! Dragging out in {timeLeft:F1}s...";
                }
                else
                {
                    // Zombie approaching
                    style.normal.textColor = new Color(1f, 0.8f, 0.3f);
                    hudText = $"⚠️ A zombie is investigating! ({dist:F0}m away)";
                }
            }

            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 50, 300, 30),
                hudText, style);

            // Show cooldown warning if applicable
            if (cooldownTimer > 0f)
            {
                style.fontSize = 12;
                style.normal.textColor = Color.yellow;
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 30, 200, 20),
                    $"Can't re-hide for {cooldownTimer:F1}s", style);
            }

            return;
        }

        // Show interact prompt when nearby
        var cartInteraction = FindFirstObjectByType<CartInteraction>();
        if (cartInteraction == null) return;

        float promptDist = Vector3.Distance(transform.position, cartInteraction.transform.position);
        if (promptDist < interactRange)
        {
            GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            // Cooldown warning
            if (cooldownTimer > 0f)
            {
                promptStyle.normal.textColor = Color.yellow;
                Vector3 sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
                if (sp.z > 0)
                    GUI.Label(new Rect(sp.x - 80, Screen.height - sp.y - 20, 160, 25),
                        $"Locked ({cooldownTimer:F1}s)", promptStyle);
                return;
            }

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screenPos.z > 0)
            {
                float sx = screenPos.x;
                float sy = Screen.height - screenPos.y;
                GUI.Label(new Rect(sx - 80, sy - 20, 160, 25),
                    $"[E] Hide in {spotName}", promptStyle);
            }
        }
    }
}
