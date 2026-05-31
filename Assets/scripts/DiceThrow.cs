using TMPro;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class DiceThrow : MonoBehaviour
{
    Rigidbody2D rb;

    public enum RiggedType
    {
        None,
        AlwaysMax,
        AlwaysMin,
        AlwaysSpecific,
        AddValue
    }

    [Header("🔥 Rigged (Hileli) Ayarları")]
    public RiggedType currentRig = RiggedType.None;
    public int rigValue = 0;

    [Header("🎨 Rigged Popup Görünümü")]
    public Vector3 riggedPopupOffset = new Vector3(0, 0.8f, 0);
    public Color riggedPopupColor = new Color(1f, 0.3f, 1f);
    public float riggedPopupDuration = 1.0f;
    public float riggedPopupFontSize = 48f;
    public TMP_FontAsset riggedPopupFont;

    [Header("💬 Rigged Mesajları")]
    public string msgPrefix = "+";
    public string msgMax = "MAX!";
    public string msgMin = "MIN!";
    public string msgFix = "FIX!";

    [Header("🎨 Genel Görsel Ayarlar")]
    public Color normalTextColor = Color.white;
    public Color riggedTextColor = new Color(1f, 0.3f, 1f);
    public Material riggedMaterial;

    
    private Material defaultMaterial;
    private Renderer objectRenderer; 

    [Header("🌑 Void Enchantment Visuals")]
    public Material voidEnchantmentMaterial; 
    public Color voidTextColor = new Color(0.8f, 0f, 1f); 

    // 🔥🔥 YENİ EKLENEN KISIM: HOLOGRAM GÖRSELLERİ 🔥🔥
    [Header("🌐 Hologram Enchantment Visuals")]
    public Material hologramEnchantmentMaterial; 
    public Color hologramTextColor = Color.cyan; 

    // Font yedeği
    private TMP_FontAsset defaultFont;

    [Header("🔥 Element / Enchantment")]
    public DiceEnchantment activeEnchantment;

    
    [HideInInspector] public bool isTemporaryClone = false;

    [Header("Ayarlar")]
    public int minRange = 1;
    public int maxRange = 6;
    public int defaultFaceValue = 6;

    [Header("Market Verileri")]
    public int purchasePrice = 0;
    public int sellValue = 0;

    [Header("Fizik Ayarları")]
    public float minThrowY = 20f;
    public float maxThrowY = 30f;
    public float minTorque = -1000f;
    public float maxTorque = 1000f;
    public float minLeftSapma = -25f;
    public float maxLeftSapma = -15f;
    public float minRightSapma = 15f;
    public float maxRightSapma = 25f;

    [Header("Durma Kontrolü")]
    public float stopVelocityThreshold = 0.5f;
    public float mustBeStoppedFor = 0.5f;
    private float stoppedTimer = 0f;

    [Header("Görsel & UI")]
    public TextMeshProUGUI diceText;
    public float popupScale = 1.5f;
    public float popupDuration = 0.3f;

    private bool canRoll = true;
    private bool resultShown = false;
    private bool isProcessingResult = false;

    private Vector3 originalScale;

    public bool IsResultAnimating { get; private set; } = false;
    public bool IsLockedForRoll { get; private set; }

    private Vector3 lockedPosition;
    public bool HasResult => resultShown;
    public System.Action<int> OnResult;
    public DiceManager diceManager;

    void Start()
    {
        if (diceManager == null) diceManager = FindFirstObjectByType<DiceManager>();
        UpdateVisuals();
    }

    void OnEnable()
    {
        ForceWakeUp();
        UpdateVisuals();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.StartAwake;

        
        objectRenderer = GetComponentInChildren<Renderer>();

        
        if (objectRenderer != null) defaultMaterial = objectRenderer.material;

        if (transform.localScale.x > 0.01f) originalScale = transform.localScale;
        else originalScale = Vector3.one;

        diceText = GetComponentInChildren<TextMeshProUGUI>(); 
        if (diceText != null)
        {
            defaultFont = diceText.font; 
            diceText.text = defaultFaceValue.ToString();

            if (normalTextColor == Color.white && diceText.color != Color.white)
                normalTextColor = diceText.color;
        }
    }

    public void UpdateVisuals()
    {
        
        if (objectRenderer == null) objectRenderer = GetComponentInChildren<Renderer>();
        if (diceText == null) diceText = GetComponentInChildren<TextMeshProUGUI>();

        
        if (objectRenderer != null && defaultMaterial != null) objectRenderer.material = defaultMaterial;

        if (diceText != null)
        {
            diceText.color = normalTextColor;
            diceText.fontStyle = FontStyles.Normal;
        }

        
        if (activeEnchantment != null)
        {
            
            if (activeEnchantment.type == DiceEnchantment.EnchantType.Void)
            {
                if (objectRenderer != null && voidEnchantmentMaterial != null)
                {
                    objectRenderer.material = voidEnchantmentMaterial;
                }
                if (diceText != null)
                {
                    diceText.color = voidTextColor;
                }
                return; 
            }
            
            else if (activeEnchantment.type == DiceEnchantment.EnchantType.Hologram)
            {
                if (objectRenderer != null && hologramEnchantmentMaterial != null)
                {
                    objectRenderer.material = hologramEnchantmentMaterial;
                }
                if (diceText != null)
                {
                    diceText.color = hologramTextColor;
                }
                return; 
            }
        }

        bool isRigged = (currentRig != RiggedType.None);
        if (isRigged)
        {
            if (diceText != null)
            {
                diceText.color = riggedTextColor;
                diceText.fontStyle = FontStyles.Bold;
            }
            if (objectRenderer != null && riggedMaterial != null)
            {
                objectRenderer.material = riggedMaterial;
            }
        }
    }

    public void ForceWakeUp()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.simulated = true;
        rb.WakeUp();
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }

    void Update()
    {
        if (!canRoll && !resultShown && !isProcessingResult) CheckIfStopped();
    }

    public void ResetToDefaultFace()
    {
        if (diceText != null) diceText.text = defaultFaceValue.ToString();
        resultShown = false;
        isProcessingResult = false;
        canRoll = true;
        IsResultAnimating = false;
        UpdateVisuals();
    }

    public void SetLockedPosition(Vector3 pos, bool locked)
    {
        lockedPosition = pos;
        IsLockedForRoll = locked;
    }

    public void AppearOnScene(float duration)
    {
        gameObject.SetActive(true);
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        ForceWakeUp();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
        StartCoroutine(AppearRoutine(duration));
    }

    private IEnumerator AppearRoutine(float duration)
    {
        float timer = 0f;
        transform.localScale = Vector3.zero;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            float elastic = Mathf.Sin(-13 * (t + 1) * Mathf.PI / 2) * Mathf.Pow(2, -10 * t) + 1;
            transform.localScale = originalScale * elastic;
            yield return null;
        }
        transform.localScale = originalScale;
    }

    public void Roll()
    {
        ForceWakeUp();
        canRoll = false;
        resultShown = false;
        isProcessingResult = false;
        IsResultAnimating = false;
        stoppedTimer = 0f;
        if (diceText != null) diceText.text = "";
        rb.bodyType = RigidbodyType2D.Dynamic;

        float xForce = (Random.value > 0.5f) ? Random.Range(minRightSapma, maxRightSapma) : Random.Range(minLeftSapma, maxLeftSapma);
        float yForce = Random.Range(minThrowY, maxThrowY);
        rb.AddForce(new Vector2(xForce, yForce), ForceMode2D.Impulse);
        rb.AddTorque(Random.Range(minTorque, maxTorque));
    }

    void CheckIfStopped()
    {
        if (rb.linearVelocity.magnitude < stopVelocityThreshold && Mathf.Abs(rb.angularVelocity) < 10f)
        {
            stoppedTimer += Time.deltaTime;
            if (stoppedTimer >= mustBeStoppedFor) ShowResult();
        }
        else stoppedTimer = 0f;
    }

    void ShowResult()
    {
        if (resultShown || isProcessingResult) return;
        isProcessingResult = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        int naturalRoll = Random.Range(minRange, maxRange + 1);
        if (diceText != null) diceText.text = naturalRoll.ToString();

        StartCoroutine(ResolveResultSequence(naturalRoll));
    }

    IEnumerator ResolveResultSequence(int baseRoll)
    {
        yield return StartCoroutine(ResultPopupAnimation());

        if (currentRig != RiggedType.None)
        {
            yield return StartCoroutine(PlayRiggedRevealAnimation(baseRoll));
        }
        else
        {
            FinalizeResult(baseRoll);
        }
    }

    IEnumerator PlayRiggedRevealAnimation(int baseRoll)
    {
        yield return new WaitForSeconds(0.3f);

        int finalResult = baseRoll;
        string msg = "";

        switch (currentRig)
        {
            case RiggedType.AddValue:
                finalResult = baseRoll + rigValue;
                msg = (rigValue >= 0) ? $"+{rigValue} added" : $"{rigValue} removed";
                break;
            case RiggedType.AlwaysMax:
                finalResult = maxRange;
                msg = "Max forced";
                break;
            case RiggedType.AlwaysMin:
                finalResult = minRange;
                msg = "Min forced";
                break;
            case RiggedType.AlwaysSpecific:
                finalResult = rigValue;
                msg = $"Fixed to {rigValue}";
                break;
        }

        if (finalResult < 0) finalResult = 0;

        if (GameLogManager.Instance != null)
        {
            GameLogManager.Instance.Log(GameLogManager.LogCategory.Hack, msg, baseRoll, finalResult);
        }

        if (diceText != null)
        {
            diceText.text = finalResult.ToString();
            diceText.color = riggedTextColor;
            diceText.fontStyle = FontStyles.Bold;
        }

        yield return StartCoroutine(PunchScaleEffect());

        FinalizeResult(finalResult);
    }

    void FinalizeResult(int value)
    {
        canRoll = true;
        resultShown = true;
        isProcessingResult = false;
        OnResult?.Invoke(value);
    }

    IEnumerator ResultPopupAnimation()
    {
        IsResultAnimating = true;
        Quaternion startRot = transform.rotation;
        Vector3 startPos = transform.position;
        float t = 0f;

        while (t < popupDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, t / popupDuration);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * popupScale, p);
            transform.rotation = Quaternion.Lerp(startRot, Quaternion.identity, p);
            transform.position = Vector3.Lerp(startPos, lockedPosition, p);
            yield return null;
        }
        transform.position = lockedPosition;
        transform.rotation = Quaternion.identity;

        t = 0f;
        while (t < popupDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, t / popupDuration);
            transform.localScale = Vector3.Lerp(originalScale * popupScale, originalScale, p);
            yield return null;
        }
        transform.localScale = originalScale;
        IsResultAnimating = false;
    }

    IEnumerator PunchScaleEffect()
    {
        float timer = 0f;
        float duration = 0.2f;
        Vector3 baseScale = originalScale;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float s = 1f + (Mathf.Sin(t * Mathf.PI) * 0.3f);
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;
    }

    public bool IsBusy => isProcessingResult || IsResultAnimating;
}