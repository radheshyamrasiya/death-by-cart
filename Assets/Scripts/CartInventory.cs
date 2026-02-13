using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the cart's inventory — tracks fullness (0-100%), weight, and item count.
/// Press U to simulate adding an item. Press I to remove one.
/// Exposes fullness % for other systems (physics, sound, stealth).
/// </summary>
public class CartInventory : MonoBehaviour
{
    [Header("Capacity")]
    [Tooltip("Max weight the cart can carry (kg)")]
    [SerializeField] private float maxWeight = 50f;

    [Tooltip("Weight added per item pickup (press U)")]
    [SerializeField] private float weightPerItem = 5f;

    [Header("Current State (read-only in Inspector)")]
    [SerializeField] private float currentWeight;
    [SerializeField] private int itemCount;

    /// <summary>0.0 (empty) to 1.0 (full). Used by CartController, sound, stealth, etc.</summary>
    public float Fullness => Mathf.Clamp01(currentWeight / maxWeight);

    /// <summary>Current weight in kg.</summary>
    public float CurrentWeight => currentWeight;

    /// <summary>Number of items in the cart.</summary>
    public int ItemCount => itemCount;

    /// <summary>Is the cart completely full?</summary>
    public bool IsFull => currentWeight >= maxWeight;

    /// <summary>Fullness tier for gameplay logic.</summary>
    public FullnessTier Tier
    {
        get
        {
            float f = Fullness;
            if (f < 0.2f) return FullnessTier.Empty;
            if (f < 0.5f) return FullnessTier.Light;
            if (f < 0.8f) return FullnessTier.Half;
            return FullnessTier.Full;
        }
    }

    public enum FullnessTier
    {
        Empty,  // 0-20%  — Silent ninja
        Light,  // 20-50% — Slight squeak
        Half,   // 50-80% — Tense, regular squeaking
        Full    // 80-100% — DINNER BELL, run for your life
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // U = Add item
        if (keyboard.uKey.wasPressedThisFrame)
        {
            AddItem();
        }

        // I = Remove item
        if (keyboard.iKey.wasPressedThisFrame)
        {
            RemoveItem();
        }
    }

    /// <summary>Add an item to the cart.</summary>
    public void AddItem(float weight = -1f)
    {
        if (weight < 0) weight = weightPerItem;

        if (currentWeight >= maxWeight)
        {
            Debug.Log("[INVENTORY] Cart is FULL! Can't add more.");
            return;
        }

        currentWeight = Mathf.Min(currentWeight + weight, maxWeight);
        itemCount++;
        Debug.Log($"[INVENTORY] +Item! Weight={currentWeight:F1}/{maxWeight} ({Fullness * 100:F0}%) Tier={Tier}");
    }

    /// <summary>Remove an item from the cart.</summary>
    public void RemoveItem()
    {
        if (itemCount <= 0 || currentWeight <= 0)
        {
            Debug.Log("[INVENTORY] Cart is empty! Nothing to remove.");
            return;
        }

        currentWeight = Mathf.Max(currentWeight - weightPerItem, 0f);
        itemCount = Mathf.Max(itemCount - 1, 0);
        Debug.Log($"[INVENTORY] -Item! Weight={currentWeight:F1}/{maxWeight} ({Fullness * 100:F0}%) Tier={Tier}");
    }

    /// <summary>Dump all items (cart tips over).</summary>
    public void SpillAll()
    {
        Debug.Log($"[INVENTORY] SPILL! Lost {itemCount} items!");
        currentWeight = 0f;
        itemCount = 0;
    }
}
