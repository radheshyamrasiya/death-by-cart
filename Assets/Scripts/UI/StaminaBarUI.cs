using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stamina bar UI. Auto-creates its own Canvas and UI elements.
/// Color transitions Green → Yellow → Red as stamina depletes.
/// Fades out when stamina is full for a clean HUD.
/// </summary>
public class StaminaBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StaminaSystem staminaSystem;

    [Header("Bar Settings")]
    [SerializeField] private float barWidth = 300f;
    [SerializeField] private float barHeight = 20f;
    [SerializeField] private Vector2 screenOffset = new Vector2(0, 50f); // from bottom center

    [Header("Colors")]
    [SerializeField] private Color fullColor = new Color(0.2f, 0.9f, 0.3f);    // green
    [SerializeField] private Color midColor = new Color(1f, 0.8f, 0.1f);       // yellow
    [SerializeField] private Color lowColor = new Color(0.9f, 0.2f, 0.1f);     // red
    [SerializeField] private Color exhaustedColor = new Color(0.5f, 0f, 0f);   // dark red
    [SerializeField] private Color bgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    [Header("Visibility")]
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private float hideDelay = 2f; // seconds after full before hiding

    // Runtime
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image bgImage;
    private Image fillImage;
    private Image borderImage;
    private float hideTimer;
    private bool wasExhausted;

    private void Start()
    {
        // Auto-find stamina system
        if (staminaSystem == null)
            staminaSystem = FindFirstObjectByType<StaminaSystem>();

        CreateUI();
    }

    private void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("StaminaBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();

        // Bar container (anchored bottom-center)
        GameObject barContainer = new GameObject("StaminaBar");
        barContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = barContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = screenOffset;
        containerRect.sizeDelta = new Vector2(barWidth + 4, barHeight + 4);

        // Border
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(barContainer.transform, false);
        borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(0.8f, 0.8f, 0.8f, 0.6f);
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(barContainer.transform, false);
        bgImage = bgObj.AddComponent<Image>();
        bgImage.color = bgColor;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2, 2);
        bgRect.offsetMax = new Vector2(-2, -2);

        // Fill bar
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = fullColor;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (staminaSystem == null) return;

        float pct = staminaSystem.StaminaPercent;

        // Update fill width
        if (fillImage != null)
        {
            RectTransform fillRect = fillImage.GetComponent<RectTransform>();
            fillRect.anchorMax = new Vector2(pct, 1);

            // Color
            if (staminaSystem.IsExhausted)
            {
                fillImage.color = exhaustedColor;
            }
            else if (pct > 0.5f)
            {
                fillImage.color = Color.Lerp(midColor, fullColor, (pct - 0.5f) * 2f);
            }
            else
            {
                fillImage.color = Color.Lerp(lowColor, midColor, pct * 2f);
            }
        }

        // Pulse border when exhausted
        if (staminaSystem.IsExhausted)
        {
            float pulse = Mathf.Sin(Time.time * 6f) * 0.3f + 0.7f;
            borderImage.color = new Color(0.9f, 0.1f, 0.1f, pulse);
            wasExhausted = true;
        }
        else
        {
            borderImage.color = new Color(0.8f, 0.8f, 0.8f, 0.6f);
            if (wasExhausted)
            {
                wasExhausted = false;
            }
        }

        // Visibility — hide when full after delay
        if (pct >= 1f)
        {
            hideTimer += Time.deltaTime;
            if (hideTimer > hideDelay)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
            }
        }
        else
        {
            hideTimer = 0f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, fadeSpeed * 2f * Time.deltaTime);
        }
    }
}
