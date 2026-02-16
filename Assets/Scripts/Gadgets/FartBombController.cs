using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A placed fart bomb in the world. Player places it, then detonates remotely.
/// On detonation: random fart-like noise pattern + green gas cloud.
/// Zombies hear the noise and investigate.
/// </summary>
public class FartBombController : MonoBehaviour
{
    // Instance tracking — NoiseSystem reads this
    public static List<FartBombController> ActiveBombs = new List<FartBombController>();

    [Header("Noise")]
    [SerializeField] private float minNoiseRadius = 15f;
    [SerializeField] private float maxNoiseRadius = 30f;
    [SerializeField] private float minDuration = 3f;
    [SerializeField] private float maxDuration = 8f;

    // State
    private bool isPlaced;
    private bool isDetonated;
    private bool isFinished;
    private float currentNoiseRadius;
    private float fartTimer;
    private float fartDuration;
    private float sputterTimer; // Random noise fluctuation

    // Gas cloud references
    private List<GameObject> gasParticles = new List<GameObject>();

    // Public API
    public bool IsPlaced => isPlaced;
    public bool IsDetonated => isDetonated;
    public bool IsFinished => isFinished;
    public float NoiseRadius => isDetonated && !isFinished ? currentNoiseRadius : 0f;

    // Link to inventory slot for remote display
    public int LinkedSlotIndex { get; set; } = -1;
    public System.Action<FartBombController> OnBombFinished;

    public void Place()
    {
        isPlaced = true;
        isDetonated = false;
        isFinished = false;

        ActiveBombs.Add(this);

        // Visual: small green sphere on ground
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.6f, 0.1f);
            rend.material = mat;
        }

        Debug.Log("[FART BOMB] Placed! Use remote (V) to detonate.");
    }

    public void Detonate()
    {
        if (!isPlaced || isDetonated) return;

        isDetonated = true;

        // Random fart characteristics
        fartDuration = Random.Range(minDuration, maxDuration);
        fartTimer = 0f;
        sputterTimer = 0f;

        Debug.Log($"[FART BOMB] 💨 DETONATED! Duration: {fartDuration:F1}s");

        // Start the fart sequence
        StartCoroutine(FartSequence());
    }

    private IEnumerator FartSequence()
    {
        // Spawn gas cloud
        SpawnGasCloud();

        // Initial big burst
        float burstRadius = Random.Range(maxNoiseRadius * 0.8f, maxNoiseRadius);
        currentNoiseRadius = burstRadius;

        yield return new WaitForSeconds(0.3f);

        // Main fart phase — random sputtering
        while (fartTimer < fartDuration)
        {
            fartTimer += Time.deltaTime;
            sputterTimer -= Time.deltaTime;

            if (sputterTimer <= 0f)
            {
                // Random sputter — sometimes loud, sometimes quiet, sometimes silent
                float roll = Random.value;
                if (roll < 0.15f)
                {
                    // Silent gap (the awkward pause)
                    currentNoiseRadius = 0f;
                    sputterTimer = Random.Range(0.2f, 0.6f);
                }
                else if (roll < 0.4f)
                {
                    // Big burst (the sequel)
                    currentNoiseRadius = Random.Range(maxNoiseRadius * 0.6f, maxNoiseRadius);
                    sputterTimer = Random.Range(0.1f, 0.4f);
                }
                else if (roll < 0.7f)
                {
                    // Medium rumble
                    currentNoiseRadius = Random.Range(minNoiseRadius, maxNoiseRadius * 0.5f);
                    sputterTimer = Random.Range(0.2f, 0.5f);
                }
                else
                {
                    // Squeaky little one
                    currentNoiseRadius = Random.Range(minNoiseRadius * 0.3f, minNoiseRadius);
                    sputterTimer = Random.Range(0.1f, 0.3f);
                }
            }

            // Fade toward end
            float fadeT = fartTimer / fartDuration;
            if (fadeT > 0.7f)
            {
                float fadeMult = 1f - ((fadeT - 0.7f) / 0.3f);
                currentNoiseRadius *= fadeMult;
            }

            // Update gas cloud scale
            UpdateGasCloud();

            yield return null;
        }

        // Final tiny squeak
        currentNoiseRadius = Random.Range(2f, 5f);
        yield return new WaitForSeconds(0.2f);

        // Done
        currentNoiseRadius = 0f;
        isFinished = true;

        Debug.Log("[FART BOMB] 💨 ...finished.");

        OnBombFinished?.Invoke(this);
        ActiveBombs.Remove(this);

        // Fade out gas cloud then destroy
        yield return FadeOutGasCloud();
        Destroy(gameObject);
    }

    private void SpawnGasCloud()
    {
        // Create multiple green semi-transparent spheres as gas cloud
        int cloudCount = Random.Range(6, 10);
        for (int i = 0; i < cloudCount; i++)
        {
            GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloud.name = "GasCloud";
            cloud.transform.SetParent(transform);
            cloud.transform.localPosition = Random.insideUnitSphere * 1.5f;
            cloud.transform.localPosition = new Vector3(
                cloud.transform.localPosition.x,
                Mathf.Abs(cloud.transform.localPosition.y) + 0.5f,
                cloud.transform.localPosition.z
            );
            cloud.transform.localScale = Vector3.one * Random.Range(0.5f, 1.5f);

            // Remove collider
            Object.Destroy(cloud.GetComponent<Collider>());

            Renderer rend = cloud.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Sprites/Default"));
            // Random green shade
            float g = Random.Range(0.5f, 0.9f);
            mat.color = new Color(0.1f, g, 0.05f, 0.2f);
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            gasParticles.Add(cloud);
        }
    }

    private void UpdateGasCloud()
    {
        // Expand and drift the cloud based on current noise
        float scale = Mathf.Max(currentNoiseRadius / maxNoiseRadius, 0.3f);
        foreach (var cloud in gasParticles)
        {
            if (cloud == null) continue;

            // Drift randomly
            cloud.transform.localPosition += Random.insideUnitSphere * Time.deltaTime * 0.5f;
            cloud.transform.localPosition = new Vector3(
                cloud.transform.localPosition.x,
                Mathf.Abs(cloud.transform.localPosition.y),
                cloud.transform.localPosition.z
            );

            // Scale with noise intensity
            float baseScale = Random.Range(0.8f, 2f);
            cloud.transform.localScale = Vector3.Lerp(
                cloud.transform.localScale,
                Vector3.one * baseScale * scale * 2f,
                Time.deltaTime * 3f
            );
        }
    }

    private IEnumerator FadeOutGasCloud()
    {
        float t = 0f;
        float fadeTime = 2f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float alpha = 1f - (t / fadeTime);
            foreach (var cloud in gasParticles)
            {
                if (cloud == null) continue;
                Renderer rend = cloud.GetComponent<Renderer>();
                if (rend != null)
                {
                    Color c = rend.material.color;
                    c.a = 0.2f * alpha;
                    rend.material.color = c;
                }
                // Slowly rise
                cloud.transform.localPosition += Vector3.up * Time.deltaTime * 0.3f;
            }
            yield return null;
        }
    }

    private void OnDestroy()
    {
        ActiveBombs.Remove(this);
    }
}
