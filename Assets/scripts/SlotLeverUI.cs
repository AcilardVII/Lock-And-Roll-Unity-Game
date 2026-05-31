using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;

public class SlotLeverUI : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Görsel Referanslar")]
    public RectTransform handleRect;  
    public RectTransform pivotRect;   
    public RectTransform rodRect;     

    [Header("Hareket Ayarları")]
    [Tooltip("Kolun en fazla kaç derece aşağı döneceği (Örn: -130)")]
    public float maxRotationAngle = -130f;

    [Tooltip("Kolu sona kadar indirmek için mouse'u kaç piksel aşağı çekmek lazım?")]
    public float verticalDragRange = 200f;

    public float triggerThreshold = 0.7f; 
    public float returnSpeed = 10f;       

    [Header("Yay Ayarı (ÖNEMLİ)")]
    [Range(0.1f, 1.5f)]
    [Tooltip("1.0 = Tam Daire. 0.5 = Dar Yay (Yeşil Çizgi). 0.1 = Dümdüz aşağı.")]
    public float widthMultiplier = 0.5f; 

    [Header("Olaylar")]
    public UnityEvent OnLeverPulled;

    private float radius;
    private float startAngle;
    private float currentVerticalDrag = 0f;
    private bool isDragging = false;
    private bool isReturning = false;

    void Start()
    {
        if (handleRect == null) handleRect = GetComponent<RectTransform>();

       
        Vector3 direction = handleRect.position - pivotRect.position;
        radius = direction.magnitude;
        startAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        UpdateVisuals(0f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isReturning) return;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isReturning) return;
        isDragging = true;

        float deltaY = eventData.delta.y;
        float canvasScale = GetComponentInParent<Canvas>().scaleFactor;

        currentVerticalDrag -= deltaY / canvasScale;
        currentVerticalDrag = Mathf.Clamp(currentVerticalDrag, 0f, verticalDragRange);

        float t = currentVerticalDrag / verticalDragRange;
        float targetAngle = Mathf.Lerp(0f, maxRotationAngle, t);

        UpdateVisuals(targetAngle);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        float ratio = currentVerticalDrag / verticalDragRange;

        if (ratio >= triggerThreshold)
        {
            if (OnLeverPulled != null) OnLeverPulled.Invoke();
            StartCoroutine(ReturnRoutine(true));
        }
        else
        {
            StartCoroutine(ReturnRoutine(false));
        }
    }

    
    void UpdateVisuals(float angleOffset)
    {
       
        float finalAngleDeg = startAngle + angleOffset;
        float finalAngleRad = finalAngleDeg * Mathf.Deg2Rad;

        

        
        float xOffset = Mathf.Cos(finalAngleRad) * radius * widthMultiplier; 
        float yOffset = Mathf.Sin(finalAngleRad) * radius;                   
        
        Vector3 newPos = pivotRect.position + new Vector3(xOffset, yOffset, 0);
        handleRect.position = newPos;

       
        if (rodRect != null)
        {
            rodRect.position = (pivotRect.position + handleRect.position) / 2f;

            Vector3 dir = handleRect.position - pivotRect.position;
            float rodAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rodRect.rotation = Quaternion.Euler(0, 0, rodAngle - 90);

            float dist = Vector3.Distance(pivotRect.position, handleRect.position);
            float currentScaleY = rodRect.lossyScale.y;
            if (!Mathf.Approximately(currentScaleY, 0))
                rodRect.sizeDelta = new Vector2(rodRect.sizeDelta.x, dist / currentScaleY);
        }
    }

    IEnumerator ReturnRoutine(bool success)
    {
        isReturning = true;

        if (success)
        {
            float bounceTarget = verticalDragRange;
            while (Mathf.Abs(currentVerticalDrag - bounceTarget) > 1f)
            {
                currentVerticalDrag = Mathf.Lerp(currentVerticalDrag, bounceTarget, Time.deltaTime * 20f);
                float t = currentVerticalDrag / verticalDragRange;
                UpdateVisuals(Mathf.Lerp(0f, maxRotationAngle, t));
                yield return null;
            }
        }

        while (currentVerticalDrag > 0.1f)
        {
            currentVerticalDrag = Mathf.Lerp(currentVerticalDrag, 0f, Time.deltaTime * returnSpeed);
            float t = currentVerticalDrag / verticalDragRange;
            UpdateVisuals(Mathf.Lerp(0f, maxRotationAngle, t));
            yield return null;
        }

        currentVerticalDrag = 0f;
        UpdateVisuals(0f);
        isReturning = false;
    }
}