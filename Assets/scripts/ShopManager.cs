using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ShopManager : MonoBehaviour
{
    [System.Serializable]
    public class ShopSlotUI
    {
        [Header("UI Elemanları")]
        public GameObject root;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI priceText;
        public Button buyButton;

        [HideInInspector] public GameObject currentPreview;
    }

    [Header("Bağlantılar")]
    public DiceManager diceManager;

    private SingleSceneBackground backgroundManager;

    [Header("UI Panel Ayarları")]
    public RectTransform shopPanel;
    public Vector2 closedPos;
    public Vector2 openPos;
    public float slideDuration = 0.4f;

    [Header("Joker Market Settings")]
    public ShopSlotUI[] jokerSlots;
    public GameObject[] allJokerPrefabs;
    public Transform jokerSpawnPoint;
    public Transform jokerParent;
    public SlotLeverUI jokerRerollLever;
    public TextMeshProUGUI jokerRerollPriceText;
    public int jokerRerollCost = 5;

    [Header("Rarity Ayarları (%)")]
    public float commonChance = 50f;
    public float uncommonChance = 30f;
    public float rareChance = 15f;
    public float legendaryChance = 5f;

    [Header("Joker Görünüm Ayarı")]
    public float jokerPreviewScale = 1.0f;
    public float jokerVerticalOffset = 0.8f;
    private int[] currentJokerIndices;

    [Header("Forge System (Left)")]
    public ShopForgeSlot forgeSlot;
    public TextMeshProUGUI forgeInfoText;
    public SlotLeverUI forgeRerollLever;
    public TextMeshProUGUI forgeRerollPriceText;
    public Button forgeConfirmButton;
    public TextMeshProUGUI forgePriceText;

    [Header("Forge Settings")]
    public int baseForgeCost = 10;
    public int baseRerollElementCost = 2;
    public List<DiceEnchantment> possibleEnchantments;

    private DiceEnchantment currentGeneratedEnchantment;
    private int lastForgeIndex = -1;
    private DiceThrow currentForgeDice;
    private DiceEnchantment backupEnchantment;
    private bool isForgeBuying = false;

    [Header("Random Dice Market (Right)")]
    public Transform randomDiceSpawnPoint;
    public TextMeshProUGUI randomDiceInfoText;
    public Button randomDiceBuyButton;
    public TextMeshProUGUI randomDicePriceText;
    public SlotLeverUI rerollLever;
    public TextMeshProUGUI randomDiceRerollPriceText;

    [Header("Dice Market Settings")]
    public GameObject[] allDicePrefabs;
    public int randomDiceRerollCost = 5;

    [Header("Rigged Dice Chance (%)")]
    [Range(0, 100)]
    public float riggedChance = 20f;

    private GameObject currentMarketDiceObj;
    private DiceThrow currentMarketDiceScript;
    private int currentMarketDicePrice;
    private GameObject currentMarketPrefab;
    private int lastMarketDiceIndex = -1;
    private bool isOpen = false;
    private Coroutine slideRoutine;

    void Awake()
    {
        if (jokerSlots != null) currentJokerIndices = new int[jokerSlots.Length];
    }

    void Start()
    {
        backgroundManager = FindFirstObjectByType<SingleSceneBackground>();
        if (shopPanel != null) shopPanel.anchoredPosition = closedPos;

        if (forgeRerollLever != null) forgeRerollLever.OnLeverPulled.AddListener(OnForgeRerollClicked);
        if (forgeConfirmButton != null) forgeConfirmButton.onClick.AddListener(OnForgeConfirmClicked);
        if (randomDiceBuyButton != null) randomDiceBuyButton.onClick.AddListener(OnBuyRandomDiceClicked);
        if (rerollLever != null) rerollLever.OnLeverPulled.AddListener(OnRerollMarketClicked);
        if (jokerRerollLever != null) jokerRerollLever.OnLeverPulled.AddListener(OnJokerRerollClicked);

        if (jokerRerollPriceText != null) jokerRerollPriceText.text = "$" + jokerRerollCost;

        UpdateForgeUI(false);
        if (forgeSlot != null) forgeSlot.shopManager = this;
    }

    public void OpenShop(int sectionLevel, bool isBossSection)
    {
        if (backgroundManager != null) backgroundManager.SwitchToShop();

        var drags = FindObjectsByType<diceDragScript>(FindObjectsSortMode.None);
        foreach (var drag in drags) { if (drag != null) drag.SnapBackToCurrentSlot(); }

        if (diceManager != null) diceManager.SetShopOpen(true);

        if (shopPanel != null)
        {
            shopPanel.gameObject.SetActive(true);
            StartSlide(true);
        }

        GenerateNewMarketDice(false);
        GenerateJokers(false);
        GenerateRandomEnchantment();

        if (forgeSlot != null && forgeSlot.currentDice != null)
        {
            if (diceManager != null) diceManager.ReturnSingleDiceToTray(forgeSlot.currentDice);
            forgeSlot.ClearSlot();
        }
        UpdateForgeUI(false);
    }

    public void OnJokerRerollClicked()
    {
        if (diceManager != null && diceManager.TrySpendMoney(jokerRerollCost))
            GenerateJokers(true);
    }

    void GenerateJokers(bool useDelay = false) { if (useDelay) StartCoroutine(GenerateJokersRoutine()); else SpawnJokersImmediate(); }

    IEnumerator GenerateJokersRoutine()
    {
        if (jokerSlots == null) yield break;
        for (int i = 0; i < jokerSlots.Length; i++)
        {
            var slot = jokerSlots[i];
            if (slot.currentPreview != null) Destroy(slot.currentPreview);
            if (slot.priceText != null) slot.priceText.text = "-";
            if (slot.nameText != null) slot.nameText.text = "...";
            if (slot.buyButton != null) slot.buyButton.interactable = false;
        }
        yield return new WaitForSeconds(0.2f);
        SpawnJokersImmediate();
    }

    void SpawnJokersImmediate()
    {
        if (jokerSlots == null) return;
        for (int i = 0; i < jokerSlots.Length; i++)
        {
            try
            {
                var slot = jokerSlots[i];
                if (slot.root != null) slot.root.SetActive(true);
                if (slot.currentPreview != null) Destroy(slot.currentPreview);
                GameObject prefab = GetRandomJokerByRarity();
                if (prefab == null) continue;
                currentJokerIndices[i] = System.Array.IndexOf(allJokerPrefabs, prefab);
                var script = prefab.GetComponent<CoinDragSnap>();
                if (slot.priceText != null) slot.priceText.text = "$" + script.shopPrice;
                if (slot.nameText != null)
                {
                    slot.nameText.text = script.jokerTitle;
                    var re = slot.nameText.GetComponent<RarityTextEffect>();
                    if (re == null) re = slot.nameText.gameObject.AddComponent<RarityTextEffect>();
                    re.SetRarity(script.rarity);
                }
                slot.currentPreview = Instantiate(prefab, slot.root.transform);
                PrepareJokerPreview(slot.currentPreview);
                var tt = slot.root.GetComponent<TooltipTrigger>();
                if (tt != null) { var spawnedScript = slot.currentPreview.GetComponent<CoinDragSnap>(); if (spawnedScript != null) tt.SetJokerDataFromPrefab(spawnedScript); }
                if (slot.buyButton != null) { slot.buyButton.onClick.RemoveAllListeners(); int index = i; slot.buyButton.interactable = true; var btnText = slot.buyButton.GetComponentInChildren<TextMeshProUGUI>(); if (btnText != null) btnText.text = "BUY"; slot.buyButton.onClick.AddListener(() => BuyJoker(index)); }
            }
            catch (System.Exception e) { Debug.LogError($"Slot {i} Error: {e.Message}"); }
        }
    }

    private GameObject GetRandomJokerByRarity()
    {
        float roll = Random.Range(0f, 100f);
        CoinDragSnap.JokerRarity rarity = CoinDragSnap.JokerRarity.Common;
        if (roll < legendaryChance) rarity = CoinDragSnap.JokerRarity.Legendary;
        else if (roll < legendaryChance + rareChance) rarity = CoinDragSnap.JokerRarity.Rare;
        else if (roll < legendaryChance + rareChance + uncommonChance) rarity = CoinDragSnap.JokerRarity.Uncommon;

        var pool = allJokerPrefabs.Where(j => j != null && j.GetComponent<CoinDragSnap>().rarity == rarity).ToList();
        if (pool.Count == 0) pool = allJokerPrefabs.Where(j => j != null && j.GetComponent<CoinDragSnap>().rarity == CoinDragSnap.JokerRarity.Common).ToList();
        return (pool.Count > 0) ? pool[Random.Range(0, pool.Count)] : allJokerPrefabs[0];
    }

    void PrepareJokerPreview(GameObject obj)
    {
        if (obj.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb)) rb.simulated = false;
        if (obj.TryGetComponent<Collider2D>(out Collider2D col)) { col.enabled = true; if (col is BoxCollider2D box) box.size = new Vector2(0.8f, 0.8f); }
        if (obj.TryGetComponent<CoinDragSnap>(out CoinDragSnap c)) { c.enabled = true; c.isPreview = true; SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>(); if (sr != null && c.normalSprite != null) sr.sprite = c.normalSprite; }
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.localPosition = new Vector3(0, jokerVerticalOffset, 0);
        obj.transform.localScale = Vector3.one * jokerPreviewScale;
        Canvas canvas = obj.GetComponent<Canvas>();
        if (canvas == null) canvas = obj.GetComponentInChildren<Canvas>();
        if (canvas != null) { canvas.overrideSorting = true; canvas.sortingOrder = 2000; }
        SpriteRenderer[] allRenderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var renderer in allRenderers) { renderer.sortingLayerName = "UI"; renderer.sortingOrder = 2000; }
    }

    public void BuyJoker(int slotIndex)
    {
        int prefabIndex = currentJokerIndices[slotIndex];
        GameObject prefab = allJokerPrefabs[prefabIndex];
        int price = prefab.GetComponent<CoinDragSnap>().shopPrice;
        if (diceManager != null && diceManager.TrySpendMoney(price))
        {
            GameObject go = Instantiate(prefab, jokerSpawnPoint.position, Quaternion.identity, jokerParent);
            go.GetComponent<CoinDragSnap>().SnapToFirstFreeCoinSlotAnimated();
            if (jokerSlots[slotIndex].root != null) jokerSlots[slotIndex].root.SetActive(false);
            if (jokerSlots[slotIndex].currentPreview != null) Destroy(jokerSlots[slotIndex].currentPreview);
        }
    }

    public void OnForgeSlotOccupied(diceDragScript dice)
    {
        DiceThrow d = dice.GetComponent<DiceThrow>();
        if (d != null && d.currentRig != DiceThrow.RiggedType.None)
        {
            if (forgeInfoText != null) forgeInfoText.text = "<color=red>CANNOT FORGE\nRIGGED DICE!</color>";
            if (forgeConfirmButton != null) forgeConfirmButton.interactable = false;
            if (forgeRerollLever != null) forgeRerollLever.enabled = false;
            if (forgePriceText != null) forgePriceText.text = "-";
            if (forgeRerollPriceText != null) forgeRerollPriceText.text = "-";
            return;
        }

        UpdateForgeUI(true);
        currentForgeDice = d;
        if (currentForgeDice != null)
        {
            backupEnchantment = currentForgeDice.activeEnchantment;
            currentForgeDice.activeEnchantment = currentGeneratedEnchantment;
            currentForgeDice.UpdateVisuals();
        }
    }

    public void OnForgeSlotCleared()
    {
        if (!isForgeBuying && currentForgeDice != null)
        {
            currentForgeDice.activeEnchantment = backupEnchantment;
            currentForgeDice.UpdateVisuals();
        }
        currentForgeDice = null;
        UpdateForgeUI(false);
    }

    void GenerateRandomEnchantment()
    {
        if (possibleEnchantments == null || possibleEnchantments.Count == 0) return;
        int idx = -1;
        if (possibleEnchantments.Count > 1)
        {
            do { idx = Random.Range(0, possibleEnchantments.Count); } while (idx == lastForgeIndex);
        }
        else idx = 0;

        lastForgeIndex = idx;
        currentGeneratedEnchantment = possibleEnchantments[idx];
    }

    void UpdateForgeUI(bool hasDice)
    {
        if (currentGeneratedEnchantment == null) GenerateRandomEnchantment();

        if (hasDice && currentGeneratedEnchantment != null)
        {
            if (forgeInfoText != null) forgeInfoText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(currentGeneratedEnchantment.displayColor)}>{currentGeneratedEnchantment.elementName}</color>\n\n{currentGeneratedEnchantment.description}";
            if (forgeConfirmButton != null) forgeConfirmButton.interactable = true;
            if (forgeRerollLever != null) forgeRerollLever.enabled = true;
            if (forgePriceText != null) forgePriceText.text = "$" + baseForgeCost;
            if (forgeRerollPriceText != null) forgeRerollPriceText.text = "$" + baseRerollElementCost;
        }
        else
        {
            if (forgeInfoText != null) forgeInfoText.text = "Place a dice to Forge...";
            if (forgeConfirmButton != null) forgeConfirmButton.interactable = false;
            if (forgeRerollLever != null) forgeRerollLever.enabled = false;
            if (forgePriceText != null) forgePriceText.text = "-";
            if (forgeRerollPriceText != null) forgeRerollPriceText.text = "-";
        }
    }

    public void OnForgeRerollClicked()
    {
        if (diceManager != null && diceManager.TrySpendMoney(baseRerollElementCost))
            StartCoroutine(ForgeRerollRoutine());
    }

    IEnumerator ForgeRerollRoutine()
    {
        if (forgeInfoText != null) forgeInfoText.text = "...";
        if (forgeConfirmButton != null) forgeConfirmButton.interactable = false;
        yield return new WaitForSeconds(0.2f);

        GenerateRandomEnchantment();

        if (currentForgeDice != null)
        {
            currentForgeDice.activeEnchantment = currentGeneratedEnchantment;
            currentForgeDice.UpdateVisuals();
        }
        UpdateForgeUI(currentForgeDice != null);
    }

    public void OnForgeConfirmClicked()
    {
        if (forgeSlot == null || forgeSlot.currentDice == null) return;
        if (diceManager != null && diceManager.TrySpendMoney(baseForgeCost))
        {
            isForgeBuying = true;
            diceDragScript processedDice = forgeSlot.currentDice;
            forgeSlot.ClearSlot();
            diceManager.ReturnSingleDiceToTray(processedDice);
            isForgeBuying = false;
            GenerateRandomEnchantment();
        }
    }

    void GenerateNewMarketDice(bool useDelay = false) { if (useDelay) StartCoroutine(GenerateDiceRoutine()); else SpawnDiceImmediate(); }

    IEnumerator GenerateDiceRoutine()
    {
        if (currentMarketDiceObj != null) Destroy(currentMarketDiceObj);
        if (randomDiceInfoText != null) randomDiceInfoText.text = "...";
        if (randomDicePriceText != null) randomDicePriceText.text = "-";
        if (randomDiceBuyButton != null) randomDiceBuyButton.interactable = false;
        yield return new WaitForSeconds(0.2f);
        SpawnDiceImmediate();
    }

    void SpawnDiceImmediate()
    {
        if (currentMarketDiceObj != null) Destroy(currentMarketDiceObj);
        if (allDicePrefabs == null || allDicePrefabs.Length == 0) return;

        int idx = -1;
        if (allDicePrefabs.Length > 1) { do { idx = Random.Range(0, allDicePrefabs.Length); } while (idx == lastMarketDiceIndex); } else idx = 0;
        lastMarketDiceIndex = idx;

        GameObject prefab = allDicePrefabs[idx];
        currentMarketPrefab = prefab;

        if (randomDiceSpawnPoint != null)
        {
            currentMarketDiceObj = Instantiate(prefab, randomDiceSpawnPoint);
            currentMarketDiceObj.transform.localPosition = Vector3.zero;
            currentMarketDiceObj.transform.localScale = Vector3.one;

            PrepareMarketDicePreview(currentMarketDiceObj);

            currentMarketDiceScript = currentMarketDiceObj.GetComponent<DiceThrow>();
            currentMarketDicePrice = currentMarketDiceScript.purchasePrice;

            float roll = Random.Range(0f, 100f);
            if (roll <= riggedChance)
            {
                DiceThrow.RiggedType randomRig = (DiceThrow.RiggedType)Random.Range(1, 5);
                currentMarketDiceScript.currentRig = randomRig;
                if (randomRig == DiceThrow.RiggedType.AlwaysSpecific) currentMarketDiceScript.rigValue = Random.Range(currentMarketDiceScript.minRange, currentMarketDiceScript.maxRange + 1);
                else if (randomRig == DiceThrow.RiggedType.AddValue) currentMarketDiceScript.rigValue = Random.Range(1, 6);
                currentMarketDicePrice = Mathf.RoundToInt(currentMarketDicePrice * 1.5f);
            }
            else currentMarketDiceScript.currentRig = DiceThrow.RiggedType.None;

            currentMarketDiceScript.UpdateVisuals();

            TooltipTrigger tt = currentMarketDiceObj.GetComponent<TooltipTrigger>();
            if (tt != null) { tt.SetDiceDataFromPrefab(currentMarketDiceScript); tt.price = currentMarketDicePrice; }

            string headerText = (tt != null && !string.IsNullOrEmpty(tt.header)) ? tt.header : prefab.name;
            string contentText = "";

            if (currentMarketDiceScript.currentRig != DiceThrow.RiggedType.None)
            {
                string colorHex = "#FF4444";
                switch (currentMarketDiceScript.currentRig)
                {
                    case DiceThrow.RiggedType.AlwaysSpecific: contentText = $"<color={colorHex}>Always rolls {currentMarketDiceScript.rigValue}.</color>"; break;
                    case DiceThrow.RiggedType.AddValue: contentText = $"<color={colorHex}>Always adds +{currentMarketDiceScript.rigValue}.</color>"; break;
                    case DiceThrow.RiggedType.AlwaysMax: contentText = $"<color={colorHex}>Always rolls Max ({currentMarketDiceScript.maxRange}).</color>"; break;
                    case DiceThrow.RiggedType.AlwaysMin: contentText = $"<color={colorHex}>Always rolls Min ({currentMarketDiceScript.minRange}).</color>"; break;
                }
            }
            else { contentText = (tt != null && !string.IsNullOrEmpty(tt.content)) ? tt.content : $"Range: {currentMarketDiceScript.minRange}-{currentMarketDiceScript.maxRange}"; }

            if (randomDiceInfoText != null) randomDiceInfoText.text = $"<size=120%><b>{headerText}</b></size>\n\n{contentText}";
            if (randomDicePriceText != null) randomDicePriceText.text = "$" + currentMarketDicePrice;
            if (randomDiceRerollPriceText != null) randomDiceRerollPriceText.text = "$" + randomDiceRerollCost;
            if (randomDiceBuyButton != null) randomDiceBuyButton.interactable = true;
        }
    }

    void PrepareMarketDicePreview(GameObject obj)
    {
        obj.layer = LayerMask.NameToLayer("UI");
        if (obj.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb)) rb.simulated = false;
        if (obj.TryGetComponent<diceDragScript>(out diceDragScript drag)) { drag.enabled = true; drag.isPreview = true; drag.diceManager = this.diceManager; }
        Canvas canvas = obj.GetComponentInChildren<Canvas>();
        if (canvas != null) { canvas.overrideSorting = true; canvas.sortingOrder = 1800; }
    }

    public void OnBuyRandomDiceClicked()
    {
        if (currentMarketDiceObj == null) return;
        if (diceManager != null && diceManager.TrySpendMoney(currentMarketDicePrice))
        {
            DiceThrow purchasedDice = diceManager.SpawnDiceDirectToFirstFreeRollSlot(currentMarketPrefab, currentMarketDicePrice, diceManager.diceParent);
            if (purchasedDice != null)
            {
                if (currentMarketDiceScript != null)
                {
                    purchasedDice.currentRig = currentMarketDiceScript.currentRig;
                    purchasedDice.rigValue = currentMarketDiceScript.rigValue;
                    purchasedDice.UpdateVisuals();
                }
                Destroy(currentMarketDiceObj);
                currentMarketDiceObj = null;
                if (randomDiceInfoText != null) randomDiceInfoText.text = "SOLD OUT";
                if (randomDicePriceText != null) randomDicePriceText.text = "-";
                if (randomDiceBuyButton != null) randomDiceBuyButton.interactable = false;
            }
            else
            {
                
                diceManager.GainMoney(currentMarketDicePrice, false);
            }
        }
    }

    public void OnRerollMarketClicked()
    {
        if (diceManager != null && diceManager.TrySpendMoney(randomDiceRerollCost))
            GenerateNewMarketDice(true);
    }

    private void StartSlide(bool opening) { if (slideRoutine != null) StopCoroutine(slideRoutine); slideRoutine = StartCoroutine(SlideRoutine(opening)); }

    private IEnumerator SlideRoutine(bool opening)
    {
        isOpen = opening;
        float t = 0f;
        Vector2 start = opening ? closedPos : openPos;
        Vector2 end = opening ? openPos : closedPos;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / slideDuration);
            float ease = 1f - Mathf.Pow(1f - p, 4);
            if (shopPanel != null) shopPanel.anchoredPosition = Vector2.Lerp(start, end, ease);
            yield return null;
        }
        if (shopPanel != null) shopPanel.anchoredPosition = end;
        if (!opening && shopPanel != null) shopPanel.gameObject.SetActive(false);
    }

    public void OnNextButtonPressed()
    {
        if (backgroundManager != null) backgroundManager.SwitchToGame();
        if (diceManager != null) diceManager.SetShopOpen(false);
        StartSlide(false);
        if (forgeSlot != null && forgeSlot.currentDice != null)
        {
            if (diceManager != null) diceManager.ReturnSingleDiceToTray(forgeSlot.currentDice);
            forgeSlot.ClearSlot();
        }
        if (diceManager != null) diceManager.StartNextSectionFromShop();
    }
}