using UnityEngine;

/// <summary>
/// Attach to any GameObject in the world to make it a pickupable item.
/// Requires a Collider (trigger) for detection.
/// The player walks near it → it gets added to the cart's grid inventory.
/// </summary>
public class WorldItem : MonoBehaviour
{
    [Header("Item")]
    public ItemData itemData;

    [Header("Visual")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float spinSpeed = 60f;
    [SerializeField] private bool enableBob = true;
    [SerializeField] private bool enableSpin = true;

    [Header("Pickup")]
    [SerializeField] private float pickupRange = 2.5f;
    [SerializeField] private bool autoPickup = false; // If true, auto-pickup when near cart

    private Vector3 startPos;
    private bool isPickedUp;
    private float bobTimer;

    // Glow effect
    private Renderer itemRenderer;
    private Color baseEmission;
    private float glowPulse;

    public bool IsPickedUp => isPickedUp;

    private void Start()
    {
        startPos = transform.position;
        itemRenderer = GetComponentInChildren<Renderer>();

        // Ensure trigger collider exists
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
        }
        else
        {
            col.isTrigger = true;
        }

        // Set material color from ItemData
        if (itemData != null && itemRenderer != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            itemRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", itemData.itemColor);
            itemRenderer.SetPropertyBlock(block);
        }
    }

    private void Update()
    {
        if (isPickedUp) return;

        // Bob animation
        if (enableBob)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            Vector3 pos = startPos;
            pos.y += Mathf.Sin(bobTimer) * bobHeight;
            transform.position = pos;
        }

        // Spin
        if (enableSpin)
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
        }

        // Glow pulse
        if (itemRenderer != null)
        {
            glowPulse += Time.deltaTime * 3f;
            float glow = Mathf.Sin(glowPulse) * 0.3f + 0.7f;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            itemRenderer.GetPropertyBlock(block);
            Color c = itemData != null ? itemData.itemColor : Color.red;
            block.SetColor("_BaseColor", c * glow);
            block.SetColor("_EmissionColor", c * (glow * 0.5f));
            itemRenderer.SetPropertyBlock(block);
        }
    }

    /// <summary>Called by ItemPickup when the player picks this up.</summary>
    public void OnPickedUp()
    {
        isPickedUp = true;
        Debug.Log($"[WORLD] Picked up {(itemData != null ? itemData.itemName : "unknown")}!");

        // Shrink + destroy animation
        StartCoroutine(PickupAnimation());
    }

    private System.Collections.IEnumerator PickupAnimation()
    {
        float t = 0;
        Vector3 startScale = transform.localScale;
        Vector3 startPosition = transform.position;

        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float p = t / 0.3f;
            transform.localScale = startScale * (1f - p);
            transform.position = startPosition + Vector3.up * p * 1.5f;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
