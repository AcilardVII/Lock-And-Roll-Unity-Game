using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CoinDragSnap : MonoBehaviour
{
    public enum JokerRarity { Common, Uncommon, Rare, Legendary }

    [Header("📝 Joker Kimliği")]
    public string jokerTitle = "New Joker";
    public JokerRarity rarity = JokerRarity.Common;
    public int shopPrice = 5;
    [TextArea] public string shopDescription;

    // ----------------------------------------------------------------------------
    //  TRIGGER
    // ----------------------------------------------------------------------------
    public enum TriggerType
    {
        AlwaysActive, N_Of_A_Kind, SpecificFaceCount, Straight, TotalSumGreater, TotalSumLess, ContainsFace,
        Odd, Even, AllDifferent, PerRemainingReroll, PerDiceValue, Chance, OnlyOneDie, LastRound, FirstRound,
        HighestDieIs, LowestDieIs, HighestDieDynamic, LowestDieDynamic, AllDiceAreX
    }

    [Header("Tetikleyici Ayarları")]
    public TriggerType triggerType = TriggerType.AlwaysActive;
    public int targetFaceValue = 6;
    public int requiredCount = 2;
    public int targetSum = 20;
    [Range(0, 100)] public float chancePercent = 50f;

    [Header("Toplam (Yüzdelik Modu)")]
    public bool usePercentageForTotalSum = false;
    [Range(0, 100)] public float targetSumPercentage = 50f;

    [Header("Koşul Ayarı")]
    public bool requireAllDice = true;

    // ----------------------------------------------------------------------------
    //  ÖDÜL 
    // ----------------------------------------------------------------------------
    [Header("Puan Ödülleri")]
    public int bonusChips = 0;
    public float bonusMultAdd = 0f;
    public float multiplier = 1f;
    public int moneyReward = 0;
    public int rerollReward = 0;
    public int roundReward = 0;

    // ----------------------------------------------------------------------------
    //  PASİF BONUSLAR
    // ----------------------------------------------------------------------------
    [Header("Pasif Bonuslar (Tur Başı)")]
    public int passiveRerolls = 0;
    public int passiveRounds = 0;
    public bool lockRerollAbility = false;

    // ----------------------------------------------------------------------------
    //  HESAPLAMA MODU & ÖZEL YETENEKLER
    // ----------------------------------------------------------------------------
    [Header("Hesaplama ve Özellikler")]
    public bool rewardPerItem = false;
    public bool useDiceValueAsXMult = false;
    public bool createsClone = false;

    [Header("Consume Ayarı")]
    public bool consumeOnTrigger = true;

    // ============================================================================
    // GÖRSEL DEĞİŞKENLER
    // ============================================================================
    [Header("Görsel Efektler")]
    public Sprite thinSprite;
    public Sprite normalSprite;
    public float snapRadius = 0.8f;
    public float snapDuration = 0.1f;
    public float hoverOffsetY = 0.2f;
    public float hoverDuration = 0.15f;
    public float dragTiltAngle = 10f;
    public float dragAnimSpeed = 6f;
    public float lockScale = 1.1f;
    public float lockDuration = 0.15f;
    public Color lockColor = Color.white;
    public float jokerPulseScale = 1.15f;
    public float jokerPulseDuration = 0.35f;
    public float jokerWobbleAngle = 12f;
    public float jokerWobbleSpeed = 18f;

    [Header("Hata / Başarısızlık Efekti")]
    public Color failureColor = Color.gray;
    public float failureShakeAmount = 0.1f;
    public float failureDuration = 0.4f;

    private CoinSlot[] slots;
    private JokerSlot[] snapPoints;
    private SpriteRenderer sr;
    private Camera cam;
    private bool isDragging = false;
    private bool isSnapping = false;
    private Vector3 dragOffset;
    private CoinSlot currentSlot;
    private JokerSlot currentJokerSlot;
    private Vector3 slotRestPos;
    private Coroutine hoverCoroutine;
    private Coroutine dragAnimCoroutine;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Coroutine lockAnimCoroutine;
    private Color originalColor;
    private bool isLockedByJoker = false;

    public bool isPreview = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        originalRotation = transform.rotation;
        if (sr != null) originalColor = sr.color;

        if (!isPreview && thinSprite != null && sr != null) sr.sprite = thinSprite;

        slotRestPos = transform.position;

        if (!isPreview) FindAndSortSlots();
    }

    
    public void InstantSnapToJokerSlot(JokerSlot slot)
    {
        if (currentSlot != null) currentSlot.Clear();
        if (currentJokerSlot != null && currentJokerSlot != slot) currentJokerSlot.Clear();

        currentSlot = null;
        currentJokerSlot = slot;
        slot.SetOccupant(this);
        slotRestPos = slot.transform.position;
        transform.position = slot.transform.position;

        
        if (normalSprite != null && sr != null) sr.sprite = normalSprite;
    }

    private void FindAndSortSlots()
    {
        slots = FindObjectsByType<CoinSlot>(FindObjectsSortMode.None);
        if (slots != null && slots.Length > 0)
            System.Array.Sort(slots, (a, b) => a.slotID.CompareTo(b.slotID));

        DiceManager dm = FindFirstObjectByType<DiceManager>();
        List<JokerSlot> tempJokerSlots = new List<JokerSlot>();

        if (dm != null && dm.snapPoints != null)
        {
            foreach (var t in dm.snapPoints)
            {
                if (t != null)
                {
                    JokerSlot js = t.GetComponent<JokerSlot>();
                    if (js != null) tempJokerSlots.Add(js);
                }
            }
        }

        if (tempJokerSlots.Count == 0)
        {
            var foundInScene = FindObjectsByType<JokerSlot>(FindObjectsSortMode.None);
            if (foundInScene != null) tempJokerSlots.AddRange(foundInScene);
        }

        snapPoints = tempJokerSlots.ToArray();
        if (snapPoints != null && snapPoints.Length > 0)
            System.Array.Sort(snapPoints, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
    }

    public void CalculateJokerEffect(List<DiceThrow> diceList, int currentRound, int maxRounds, int remainingRerolls, ref int totalChips, ref float totalMult, ref int moneyGain, ref int rerollGain, ref int roundGain, out bool triggered)
    {
        triggered = false;
        List<int> values = new List<int>();
        foreach (var d in diceList)
        {
            if (d.HasResult)
            {
                int val = 0;
                if (int.TryParse(d.diceText.text, out val)) values.Add(val);
            }
        }

        bool needsDice = (triggerType != TriggerType.AlwaysActive &&
                          triggerType != TriggerType.LastRound &&
                          triggerType != TriggerType.FirstRound &&
                          triggerType != TriggerType.PerRemainingReroll);

        if (needsDice && values.Count == 0) return;

        var groups = values.GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());
        int maxCountOfAnyFace = groups.Count > 0 ? groups.Values.Max() : 0;
        int totalSum = values.Sum();

        bool conditionMet = false;
        int dynamicMultiplierCount = 1;

        switch (triggerType)
        {
            case TriggerType.AlwaysActive: conditionMet = true; break;
            case TriggerType.N_Of_A_Kind: if (maxCountOfAnyFace >= requiredCount) conditionMet = true; break;
            case TriggerType.SpecificFaceCount: if (groups.ContainsKey(targetFaceValue) && groups[targetFaceValue] >= requiredCount) { conditionMet = true; if (rewardPerItem) dynamicMultiplierCount = groups[targetFaceValue]; } break;
            case TriggerType.Straight: if (values.Count >= 5) { values.Sort(); bool isStraight = true; for (int i = 0; i < values.Count - 1; i++) if (values[i + 1] != values[i] + 1) isStraight = false; conditionMet = isStraight; } break;
            case TriggerType.TotalSumGreater: if (usePercentageForTotalSum) { int maxPossible = 0; foreach (var d in diceList) maxPossible += (d.maxRange - 1); float threshold = maxPossible * (targetSumPercentage / 100f); conditionMet = totalSum > threshold; } else { conditionMet = totalSum > targetSum; } break;
            case TriggerType.TotalSumLess: if (usePercentageForTotalSum) { int maxPossible = 0; foreach (var d in diceList) maxPossible += (d.maxRange - 1); float threshold = maxPossible * (targetSumPercentage / 100f); conditionMet = totalSum < threshold; } else { conditionMet = totalSum < targetSum; } break;
            case TriggerType.ContainsFace: int count = values.Count(v => v == targetFaceValue); if (count > 0) { conditionMet = true; if (rewardPerItem) dynamicMultiplierCount = count; } break;
            case TriggerType.Odd: int odds = values.Count(v => v % 2 != 0); if (values.Count > 0) { if (requireAllDice) conditionMet = (odds == values.Count); else conditionMet = (odds > 0); if (conditionMet && rewardPerItem) dynamicMultiplierCount = odds; } break;
            case TriggerType.Even: int evens = values.Count(v => v % 2 == 0); if (values.Count > 0) { if (requireAllDice) conditionMet = (evens == values.Count); else conditionMet = (evens > 0); if (conditionMet && rewardPerItem) dynamicMultiplierCount = evens; } break;
            case TriggerType.AllDifferent: if (values.Count > 0 && values.Distinct().Count() == values.Count) { conditionMet = true; if (rewardPerItem) dynamicMultiplierCount = values.Count; } break;
            case TriggerType.PerRemainingReroll: conditionMet = true; dynamicMultiplierCount = remainingRerolls; break;
            case TriggerType.PerDiceValue: if (values.Count > 0) { conditionMet = true; dynamicMultiplierCount = totalSum; } break;
            case TriggerType.Chance: float rnd = Random.Range(0f, 100f); conditionMet = (rnd <= chancePercent); break;
            case TriggerType.OnlyOneDie: if (values.Count == 1) { conditionMet = true; dynamicMultiplierCount = values[0]; } break;
            case TriggerType.LastRound: conditionMet = (currentRound <= 0); break;
            case TriggerType.FirstRound: conditionMet = (currentRound == maxRounds - 1); break;
            case TriggerType.HighestDieIs: if (values.Count > 0 && values.Max() == targetFaceValue) conditionMet = true; break;
            case TriggerType.LowestDieIs: if (values.Count > 0 && values.Min() == targetFaceValue) conditionMet = true; break;
            case TriggerType.HighestDieDynamic: if (values.Count > 0) { conditionMet = true; dynamicMultiplierCount = values.Max(); } break;
            case TriggerType.LowestDieDynamic: if (values.Count > 0) { conditionMet = true; dynamicMultiplierCount = values.Min(); } break;
            case TriggerType.AllDiceAreX: if (values.Count > 0 && values.All(v => v == targetFaceValue)) { conditionMet = true; if (rewardPerItem) dynamicMultiplierCount = values.Count; } break;
        }

        if (conditionMet)
        {
            triggered = true;
            int finalChips = bonusChips;
            float finalMultAdd = bonusMultAdd;
            int finalMoney = moneyReward;
            int finalReroll = rerollReward;
            int finalRound = roundReward;

            bool isDynamicType = (triggerType == TriggerType.HighestDieDynamic || triggerType == TriggerType.LowestDieDynamic || triggerType == TriggerType.OnlyOneDie);

            if (rewardPerItem || triggerType == TriggerType.PerRemainingReroll || triggerType == TriggerType.PerDiceValue)
            {
                finalChips *= dynamicMultiplierCount;
                finalMultAdd *= dynamicMultiplierCount;
                finalMoney *= dynamicMultiplierCount;
                finalReroll *= dynamicMultiplierCount;
                finalRound *= dynamicMultiplierCount;
            }

            if (finalChips > 0) totalChips += finalChips;
            if (finalMultAdd > 0) totalMult += finalMultAdd;

            if (useDiceValueAsXMult && isDynamicType) totalMult *= dynamicMultiplierCount;
            else if (multiplier > 1.01f) totalMult *= multiplier;

            if (finalMoney > 0) moneyGain += finalMoney;
            if (finalReroll > 0) rerollGain += finalReroll;
            if (finalRound > 0) roundGain += finalRound;
        }
    }

    public bool CheckCloneCondition(int diceCount) => (triggerType == TriggerType.OnlyOneDie && diceCount == 1 && createsClone);

    void OnMouseDown()
    {
        if (isPreview || isLockedByJoker || isSnapping) return;
        if (currentJokerSlot != null && currentJokerSlot.isLocked) return;

        if (cam == null) cam = Camera.main;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        dragOffset = transform.position - mouseWorld;
        isDragging = true;
        StopHover();
        StartDragAnimation();
        if (sr != null && normalSprite != null) sr.sprite = normalSprite;
        if (currentSlot != null) { currentSlot.Clear(); currentSlot = null; }
        if (currentJokerSlot != null) { currentJokerSlot.Clear(); currentJokerSlot = null; }
    }

    void OnMouseDrag()
    {
        if (isPreview || !isDragging || isLockedByJoker) return;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, mouseWorld + dragOffset, Time.deltaTime * 25f);
    }

    void OnMouseUp()
    {
        if (isPreview || !isDragging) return;
        isDragging = false;
        StopDragAnimation();
        if ((slots == null || slots.Length == 0) || (snapPoints == null || snapPoints.Length == 0)) FindAndSortSlots();

        float bestDist = float.MaxValue;
        CoinSlot bestCoinSlot = null;
        JokerSlot bestJokerSlot = null;

        if (slots != null)
        {
            foreach (var s in slots)
            {
                if (s == null) continue;
                if (s.IsOccupied && s.occupant != this) continue;
                float d = Vector2.Distance(transform.position, s.transform.position);
                if (d < bestDist) { bestDist = d; bestCoinSlot = s; bestJokerSlot = null; }
            }
        }
        if (snapPoints != null)
        {
            foreach (var js in snapPoints)
            {
                if (js == null) continue;
                if (js.IsOccupied && js.occupant != this) continue;
                if (js.isLocked) continue;
                float d = Vector2.Distance(transform.position, js.transform.position);
                if (d <= bestDist) { bestDist = d; bestCoinSlot = null; bestJokerSlot = js; }
            }
        }

        if (bestDist <= snapRadius)
        {
            if (bestCoinSlot != null) StartCoroutine(SnapToCoinSlot(bestCoinSlot));
            else if (bestJokerSlot != null) StartCoroutine(SnapToJokerSlot(bestJokerSlot));
        }
        else SnapToFirstFreeCoinSlotAnimated();
    }

    void OnMouseEnter() { if (!isPreview && IsInSlot && !isDragging && !isLockedByJoker && !isSnapping) StartHoverUp(); }
    void OnMouseExit() { if (!isPreview && IsInSlot && !isDragging && !isLockedByJoker && !isSnapping) StartHoverDown(); }

    public void StartHoverUp() { if (!IsInSlot) return; if (hoverCoroutine != null) StopCoroutine(hoverCoroutine); Vector3 targetPos = slotRestPos + Vector3.up * hoverOffsetY; hoverCoroutine = StartCoroutine(HoverRoutine(transform.position, targetPos)); }
    public void StartHoverDown() { if (!IsInSlot) return; if (hoverCoroutine != null) StopCoroutine(hoverCoroutine); Vector3 targetPos = slotRestPos; hoverCoroutine = StartCoroutine(HoverRoutine(transform.position, targetPos)); }
    public void StopHover() { if (hoverCoroutine != null) { StopCoroutine(hoverCoroutine); hoverCoroutine = null; } if (IsInSlot && !isSnapping) transform.position = slotRestPos; }
    IEnumerator HoverRoutine(Vector3 start, Vector3 end) { float t = 0f; while (t < hoverDuration) { t += Time.deltaTime; float lerp = t / hoverDuration; transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, lerp)); yield return null; } transform.position = end; hoverCoroutine = null; }

    public IEnumerator SnapToCoinSlot(CoinSlot slot)
    {
        isSnapping = true;
        if (currentSlot != null && currentSlot != slot) currentSlot.Clear();
        if (currentJokerSlot != null) currentJokerSlot.Clear();
        currentSlot = slot;
        currentJokerSlot = null;
        slot.SetOccupant(this);
        slotRestPos = slot.transform.position;
        Vector3 start = transform.position;
        Vector3 end = slot.transform.position;
        float t = 0f;
        while (t < snapDuration) { t += Time.deltaTime; float lerp = t / snapDuration; transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, lerp)); yield return null; }
        transform.position = end;
        if (thinSprite != null && sr != null) sr.sprite = thinSprite;
        isSnapping = false;
    }

    IEnumerator SnapToJokerSlot(JokerSlot slot)
    {
        isSnapping = true;
        if (currentSlot != null) currentSlot.Clear();
        if (currentJokerSlot != null && currentJokerSlot != slot) currentJokerSlot.Clear();
        currentSlot = null;
        currentJokerSlot = slot;
        slot.SetOccupant(this);
        slotRestPos = slot.transform.position;
        Vector3 start = transform.position;
        Vector3 end = slot.transform.position;
        float t = 0f;
        while (t < snapDuration) { t += Time.deltaTime; float lerp = t / snapDuration; transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, lerp)); yield return null; }
        transform.position = end;
        if (normalSprite != null && sr != null) sr.sprite = normalSprite;
        isSnapping = false;
    }

    void StartDragAnimation() { if (dragAnimCoroutine != null) StopCoroutine(dragAnimCoroutine); dragAnimCoroutine = StartCoroutine(DragAnimRoutine()); }
    void StopDragAnimation() { if (dragAnimCoroutine != null) { StopCoroutine(dragAnimCoroutine); dragAnimCoroutine = null; } transform.localScale = originalScale; transform.rotation = originalRotation; }
    IEnumerator DragAnimRoutine() { float angle = dragTiltAngle; float speed = dragAnimSpeed; while (isDragging) { float wobble = Mathf.Sin(Time.time * speed) * angle; transform.localScale = originalScale * 1.02f; transform.rotation = Quaternion.Euler(0f, 0f, wobble); yield return null; } transform.localScale = originalScale; transform.rotation = originalRotation; }
    public void LockFromJoker() { if (isLockedByJoker) return; isLockedByJoker = true; StopHover(); StopDragAnimation(); if (lockAnimCoroutine != null) StopCoroutine(lockAnimCoroutine); lockAnimCoroutine = StartCoroutine(LockAnim()); }
    IEnumerator LockAnim() { if (sr == null) yield break; Color targetColor = lockColor; Vector3 baseScale = originalScale; Vector3 peakScale = baseScale * lockScale; float halfDuration = lockDuration * 0.5f; float t = 0f; while (t < halfDuration) { t += Time.deltaTime; float lerp = t / halfDuration; sr.color = Color.Lerp(originalColor, targetColor, lerp); transform.localScale = Vector3.Lerp(baseScale, peakScale, Mathf.SmoothStep(0f, 1f, lerp)); yield return null; } yield return new WaitForSeconds(0.05f); t = 0f; while (t < halfDuration) { t += Time.deltaTime; float lerp = t / halfDuration; sr.color = Color.Lerp(targetColor, originalColor, lerp); transform.localScale = Vector3.Lerp(peakScale, baseScale, Mathf.SmoothStep(0f, 1f, lerp)); yield return null; } transform.localScale = baseScale; sr.color = originalColor; }

    public IEnumerator PlayJokerActivationAnim()
    {
        float elapsed = 0f;
        while (elapsed < jokerPulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jokerPulseDuration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            float scaleFactor = Mathf.Lerp(1f, jokerPulseScale, pulse);
            float wobble = Mathf.Sin(t * jokerWobbleSpeed) * jokerWobbleAngle;
            transform.localScale = originalScale * scaleFactor;
            transform.rotation = Quaternion.Euler(0f, 0f, wobble);
            yield return null;
        }
        transform.localScale = originalScale;
        transform.rotation = originalRotation;
    }

    public IEnumerator PlayFailureAnim()
    {
        if (sr == null) yield break;
        Vector3 basePos = IsInSlot ? slotRestPos : transform.position;
        Quaternion baseRot = originalRotation;
        float elapsed = 0f;
        while (elapsed < failureDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / failureDuration;
            if (t < 0.2f) sr.color = Color.Lerp(originalColor, failureColor, t / 0.2f);
            else sr.color = Color.Lerp(failureColor, originalColor, (t - 0.2f) / 0.8f);
            float shake = Mathf.Sin(elapsed * 50f) * failureShakeAmount * (1f - t);
            transform.position = basePos + new Vector3(shake, 0f, 0f);
            yield return null;
        }
        transform.position = basePos;
        transform.rotation = baseRot;
        sr.color = originalColor;
    }

    public bool IsInAnySlot => (currentSlot != null || currentJokerSlot != null);
    public bool IsInSlot => IsInAnySlot;

    public void SnapToFirstFreeCoinSlotAnimated()
    {
        if (isPreview) return;
        FindAndSortSlots();
        if (slots != null) { for (int i = 0; i < slots.Length; i++) { if (slots[i] != null && !slots[i].IsOccupied) { StartCoroutine(SnapToCoinSlot(slots[i])); return; } } }
        DiceManager dm = FindFirstObjectByType<DiceManager>();
        if (dm != null && dm.snapPoints != null) { for (int i = 0; i < dm.snapPoints.Length; i++) { if (dm.snapPoints[i] == null) continue; JokerSlot js = dm.snapPoints[i].GetComponent<JokerSlot>(); if (js != null && !js.IsOccupied && !js.isLocked) { StartCoroutine(SnapToJokerSlot(js)); return; } } }
    }

    
    public void InstantSnapToCoinSlot(CoinSlot slot)
    {
        if (currentJokerSlot != null) currentJokerSlot.Clear();
        if (currentSlot != null && currentSlot != slot) currentSlot.Clear();

        currentSlot = slot;
        currentJokerSlot = null;
        slot.SetOccupant(this);
        transform.position = slot.transform.position;

        
        if (thinSprite != null && sr != null) sr.sprite = thinSprite;
    }
}