using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class SingleSceneGameUI : MonoBehaviour
{
    [Header("UI Ayarları")]
    public RectTransform uiPanel;
    public float slideDuration = 0.6f;
    public float delayBeforeSlide = 0.5f;

    [Header("Pozisyonlar")]
    public Vector2 hiddenPosition = new Vector2(1920f, 0f);
    public Vector2 visiblePosition = Vector2.zero;

    [Header("Olaylar")]
    public UnityEvent onSlideComplete;

    void Start()
    {
        if (uiPanel == null)
            uiPanel = GetComponent<RectTransform>();

        if (uiPanel != null) uiPanel.anchoredPosition = hiddenPosition;
    }

    public void SlideIn()
    {
        StopAllCoroutines();
        StartCoroutine(MoveRoutine(hiddenPosition, visiblePosition, delayBeforeSlide, true));
    }

    public void SlideOut()
    {
        StopAllCoroutines();
        
        StartCoroutine(MoveRoutine(visiblePosition, hiddenPosition, 0f, false));
    }

    private IEnumerator MoveRoutine(Vector2 start, Vector2 end, float delay, bool isEntering)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        float timer = 0f;
        while (timer < slideDuration)
        {
            
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / slideDuration);

            if (uiPanel != null)
                uiPanel.anchoredPosition = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));

            yield return null;
        }

        if (uiPanel != null) uiPanel.anchoredPosition = end;

        
    }
}