using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

/// <summary>
/// Stun Mine: placed on the ground, sits forever.
/// When a zombie enters the 3.5m trigger radius: AoE blast stuns ALL nearby zombies.
/// Stunned zombies are slowed by 95% for 2 seconds.
/// One-time use — mine is destroyed after triggering.
/// Does NOT affect the player.
/// </summary>
public class StunMineController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float triggerRadius = 3.5f;
    [SerializeField] private float stunDuration = 3f;
    [SerializeField] private float slowMultiplier = 0.05f; // 95% slow = 5% speed

    // State
    private bool isPlaced;
    private bool hasTriggered;

    // Visual references
    private GameObject blinkLight;
    private float blinkTimer;
    private Renderer blinkRenderer;

    public bool IsPlaced => isPlaced;

    public void Place()
    {
        isPlaced = true;
        hasTriggered = false;

        // Build mine visual
        BuildMineVisual();

        Debug.Log("[STUN MINE] Placed! Waiting for zombies...");
    }

    private void Update()
    {
        if (!isPlaced || hasTriggered) return;

        // Blink the light
        if (blinkRenderer != null)
        {
            blinkTimer += Time.deltaTime;
            float blink = Mathf.Sin(blinkTimer * 3f);
            blinkRenderer.material.SetColor("_EmissionColor",
                blink > 0 ? Color.cyan * 3f : Color.cyan * 0.3f);
        }

        // Check for nearby zombies
        ZombieAI[] zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
        foreach (var zombie in zombies)
        {
            float dist = Vector3.Distance(transform.position, zombie.transform.position);
            if (dist < triggerRadius)
            {
                Trigger(zombies);
                return;
            }
        }
    }

    private void Trigger(ZombieAI[] allZombies)
    {
        hasTriggered = true;

        Debug.Log("[STUN MINE] ⚡ TRIGGERED! Stunning all zombies in range!");

        // Stun all zombies in blast radius
        List<ZombieAI> stunnedZombies = new List<ZombieAI>();
        foreach (var zombie in allZombies)
        {
            float dist = Vector3.Distance(transform.position, zombie.transform.position);
            if (dist < triggerRadius)
            {
                stunnedZombies.Add(zombie);
                zombie.ApplyStun(stunDuration, slowMultiplier);
                Debug.Log($"[STUN MINE] ⚡ Stunned {zombie.name}!");
            }
        }

        // Visual flash
        StartCoroutine(FlashAndDestroy());
    }

    private IEnumerator FlashAndDestroy()
    {
        // Create expanding blue flash ring
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        flash.name = "StunFlash";
        flash.transform.position = transform.position + Vector3.up * 0.1f;
        flash.transform.localScale = new Vector3(1f, 0.02f, 1f);
        Object.Destroy(flash.GetComponent<Collider>());

        Renderer flashRend = flash.GetComponent<Renderer>();
        Material flashMat = new Material(Shader.Find("Sprites/Default"));
        flashMat.color = new Color(0.3f, 0.7f, 1f, 0.6f);
        flashRend.material = flashMat;
        flashRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Create electric spark particles (small blue spheres)
        List<GameObject> sparks = new List<GameObject>();
        for (int i = 0; i < 12; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "Spark";
            spark.transform.position = transform.position + Random.insideUnitSphere * 1f;
            spark.transform.position = new Vector3(
                spark.transform.position.x,
                transform.position.y + Random.Range(0.2f, 1.5f),
                spark.transform.position.z
            );
            spark.transform.localScale = Vector3.one * Random.Range(0.05f, 0.15f);
            Object.Destroy(spark.GetComponent<Collider>());

            Renderer sRend = spark.GetComponent<Renderer>();
            Material sMat = new Material(Shader.Find("Sprites/Default"));
            sMat.color = new Color(0.5f, 0.8f, 1f, 0.9f);
            sRend.material = sMat;
            sRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            sparks.Add(spark);
        }

        // Expand the flash ring
        float t = 0f;
        float expandTime = 0.5f;
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float progress = t / expandTime;

            // Expand ring
            float diameter = triggerRadius * 2f * progress;
            flash.transform.localScale = new Vector3(diameter, 0.02f, diameter);

            // Fade out
            Color c = flashMat.color;
            c.a = 0.6f * (1f - progress);
            flashMat.color = c;

            // Move sparks outward and upward
            foreach (var spark in sparks)
            {
                if (spark == null) continue;
                Vector3 dir = (spark.transform.position - transform.position).normalized;
                dir.y = Random.Range(0.5f, 2f);
                spark.transform.position += dir * Time.deltaTime * 4f;

                // Flicker
                Renderer sRend = spark.GetComponent<Renderer>();
                if (sRend != null)
                {
                    Color sc = sRend.material.color;
                    sc.a = Random.Range(0.3f, 1f) * (1f - progress);
                    sRend.material.color = sc;
                }
            }

            yield return null;
        }

        // Cleanup
        Destroy(flash);
        foreach (var spark in sparks)
        {
            if (spark != null) Destroy(spark);
        }

        // Destroy the mine itself
        Destroy(gameObject);
    }

    private void BuildMineVisual()
    {
        // Mine base — flat blue disc
        GameObject baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseDisc.name = "MineBase";
        baseDisc.transform.SetParent(transform);
        baseDisc.transform.localPosition = new Vector3(0, 0.05f, 0);
        baseDisc.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        Object.Destroy(baseDisc.GetComponent<Collider>());

        Renderer baseRend = baseDisc.GetComponent<Renderer>();
        Material baseMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        baseMat.color = new Color(0.15f, 0.25f, 0.5f);
        baseRend.material = baseMat;

        // Mine top — smaller darker disc
        GameObject topDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        topDisc.name = "MineTop";
        topDisc.transform.SetParent(transform);
        topDisc.transform.localPosition = new Vector3(0, 0.12f, 0);
        topDisc.transform.localScale = new Vector3(0.3f, 0.03f, 0.3f);
        Object.Destroy(topDisc.GetComponent<Collider>());

        Renderer topRend = topDisc.GetComponent<Renderer>();
        Material topMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        topMat.color = new Color(0.1f, 0.15f, 0.35f);
        topRend.material = topMat;

        // Blinking light on top
        blinkLight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        blinkLight.name = "BlinkLight";
        blinkLight.transform.SetParent(transform);
        blinkLight.transform.localPosition = new Vector3(0, 0.18f, 0);
        blinkLight.transform.localScale = Vector3.one * 0.08f;
        Object.Destroy(blinkLight.GetComponent<Collider>());

        blinkRenderer = blinkLight.GetComponent<Renderer>();
        Material blinkMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        blinkMat.color = Color.cyan;
        blinkMat.EnableKeyword("_EMISSION");
        blinkMat.SetColor("_EmissionColor", Color.cyan * 2f);
        blinkRenderer.material = blinkMat;

        // Trigger radius indicator — faint blue ring on ground
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "TriggerRing";
        ring.transform.SetParent(transform);
        ring.transform.localPosition = new Vector3(0, 0.02f, 0);
        float ringDiameter = triggerRadius * 2f;
        ring.transform.localScale = new Vector3(ringDiameter, 0.005f, ringDiameter);
        Object.Destroy(ring.GetComponent<Collider>());

        Renderer ringRend = ring.GetComponent<Renderer>();
        Material ringMat = new Material(Shader.Find("Sprites/Default"));
        ringMat.color = new Color(0.3f, 0.6f, 1f, 0.08f);
        ringRend.material = ringMat;
        ringRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
