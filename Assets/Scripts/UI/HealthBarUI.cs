using UnityEngine;

/// <summary>
/// Renders player health bar and damage flash using OnGUI.
/// Attach to the Player GameObject (same object as HealthSystem).
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    private HealthSystem health;
    private GUIStyle barBgStyle;
    private GUIStyle barFillStyle;
    private GUIStyle labelStyle;
    private GUIStyle deathStyle;
    private Texture2D whiteTex;
    private Texture2D redTex;

    private void Start()
    {
        health = GetComponent<HealthSystem>();
        if (health == null)
        {
            Debug.LogWarning("[HEALTH UI] No HealthSystem found!");
            enabled = false;
            return;
        }

        // Create textures
        whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();

        redTex = new Texture2D(1, 1);
        redTex.SetPixel(0, 0, Color.red);
        redTex.Apply();
    }

    private void OnGUI()
    {
        if (health == null) return;

        DrawHealthBar();
        DrawDamageFlash();

        if (health.IsDead)
            DrawDeathScreen();
    }

    private void DrawHealthBar()
    {
        float barWidth = 220f;
        float barHeight = 22f;
        float x = 15f;
        float y = 15f;
        float padding = 3f;

        // Background
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), whiteTex);

        // Fill
        float pct = health.HealthPercent;
        Color fillColor;
        if (pct > 0.6f)
            fillColor = new Color(0.2f, 0.85f, 0.2f); // Green
        else if (pct > 0.3f)
            fillColor = new Color(1f, 0.7f, 0.1f);     // Yellow
        else
            fillColor = new Color(0.9f, 0.15f, 0.1f);  // Red

        // Pulse when low
        if (pct <= 0.3f && pct > 0f)
        {
            float pulse = Mathf.Sin(Time.time * 6f) * 0.15f;
            fillColor.r = Mathf.Clamp01(fillColor.r + pulse);
        }

        GUI.color = fillColor;
        float fillWidth = (barWidth - padding * 2) * pct;
        GUI.DrawTexture(new Rect(x + padding, y + padding, fillWidth, barHeight - padding * 2), whiteTex);

        // HP text
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 13;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = Color.white;
        }

        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, barWidth, barHeight),
            $"HP  {health.CurrentHealth:F0} / {health.MaxHealth:F0}", labelStyle);

        // Invincibility indicator
        if (health.IsInvincible)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.4f);
            GUI.Label(new Rect(x + barWidth + 8, y, 40, barHeight), "INV", labelStyle);
        }
        GUI.color = Color.white;
    }

    private void DrawDamageFlash()
    {
        float alpha = health.FlashAlpha;
        if (alpha <= 0f) return;

        // Red vignette over entire screen
        GUI.color = new Color(1f, 0f, 0f, alpha * 0.35f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), redTex);
        GUI.color = Color.white;
    }

    private void DrawDeathScreen()
    {
        if (deathStyle == null)
        {
            deathStyle = new GUIStyle(GUI.skin.box);
            deathStyle.fontSize = 64;
            deathStyle.alignment = TextAnchor.MiddleCenter;
            deathStyle.normal.textColor = new Color(0.9f, 0.1f, 0.1f);
            deathStyle.fontStyle = FontStyle.Bold;
        }

        // Dark overlay
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);

        // Death text
        GUI.color = Color.white;
        GUI.backgroundColor = new Color(0, 0, 0, 0.9f);

        float w = 500, h = 120;
        Rect rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        GUI.Box(rect, "YOU DIED", deathStyle);

        GUI.backgroundColor = Color.white;
    }
}
