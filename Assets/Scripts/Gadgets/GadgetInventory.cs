using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 3-slot gadget inventory for the player.
/// Q cycles: Holster → Slot1 → Slot2 → Slot3 → Holster
/// V activates the gadget in the current slot.
/// Supports: RC Car, Fart Bomb (→ Remote after placing).
/// Attach to the Player.
/// </summary>
public class GadgetInventory : MonoBehaviour
{
    // 3 gadget slots
    private ItemData[] slots = new ItemData[3];

    // Placed fart bombs linked to slots (null = no bomb placed for this slot)
    private FartBombController[] linkedBombs = new FartBombController[3];
    // Track which slots have been converted to remotes
    private bool[] isRemote = new bool[3];

    // -1 = holster (nothing equipped), 0/1/2 = slot index
    private int activeIndex = -1;

    // Public API
    public int ActiveIndex => activeIndex;
    public int SlotCount => slots.Length;
    public ItemData ActiveGadget => activeIndex >= 0 && activeIndex < slots.Length ? slots[activeIndex] : null;

    // References
    private RCCarItem rcCarItem;

    private void Start()
    {
        rcCarItem = GetComponent<RCCarItem>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        // ─── Q / D-Pad Right = cycle gadget slot ───
        bool cyclePressed = false;
        if (keyboard != null && keyboard.qKey.wasPressedThisFrame) cyclePressed = true;
        if (gamepad != null && gamepad.dpad.right.wasPressedThisFrame) cyclePressed = true;

        if (cyclePressed)
        {
            CycleSlot();
        }

        // ─── V / D-Pad Down = use active gadget ───
        bool usePressed = false;
        if (keyboard != null && keyboard.vKey.wasPressedThisFrame) usePressed = true;
        if (gamepad != null && gamepad.dpad.down.wasPressedThisFrame) usePressed = true;

        if (usePressed)
        {
            UseActiveGadget();
        }
    }

    private void CycleSlot()
    {
        // Don't cycle while actively controlling something
        if (rcCarItem != null && rcCarItem.IsControlling) return;

        activeIndex++;
        if (activeIndex >= slots.Length)
            activeIndex = -1; // Back to holster

        string slotName;
        if (activeIndex < 0)
            slotName = "Holster";
        else if (isRemote[activeIndex])
            slotName = $"Remote #{activeIndex + 1}";
        else if (slots[activeIndex] != null)
            slotName = slots[activeIndex].itemName;
        else
            slotName = $"Slot {activeIndex + 1} (empty)";

        Debug.Log($"[GADGET] Equipped: {slotName}");
    }

    private void UseActiveGadget()
    {
        // If RC car is being controlled, V cancels it
        if (rcCarItem != null && rcCarItem.IsControlling)
        {
            rcCarItem.CancelRCCar();
            return;
        }

        // Nothing equipped
        if (activeIndex < 0)
        {
            Debug.Log("[GADGET] Nothing equipped!");
            return;
        }

        // Check if this slot has a remote (placed bomb waiting to detonate)
        if (isRemote[activeIndex] && linkedBombs[activeIndex] != null)
        {
            DetonateBomb(activeIndex);
            return;
        }

        // No gadget in this slot
        if (slots[activeIndex] == null)
        {
            Debug.Log("[GADGET] Nothing equipped!");
            return;
        }

        ItemData gadget = slots[activeIndex];

        // ─── RC Car ───
        if (gadget.itemName == "RC Car")
        {
            if (rcCarItem != null)
            {
                rcCarItem.DeployRCCar();
                slots[activeIndex] = null;
            }
        }
        // ─── Fart Bomb ───
        else if (gadget.itemName == "Fart Bomb")
        {
            PlaceFartBomb(activeIndex);
        }
        // ─── Stun Mine ───
        else if (gadget.itemName == "Stun Mine")
        {
            PlaceStunMine(activeIndex);
        }
        else
        {
            Debug.Log($"[GADGET] {gadget.itemName} — not implemented yet!");
        }
    }

    private void PlaceFartBomb(int slotIndex)
    {
        // Create the bomb object at player's feet
        GameObject bombObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bombObj.name = "FartBomb_Placed";
        bombObj.transform.position = transform.position + transform.forward * 0.5f;
        bombObj.transform.localScale = Vector3.one * 0.35f;

        FartBombController bomb = bombObj.AddComponent<FartBombController>();
        bomb.LinkedSlotIndex = slotIndex;
        bomb.OnBombFinished += OnBombFinished;
        bomb.Place();

        // Convert slot to remote
        linkedBombs[slotIndex] = bomb;
        isRemote[slotIndex] = true;
        // Keep the slot's ItemData so the HUD still shows something
        // but isRemote flag changes the display

        Debug.Log($"[GADGET] Fart Bomb placed! Slot {slotIndex + 1} → Remote. Press V to detonate.");
    }

    private void PlaceStunMine(int slotIndex)
    {
        // Create the mine at player's feet
        GameObject mineObj = new GameObject("StunMine_Placed");
        mineObj.transform.position = transform.position;

        StunMineController mine = mineObj.AddComponent<StunMineController>();
        mine.Place();

        // Consume the gadget
        slots[slotIndex] = null;

        Debug.Log($"[GADGET] Stun Mine placed! Waiting for zombies...");
    }

    private void DetonateBomb(int slotIndex)
    {
        FartBombController bomb = linkedBombs[slotIndex];
        if (bomb == null || bomb.IsDetonated) return;

        bomb.Detonate();
        Debug.Log($"[GADGET] 💨 Remote {slotIndex + 1} triggered!");
    }

    private void OnBombFinished(FartBombController bomb)
    {
        // Clear the slot when the bomb finishes its fart sequence
        int slot = bomb.LinkedSlotIndex;
        if (slot >= 0 && slot < slots.Length)
        {
            slots[slot] = null;
            linkedBombs[slot] = null;
            isRemote[slot] = false;
            Debug.Log($"[GADGET] Slot {slot + 1} cleared (bomb finished).");
        }
    }

    /// <summary>Add a gadget to the first empty slot. Returns true if successful.</summary>
    public bool AddGadget(ItemData gadget)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null && !isRemote[i])
            {
                slots[i] = gadget;
                Debug.Log($"[GADGET] {gadget.itemName} added to slot {i + 1}");
                return true;
            }
        }

        Debug.Log("[GADGET] All gadget slots full!");
        return false;
    }

    /// <summary>Get the gadget in a specific slot.</summary>
    public ItemData GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    /// <summary>Clear a specific slot.</summary>
    public void ClearSlot(int index)
    {
        if (index >= 0 && index < slots.Length)
        {
            slots[index] = null;
            linkedBombs[index] = null;
            isRemote[index] = false;
        }
    }

    // ─── HUD ───
    private void OnGUI()
    {
        DrawGadgetHUD();
    }

    private void DrawGadgetHUD()
    {
        float startX = 10f;
        float startY = Screen.height - 90f;
        float slotW = 60f;
        float slotH = 60f;
        float gap = 6f;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        // Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.85f, 0.3f) }
        };

        string activeLabel = activeIndex < 0 ? "HOLSTER" : $"SLOT {activeIndex + 1}";
        GUI.Label(new Rect(startX, startY - 20, 200, 18), $"Gadgets [Q] — {activeLabel}", titleStyle);

        Color oldColor = GUI.color;

        for (int i = 0; i < slots.Length; i++)
        {
            float x = startX + i * (slotW + gap);
            bool isActiveSlot = (i == activeIndex);

            // Slot background
            if (isRemote[i])
            {
                // Remote slot — greenish tint
                GUI.color = isActiveSlot
                    ? new Color(0.3f, 0.9f, 0.3f, 0.6f)
                    : new Color(0.1f, 0.3f, 0.1f, 0.7f);
            }
            else
            {
                GUI.color = isActiveSlot
                    ? new Color(1f, 0.8f, 0.2f, 0.6f)
                    : new Color(0.15f, 0.15f, 0.15f, 0.7f);
            }
            GUI.DrawTexture(new Rect(x, startY, slotW, slotH), Texture2D.whiteTexture);

            // Border
            if (isRemote[i])
            {
                GUI.color = isActiveSlot
                    ? new Color(0.3f, 1f, 0.3f, 1f)
                    : new Color(0.2f, 0.5f, 0.2f, 0.8f);
            }
            else
            {
                GUI.color = isActiveSlot
                    ? new Color(1f, 0.9f, 0.3f, 1f)
                    : new Color(0.4f, 0.4f, 0.4f, 0.8f);
            }
            GUI.DrawTexture(new Rect(x, startY, slotW, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, startY + slotH - 2, slotW, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, startY, 2, slotH), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + slotW - 2, startY, 2, slotH), Texture2D.whiteTexture);

            GUI.color = Color.white;

            if (isRemote[i])
            {
                // Show "REMOTE" with blinking if bomb is detonated
                labelStyle.normal.textColor = isActiveSlot ? Color.black : new Color(0.3f, 1f, 0.3f);

                bool bombDetonated = linkedBombs[i] != null && linkedBombs[i].IsDetonated;
                if (bombDetonated)
                {
                    // Blinking "BOOM" text
                    float blink = Mathf.Sin(Time.time * 8f);
                    if (blink > 0)
                        GUI.Label(new Rect(x, startY + 5, slotW, 20), "💨", labelStyle);
                    else
                        GUI.Label(new Rect(x, startY + 5, slotW, 20), "BOOM", labelStyle);
                }
                else
                {
                    GUI.Label(new Rect(x, startY + 5, slotW, 20), "REMOTE", labelStyle);
                    labelStyle.fontSize = 8;
                    GUI.Label(new Rect(x, startY + 25, slotW, 16), "V = 💨", labelStyle);
                    labelStyle.fontSize = 10;
                }
            }
            else if (slots[i] != null)
            {
                labelStyle.normal.textColor = isActiveSlot ? Color.black : Color.white;
                GUI.Label(new Rect(x, startY + 5, slotW, 20), slots[i].itemName, labelStyle);

                GUI.color = slots[i].itemColor;
                GUI.DrawTexture(new Rect(x + 15, startY + 28, 30, 20), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            else
            {
                labelStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                GUI.Label(new Rect(x, startY + 18, slotW, 20), "Empty", labelStyle);
            }

            // Slot number
            GUIStyle numStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.LowerRight,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
            GUI.Label(new Rect(x, startY + slotH - 16, slotW - 4, 14), $"{i + 1}", numStyle);
        }

        GUI.color = oldColor;
    }
}
