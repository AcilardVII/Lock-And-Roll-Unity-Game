using UnityEngine;
using TMPro;
using UnityEngine.UI; // LayoutRebuilder için gerekli
using System.Collections;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("UI References")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI headerText;
    public RarityTextEffect rarityEffect;
    public TextMeshProUGUI contentText;
    public LayoutElement layoutElement;
    public RectTransform rectTransform;

    [Header("Camera Settings")]
    public Camera uiCamera;
    public RectTransform parentCanvasRect;

    // ========================================================================
    // 🎨 RENK PALETİ
    // ========================================================================
    [Header("Text Colors")]
    public Color customHighlightColor = new Color(1f, 0.64f, 0f);
    public Color pointsColor = new Color(0f, 0.6f, 1f);
    public Color addMultColor = new Color(1f, 0.33f, 0.33f);
    public Color xMultColor = new Color(1f, 0.8f, 0f);
    public Color moneyColor = Color.green;
    public Color rerollColor = new Color(1f, 1f, 0.5f);
    public Color roundColor = Color.white;

    public string ToHex(Color color)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(color);
    }
    

    [Header("Settings")]
    public int characterWrapLimit = 80;
    public float shiftAmount = 60f;

    [Header("Animation Settings")]
    [Range(0.05f, 1f)]
    public float animationDuration = 0.25f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        HideTooltip();

        
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999; 
            canvas.sortingLayerName = "UI";
        }

        if (uiCamera == null) uiCamera = Camera.main;
        if (parentCanvasRect == null) parentCanvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }
    private void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            Vector2 mousePos = Input.mousePosition;
            float pivotX = mousePos.x / Screen.width;
            float pivotY = mousePos.y / Screen.height;

            if (rectTransform != null) rectTransform.pivot = new Vector2(pivotX, pivotY);

            float finalOffsetX = (pivotX > 0.5f) ? -shiftAmount : shiftAmount;
            float finalOffsetY = (pivotY > 0.5f) ? -shiftAmount : shiftAmount;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvasRect,
                mousePos,
                uiCamera,
                out localPoint
            );

            tooltipPanel.transform.localPosition = localPoint + new Vector2(finalOffsetX, finalOffsetY);
            Vector3 currentLocal = tooltipPanel.transform.localPosition;
            tooltipPanel.transform.localPosition = new Vector3(currentLocal.x, currentLocal.y, 0f);
        }
    }

    public void ShowTooltip(string header, string content, CoinDragSnap.JokerRarity rarity = CoinDragSnap.JokerRarity.Common, bool showRarity = false)
    {
        if (string.IsNullOrEmpty(header) && string.IsNullOrEmpty(content)) return;

        
        if (headerText != null)
        {
            headerText.gameObject.SetActive(!string.IsNullOrEmpty(header));
            headerText.text = header;
        }

       
        if (rarityEffect != null)
        {
            if (showRarity)
            {
                rarityEffect.gameObject.SetActive(true);
                rarityEffect.SetRarity(rarity);
            }
            else
            {
                rarityEffect.gameObject.SetActive(false);
            }
        }

        
        if (contentText != null)
        {
            contentText.gameObject.SetActive(!string.IsNullOrEmpty(content));
            contentText.text = content;
        }

        
        if (layoutElement != null && headerText != null && contentText != null)
        {
            int headerLength = headerText.text.Length;
            int contentLength = contentText.text.Length;

            
            layoutElement.enabled = (headerLength > characterWrapLimit || contentLength > characterWrapLimit);
        }

        
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);

            
            if (rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }

            StopAllCoroutines();
            StartCoroutine(PopupAnim());
        }
    }

    public void HideTooltip()
    {
        StopAllCoroutines();
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    IEnumerator PopupAnim()
    {
        if (tooltipPanel == null) yield break;

        float expandTime = animationDuration * 0.7f;
        float settleTime = animationDuration * 0.3f;

        float t = 0f;
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float scale = Mathf.SmoothStep(0f, 1.1f, t / expandTime);
            tooltipPanel.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        t = 0f;
        while (t < settleTime)
        {
            t += Time.deltaTime;
            float scale = Mathf.SmoothStep(1.1f, 1.0f, t / settleTime);
            tooltipPanel.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        tooltipPanel.transform.localScale = Vector3.one;
    }
}