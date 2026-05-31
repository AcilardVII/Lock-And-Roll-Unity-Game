using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIHealthManager : MonoBehaviour
{
    [Header("UI Health Bar")]
    public Image healthFillImage;
    public TextMeshProUGUI healthText;

    [Header("Shake Ayarları")]
    public RectTransform shakeRoot;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 8f;

    [Header("Flash Ayarları")]
    public Image flashImage;
    public float flashDuration = 0.15f;

    [Header("Can Değerleri (INT)")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Bar Smooth")]
    public float reduceSpeed = 3f;

    [Header("Damage'da Bar Davranışı")]
    [Tooltip("Hasar aldığında fillAmount'ı aynı frame’de garanti güncelle (görsel bug fix).")]
    public bool immediateBarUpdateOnDamage = true;

    [Header("Dice / Section")]
    public DiceManager diceManager;

    private bool isDead = false;
    private float targetFill = 1f;

    private Vector3 originalShakePos;
    private Coroutine shakeRoutine;
    private Coroutine flashRoutine;

    void Start()
    {
        currentHealth = maxHealth;
        targetFill = 1f;

        if (healthFillImage != null)
            healthFillImage.fillAmount = 1f;

        if (shakeRoot != null)
            originalShakePos = shakeRoot.localPosition;

        if (flashImage != null)
        {
            Color c = flashImage.color;
            c.a = 0f;
            flashImage.color = c;
        }

        isDead = false;
        UpdateHealthText();
    }

    void Update()
    {
        if (healthFillImage != null && !Mathf.Approximately(healthFillImage.fillAmount, targetFill))
        {
            healthFillImage.fillAmount = Mathf.Lerp(
                healthFillImage.fillAmount,
                targetFill,
                Time.deltaTime * reduceSpeed
            );
        }
    }

    
    public void SetHealth(int value)
    {
        maxHealth = Mathf.Max(1, value);
        currentHealth = maxHealth;

        isDead = false;

        targetFill = 1f;
        if (healthFillImage != null)
            healthFillImage.fillAmount = 1f;

        UpdateHealthText();
    }

    
    public void ScaleHealth(float multiplier)
    {
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * multiplier));
        currentHealth = maxHealth;

        isDead = false;

        targetFill = 1f;
        if (healthFillImage != null)
            healthFillImage.fillAmount = 1f;

        UpdateHealthText();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        int dmg = Mathf.Max(0, Mathf.RoundToInt(damage));
        currentHealth -= dmg;
        if (currentHealth < 0) currentHealth = 0;

        targetFill = (maxHealth <= 0)
            ? 0f
            : Mathf.Clamp01((float)currentHealth / (float)maxHealth);

        
        if (immediateBarUpdateOnDamage && healthFillImage != null)
            healthFillImage.fillAmount = targetFill;

        UpdateHealthText();

        if (shakeRoot != null)
        {
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(DoShake());
        }

        if (flashImage != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(DoFlash());
        }

        if (!isDead && currentHealth <= 0)
        {
            isDead = true;
            if (diceManager != null)
                diceManager.OnEnemyDied();
        }
    }

    void UpdateHealthText()
    {
        if (healthText != null)
            healthText.text = currentHealth.ToString();
    }

    IEnumerator DoShake()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shakeDuration;

            Vector2 offset = Random.insideUnitCircle * shakeMagnitude * (1f - t);
            shakeRoot.localPosition = originalShakePos + new Vector3(offset.x, offset.y, 0f);

            yield return null;
        }

        shakeRoot.localPosition = originalShakePos;
        shakeRoutine = null;
    }

    IEnumerator DoFlash()
    {
        if (flashImage == null) yield break;

        float half = flashDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);

            Color c = flashImage.color;
            c.a = Mathf.Lerp(0f, 1f, t);
            flashImage.color = c;

            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);

            Color c = flashImage.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            flashImage.color = c;

            yield return null;
        }

        Color end = flashImage.color;
        end.a = 0f;
        flashImage.color = end;

        flashRoutine = null;
    }

    public bool IsDead => isDead;
}
