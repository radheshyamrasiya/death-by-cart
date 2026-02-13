using UnityEngine;

/// <summary>
/// Bridges StaminaSystem with Starter Assets' ThirdPersonController.
/// When on foot: gates sprint on stamina and drains it.
/// Attach to the Player (same object as ThirdPersonController + StaminaSystem).
/// </summary>
public class PlayerStaminaBridge : MonoBehaviour
{
    private StaminaSystem stamina;
    private StarterAssets.StarterAssetsInputs inputs;
    private StarterAssets.ThirdPersonController tpc;

    private void Start()
    {
        stamina = GetComponent<StaminaSystem>();
        inputs = GetComponent<StarterAssets.StarterAssetsInputs>();
        tpc = GetComponent<StarterAssets.ThirdPersonController>();

        if (stamina == null)
            Debug.LogWarning("[PLAYER] ⚠️ No StaminaSystem found on Player!");
        if (inputs == null)
            Debug.LogWarning("[PLAYER] ⚠️ No StarterAssetsInputs found on Player!");
    }

    private void Update()
    {
        if (stamina == null || inputs == null) return;

        // Only manage stamina when player is on foot (not pushing cart)
        // CartController handles stamina drain when pushing
        var cartInteraction = GetComponent<CartInteraction>();
        bool isPushingCart = cartInteraction != null && cartInteraction.IsAttached;

        if (isPushingCart) return; // CartController handles stamina when pushing

        // Player wants to sprint but stamina is empty — block it
        bool wantsSprint = inputs.sprint;
        bool isMoving = inputs.move.sqrMagnitude > 0.01f;

        if (wantsSprint && !stamina.CanSprint)
        {
            inputs.sprint = false;
        }

        // Tell stamina system if we're actually sprinting on foot
        stamina.SetSprinting(wantsSprint && stamina.CanSprint && isMoving);
    }
}
