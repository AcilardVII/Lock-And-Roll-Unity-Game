using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 

public class DiceManager : MonoBehaviour
{
    // --- VERİ YAPILARI ---
    [System.Serializable]
    public class SectionProfile
    {
        [HideInInspector] public string name;
        public bool isBossSection = false;
        [Header("Can Ayarı")] public int sectionHealth = 10;
    }

    [System.Serializable]
    public class BossProfile
    {
        [HideInInspector] public string name;
        public string bossName = "Boss Name";

        [Header("Bu Boss'a Özel Kurallar")]
        public int maxRounds = 5;
        public int maxRerolls = 3;

        [Header("Yasaklı Sayı (Debuff)")]
        
        public int debuffedNumber = 0;

        [Header("Boss Açıklaması (Tooltip)")]
        [TextArea] public string bossDescription = "A powerful foe awaits...";
    }

    [Header("ÖNEMLİ AYARLAR")]
    public Transform diceParent;
    public UIHealthManager healthManager;

    [Header("BAŞLANGIÇ AYARLARI")]
    public GameObject defaultDicePrefab;
    public int startingDiceCount = 5;

    [Header("Zar Ayarları")]
    public float dicePopDuration = 0.5f;
    public float diceStaggerDelay = 0.1f;
    public float stopThreshold = 0.1f;

    [Header("UI - Temel")]
    public TextMeshProUGUI blueRerollText;
    public TextMeshProUGUI purpleRoundText;
    public TextMeshProUGUI mainInfoText;
    public TextMeshProUGUI sectionNameText;
    public TextMeshProUGUI bossNameText; 
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI bossDescriptionText;

    
    [Header("Bölüm İsimleri (Döngü 1-2-Boss)")]
    public string cycleTextPhase1 = "lowbet"; 
    public string cycleTextPhase2 = "highbet"; 

    [Header("UI - Skor Tablosu")]
    public TextMeshProUGUI roundScoreText;
    public TextMeshProUGUI multText;

    [Header("Animasyon Ayarları")]
    public Transform uiParent;
    public GameObject popupTextPrefab;
    public float countUpDuration = 0.4f;
    public float delayBetweenJokerAndScore = 0.2f;
    public float jokerSequenceDelay = 0.6f;
    public float failureSequenceDelay = 0.5f;
    public float popupDuration = 0.7f;
    public float popupPunchScale = 1.5f;
    public float multShakeMagnitude = 5f;
    public float chipsPunchScale = 1.3f;
    public float popupVerticalOffsetPixels = 100f;

    [Header("Renkler")]
    public Color chipColor = new Color(0f, 0.8f, 1f);
    public Color multAddColor = new Color(1f, 0.2f, 0.2f);
    public Color multTimesColor = new Color(1f, 0.8f, 0f);
    public Color moneyColor = Color.green;
    public Color rerollColor = new Color(0.3f, 0.5f, 1f);
    public Color roundColor = new Color(0.7f, 0.3f, 1f);

    [Header("Uyarı Sistemi")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningText;
    private Coroutine warningRoutine;
    public float warningPopDuration = 0.4f;
    public float warningStayDuration = 0.8f;
    public float warningFadeDuration = 0.3f;
    public string warningMsgNoMoney = "NO MONEY!";
    public string warningMsgLastDice = "LAST DICE!";

    public string[] sectionClearMessages;
    public string[] defeatMessages;

    [Header("Buton Renkleri")]
    public Color lockButtonColor_Lock;
    public Color lockButtonColor_Locked;
    public Color lockButtonColor_Reroll;
    public Color lockButtonColor_Disabled;
    public Color actionButtonColor_Roll;
    public Color actionButtonColor_Play;
    public Color actionButtonColor_Next;
    public Color actionButtonColor_Disabled;

    [Header("Butonlar")]
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;
    public Button lockButton;
    public TextMeshProUGUI lockButtonText;

    [SerializeField] private List<DiceThrow> allDice = new List<DiceThrow>();
    private List<DiceThrow> temporaryDiceList = new List<DiceThrow>();

    
    private List<GameObject> activePopups = new List<GameObject>();

    public bool autoFindSnapSlots = true;
    public Transform snapSlotParent;
    public Transform[] snapPoints;
    public bool autoPlaceFreeDiceOnStart = true;

    public int globalMaxRounds = 5;
    public int globalMaxRerolls = 3;
    public List<SectionProfile> gameSections = new List<SectionProfile>();
    public List<BossProfile> bossPresetList = new List<BossProfile>();
    private List<BossProfile> shuffledBossPool = new List<BossProfile>();
    private int currentBossPoolIndex = 0;

    
    private BossProfile activeBossProfile;

    public JokerSlot[] jokerSlots;
    private bool jokersLocked = false;

    public int money = 0;
    public ShopManager shopManager;
    public bool IsShopOpen { get; private set; } = false;
    public void SetShopOpen(bool open) => IsShopOpen = open;
    public bool enableShopBetweenSections = true;

    public int currentSectionIndex = 0;
    private bool isBossSectionActive = false;
    private int currentRerollCount;
    private int currentRound;
    private bool sectionFinished = false;
    private bool isGameLocked = false;
    private bool isRolling = false;
    private bool hasRolled = false;
    private bool hasAttacked = false;
    private bool waitingForNextSection = false;

    private List<DiceThrow> activeBattleDice = new List<DiceThrow>();
    private Coroutine attackSequenceRoutine;
    private Coroutine rollSequenceRoutine;
    private int currentRunningDiceScore = 0;
    private int lastOverkillDamage = 0;

    public int moneyPerEmptyJoker = 1;
    public int moneyPerRemainingRound = 1;
    public int moneyPerRemainingReroll = 1;
    public float moneyAnimDuration = 0.35f;
    public float moneyPunchScale = 1.2f;
    public Color moneyDefaultColor = Color.white;
    public Color moneyGainColor = Color.yellow;
    private Coroutine moneyAnimRoutine;

    private Coroutine actionBtnColorRoutine;
    private Coroutine lockBtnColorRoutine;
    private float colorFadeDuration = 0.25f;

    private Canvas rootCanvas;
    private int logicChips = 0;
    private float logicMult = 1f;
    private Vector2 warningOriginalPos;

    [HideInInspector] public TextMeshProUGUI jokerDeltaText;

    [Header("Joker Global Popup Ayarları (Yedek)")]
    public float globalJokerFontSize = 48f;
    public TMP_FontAsset globalJokerFont;
    public float globalJokerPopupDuration = 0.8f;
    public Vector3 globalJokerPopupOffset = new Vector3(0, 0.8f, 0);

    [Header("VOID GÖRSEL AYARLARI")]
    public GameObject blackHolePrefab;
    public Material blackHoleMaterial;
    public float voidSuckDuration = 0.8f;
    public float voidSpitDuration = 0.6f;
    public float blackHoleAppearDuration = 0.6f;
    public float voidRotationSpeed = 10f;

    [Header("HOLOGRAM GÖRSEL AYARLARI")]
    public Material hologramMaterial;
    [Range(0f, 1f)] public float hologramAlpha = 0.6f;
    public Vector3 hologramSpawnOffset = new Vector3(0.8f, 0f, 0f);
    public float hologramAppearDuration = 0.3f;

    public float voidHoldDuration = 0.5f;

    void OnValidate() { if (gameSections != null) { for (int i = 0; i < gameSections.Count; i++) { string title = $"Section {i + 1}"; if (gameSections[i].isBossSection) title += $" [BOSS SLOT] (HP: {gameSections[i].sectionHealth})"; else title += $" (Target: {gameSections[i].sectionHealth})"; gameSections[i].name = title; } } if (bossPresetList != null) { for (int i = 0; i < bossPresetList.Count; i++) bossPresetList[i].name = bossPresetList[i].bossName; } }

    void Start()
    {
        if (GameSeedManager.Instance == null) { GameObject go = new GameObject("GameSeedManager"); go.AddComponent<GameSeedManager>(); }
        if (uiParent == null) { GameObject uiObj = GameObject.Find("UI"); if (uiObj != null) uiParent = uiObj.transform; else { Canvas c = FindFirstObjectByType<Canvas>(); if (c != null) uiParent = c.transform; } }
        if (rootCanvas == null) rootCanvas = FindFirstObjectByType<Canvas>();
        if (diceParent != null) { foreach (Transform child in diceParent) if (child != null) child.gameObject.SetActive(false); } else { var existingDice = FindObjectsByType<DiceThrow>(FindObjectsSortMode.None); foreach (var d in existingDice) if (d != null) d.gameObject.SetActive(false); }

        if (warningPanel != null) warningPanel.SetActive(false);
        else if (warningText != null) warningText.gameObject.SetActive(false);
        if (warningPanel != null)
            warningOriginalPos = warningPanel.GetComponent<RectTransform>().anchoredPosition;
    }

    public void StartGameLoop()
    {
        Debug.Log("[DEBUG] StartGameLoop Başladı...");
        SetWarningPanelPosition(false);

        PrepareBossPool();
        currentSectionIndex = 0;
        LoadSectionData(currentSectionIndex);
        ResetScoreUI();

        if (allDice.Count == 0)
        {
            if (diceParent == null)
            {
                Debug.LogError("[HATA] Dice Parent (Dice Container) BOŞ! Inspector'dan ata.");
            }
            else
            {
                List<GameObject> diceToSpawnPool = new List<GameObject>();

                if (defaultDicePrefab != null)
                {
                    for (int i = 0; i < startingDiceCount; i++) diceToSpawnPool.Add(defaultDicePrefab);
                }

                if (ProgressionManager.Instance != null)
                {
                    List<GameObject> extraDice = ProgressionManager.Instance.GetStartingDicePrefabs();
                    if (extraDice != null && extraDice.Count > 0) diceToSpawnPool.AddRange(extraDice);
                }

                foreach (GameObject prefab in diceToSpawnPool)
                {
                    if (prefab == null) continue;
                    GameObject go = Instantiate(prefab, diceParent);
                    DiceThrow newDice = go.GetComponent<DiceThrow>();
                    newDice.ResetToDefaultFace();
                    RegisterNewDice(newDice);
                    newDice.gameObject.SetActive(true);
                    newDice.transform.localScale = Vector3.zero;
                }
            }
        }

        List<Transform> snaps = new List<Transform>();
        if (autoFindSnapSlots)
        {
            if (snapSlotParent != null) { for (int i = 0; i < snapSlotParent.childCount; i++) { Transform ch = snapSlotParent.GetChild(i); if (ch != null) snaps.Add(ch); } }
            else { var slots = FindObjectsByType<SnapSlots>(FindObjectsSortMode.None); foreach (var s in slots) if (s != null) snaps.Add(s.transform); }
            snapPoints = snaps.ToArray();
            if (snapPoints != null && snapPoints.Length > 0) System.Array.Sort(snapPoints, (a, b) => a.position.x.CompareTo(b.position.x));
        }

        if (ProgressionManager.Instance != null)
        {
            money += ProgressionManager.Instance.GetTotalStartingMoney();
        }

        UpdateMoneyUI();
        StartNewRoundState();
        if (autoPlaceFreeDiceOnStart) AutoPlaceAllFreeDiceIntoRollSlots();
        StartCoroutine(SpawnDiceWithPopAnimation());
        
        CoinDragSnap.JokerRarity rarity;
        int count;

        

        //  BAŞLANGIÇ JOKERİ SİSTEMİ
        if (ProgressionManager.Instance != null)
        {


            
            if (ProgressionManager.Instance.GetUnlockedStartingJokerProfile(out rarity, out count))
            {
                Debug.Log($"Başlangıç Hediyesi: {count} adet {rarity} Joker veriliyor.");

                for (int i = 0; i < count; i++)
                {
                    GameObject jokerPrefab = ProgressionManager.Instance.GetRandomJokerByRarity(rarity);

                    if (jokerPrefab != null)
                    {
                        
                        var allCoinSlots = FindObjectsByType<CoinSlot>(FindObjectsSortMode.None)
                                            .OrderBy(s => s.slotID).ToList();

                        CoinSlot freeSocket = allCoinSlots.FirstOrDefault(s => !s.IsOccupied);

                        if (freeSocket != null)
                        {
                            
                            Transform p = ProgressionManager.Instance.inventorySocketParent;
                            GameObject newJoker = Instantiate(jokerPrefab, p);

                            var coinScript = newJoker.GetComponent<CoinDragSnap>();
                            if (coinScript != null)
                            {
                                
                                newJoker.transform.position = freeSocket.transform.position;
                                coinScript.SnapToFirstFreeCoinSlotAnimated(); 

                                SpawnStationaryPopup(freeSocket.transform.position, "BONUS!", Color.blue);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Envanterde (Sockets) boş yer kalmadı!");
                            break;
                        }
                    }
                }
            }
        }
    }

    public JokerSlot GetFirstFreeJokerSlot()
    {
        if (jokerSlots == null) return null;
        foreach (var slot in jokerSlots)
        {
            if (slot != null && !slot.IsOccupied) return slot;
        }
        return null;
    }

    IEnumerator SpawnDiceWithPopAnimation()
    {
        foreach (var dice in allDice) { if (dice != null) { dice.AppearOnScene(dicePopDuration); yield return new WaitForSeconds(diceStaggerDelay); } }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) { if (!sectionFinished && !waitingForNextSection) { if (healthManager != null) healthManager.SetHealth(0); OnEnemyDied(); } }
    }

    void ResetScoreUI()
    {
        if (roundScoreText != null) roundScoreText.text = "0";
        if (multText != null) multText.text = "x1.0";
        if (jokerDeltaText != null) jokerDeltaText.gameObject.SetActive(false);
        currentRunningDiceScore = 0;
    }

    public void FullReset()
    {
        StopAllCoroutines();

        SetWarningPanelPosition(false);

        CleanUpTemporaryDice();
        for (int i = allDice.Count - 1; i >= 0; i--) { var dice = allDice[i]; if (dice != null) { if (dice.gameObject.scene.IsValid()) { Destroy(dice.gameObject); } } }
        allDice.Clear();
        var allJokers = FindObjectsByType<CoinDragSnap>(FindObjectsSortMode.None);
        foreach (var joker in allJokers) { if (joker != null && joker.gameObject.scene.IsValid()) Destroy(joker.gameObject); }
        var allCoinSlots = FindObjectsByType<CoinSlot>(FindObjectsSortMode.None);
        foreach (var slot in allCoinSlots) { if (slot != null) slot.Clear(); }
        if (jokerSlots != null) { foreach (var slot in jokerSlots) { if (slot != null) slot.Clear(); } }

        if (warningPanel != null) warningPanel.SetActive(false);
        if (warningText != null) warningText.gameObject.SetActive(false);

        if (activePopups != null)
        {
            for (int i = activePopups.Count - 1; i >= 0; i--)
            {
                if (activePopups[i] != null) Destroy(activePopups[i]);
            }
            activePopups.Clear();
        }

        money = 0; currentSectionIndex = 0; sectionFinished = false; waitingForNextSection = false; isGameLocked = false; isRolling = false; hasRolled = false; hasAttacked = false;
        UpdateMoneyUI(); UpdateUI();
    }

    void PrepareBossPool() { shuffledBossPool.Clear(); shuffledBossPool.AddRange(bossPresetList); GameSeedManager.Instance.ShuffleList(shuffledBossPool); currentBossPoolIndex = 0; }

    
    void LoadSectionData(int index)
    {
        if (sectionNameText != null) sectionNameText.text = "Section " + (index + 1);

        
        if (bossNameText != null)
        {
            var existingTrigger = bossNameText.GetComponent<TooltipTrigger>();
            if (existingTrigger != null) Destroy(existingTrigger);
        }

        activeBossProfile = null; 

        if (gameSections == null || index < 0 || index >= gameSections.Count)
        {
            currentRound = globalMaxRounds;
            currentRerollCount = globalMaxRerolls;
            isBossSectionActive = false;
            if (healthManager != null) healthManager.SetHealth(50 + (index * 10));

            SetCycleName(index);
            return;
        }

        SectionProfile currentLevel = gameSections[index];
        isBossSectionActive = currentLevel.isBossSection;
        if (healthManager != null) healthManager.SetHealth(currentLevel.sectionHealth);
        if (isBossSectionActive)
        {
            if (shuffledBossPool.Count > 0)
            {
                BossProfile activeBoss = shuffledBossPool[currentBossPoolIndex];
                activeBossProfile = activeBoss;

                currentBossPoolIndex = (currentBossPoolIndex + 1) % shuffledBossPool.Count;
                currentRound = activeBoss.maxRounds;
                currentRerollCount = activeBoss.maxRerolls;

                if (bossNameText != null)
                {
                    bossNameText.text = activeBoss.bossName;
                }

                
                if (bossDescriptionText != null)
                {
                    bossDescriptionText.gameObject.SetActive(true);

                    
                    string fullDescription = activeBoss.bossDescription;

                    

                    

                    bossDescriptionText.text = fullDescription;
                }

                if (mainInfoText != null) mainInfoText.text = "BOSS FIGHT!";
            }
            else
            {
                currentRound = globalMaxRounds;
                currentRerollCount = globalMaxRerolls;
                if (bossNameText != null) bossNameText.text = "Unknown Boss";

                if (bossDescriptionText != null)
                {
                    bossDescriptionText.gameObject.SetActive(false);
                    bossDescriptionText.text = "";
                }
            }
        }
        else
        {
            currentRound = globalMaxRounds;
            currentRerollCount = globalMaxRerolls;
            if (mainInfoText != null) mainInfoText.text = "NEXT ROUND";
            bossDescriptionText.gameObject.SetActive(false);

            SetCycleName(index);
        }
        UpdateUI();
    }

    void SetCycleName(int index)
    {
        if (bossNameText == null) return;

        int cyclePos = index % 3;

        if (cyclePos == 0) // 1, 4, 7...
        {
            bossNameText.text = cycleTextPhase1;
        }
        else if (cyclePos == 1) // 2, 5, 8...
        {
            bossNameText.text = cycleTextPhase2;
        }
        else
        {
            bossNameText.text = "";
        }
    }

    void StartNewRoundState()
    {
        isGameLocked = false; isRolling = false; hasRolled = false; hasAttacked = false;
        CleanUpTemporaryDice();
        ReturnAllDiceToTray(); SetJokersLocked(false); ResetScoreUI(); ApplyPassiveJokers(); UpdateUI(); if (mainInfoText != null && !isBossSectionActive) mainInfoText.text = "LOCK YOUR JOKERS!"; SetButtonState(lockButton, lockButtonText, "LOCK", lockButtonColor_Lock, true); SetButtonState(actionButton, actionButtonText, "ROLL", actionButtonColor_Disabled, false);
    }
    void ApplyPassiveJokers() { if (jokerSlots == null) return; foreach (var js in jokerSlots) { if (js != null && js.IsOccupied && js.occupant != null) { var j = js.occupant; if (j.passiveRounds > 0) currentRound += j.passiveRounds; if (j.passiveRerolls > 0) currentRerollCount += j.passiveRerolls; } } }

    public void OnLeftButtonPressed() { if (waitingForNextSection || sectionFinished || hasAttacked) return; if (!isGameLocked) { isGameLocked = true; SetJokersLocked(true); bool disableReroll = false; foreach (var js in jokerSlots) if (js != null && js.IsOccupied && js.occupant.lockRerollAbility) disableReroll = true; if (disableReroll) SetButtonState(lockButton, lockButtonText, "NO REROLL", lockButtonColor_Disabled, false); else SetButtonState(lockButton, lockButtonText, "LOCKED", lockButtonColor_Locked, false); SetButtonState(actionButton, actionButtonText, "ROLL", actionButtonColor_Roll, true); if (mainInfoText != null && !isBossSectionActive) mainInfoText.text = "Ready"; } else if (!isRolling && !hasAttacked && hasRolled) { if (CheckIfRerollBlocked()) return; TryReroll(); } }
    public void OnRightButtonPressed() { if (waitingForNextSection) { waitingForNextSection = false; if (enableShopBetweenSections && shopManager != null) shopManager.OpenShop(currentSectionIndex + 1, isBossSectionActive); else StartNextSection(); return; } if (hasAttacked) { StartCoroutine(StartNewRoundAnimated()); return; } if (hasRolled) { ConfirmAttack(); return; } if (isGameLocked && !isRolling) { StartRollSequence(true); return; } }

    IEnumerator StartNewRoundAnimated()
    {
        StartNewRoundState();
        foreach (var dice in allDice) { if (dice != null) { dice.gameObject.SetActive(true); dice.transform.localScale = Vector3.zero; } }
        if (autoPlaceFreeDiceOnStart) AutoPlaceAllFreeDiceIntoRollSlots();
        yield return StartCoroutine(SpawnDiceWithPopAnimation());
    }

    void SetButtonState(Button btn, TextMeshProUGUI txt, string label, Color targetColor, bool interactable) { if (btn == null) return; btn.interactable = interactable; if (txt != null) txt.text = label; Image img = btn.GetComponent<Image>(); if (img != null) { if (btn == actionButton) { if (actionBtnColorRoutine != null) StopCoroutine(actionBtnColorRoutine); actionBtnColorRoutine = StartCoroutine(SmoothColorChange(img, targetColor)); } else if (btn == lockButton) { if (lockBtnColorRoutine != null) StopCoroutine(lockBtnColorRoutine); lockBtnColorRoutine = StartCoroutine(SmoothColorChange(img, targetColor)); } else img.color = targetColor; } }
    IEnumerator SmoothColorChange(Image targetImg, Color targetColor) { float t = 0f; Color startColor = targetImg.color; while (t < colorFadeDuration) { t += Time.deltaTime; targetImg.color = Color.Lerp(startColor, targetColor, t / colorFadeDuration); yield return null; } targetImg.color = targetColor; }
    void SetJokersLocked(bool locked) { if (jokerSlots == null) return; foreach (var slot in jokerSlots) { if (slot == null) continue; slot.isLocked = locked; if (slot.occupant != null && locked) slot.occupant.LockFromJoker(); } jokersLocked = locked; }

    void StartRollSequence(bool countRound = true)
    {
        if (sectionFinished || isRolling) return;
        activeBattleDice.Clear();
        foreach (var dice in allDice) { if (dice == null) continue; if (dice.IsLockedForRoll) activeBattleDice.Add(dice); }
        if (activeBattleDice.Count == 0) return;
        foreach (var dice in allDice) { if (dice != null && !activeBattleDice.Contains(dice)) dice.gameObject.SetActive(false); }
        if (countRound) currentRound--;
        currentRunningDiceScore = 0;
        if (roundScoreText != null) roundScoreText.text = "0";
        isRolling = true; hasRolled = false; hasAttacked = false;
        if (mainInfoText != null) mainInfoText.text = "ROLLING!";
        SetButtonState(actionButton, actionButtonText, "...", actionButtonColor_Disabled, false);
        SetButtonState(lockButton, lockButtonText, "...", lockButtonColor_Disabled, false);
        if (rollSequenceRoutine != null) StopCoroutine(rollSequenceRoutine);
        rollSequenceRoutine = StartCoroutine(RollRoutine());
        UpdateUI();
    }

    IEnumerator RollRoutine()
    {
        foreach (var d in activeBattleDice) { if (d != null) d.Roll(); }
        yield return new WaitForSeconds(0.6f);
        float maxWaitTime = 4.0f; float timer = 0f; bool allStopped = false;
        while (!allStopped && timer < maxWaitTime)
        {
            timer += Time.deltaTime; allStopped = true;
            foreach (var d in activeBattleDice) { if (d == null) continue; Rigidbody2D rb = d.GetComponent<Rigidbody2D>(); if (rb != null) { if (rb.linearVelocity.sqrMagnitude > (stopThreshold * stopThreshold) || Mathf.Abs(rb.angularVelocity) > stopThreshold * 10) { allStopped = false; break; } } }
            yield return null;
        }
        foreach (var d in activeBattleDice) { if (d == null) continue; Rigidbody2D rb = d.GetComponent<Rigidbody2D>(); if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; } }
        yield return new WaitForSeconds(0.1f);
        OnAllDiceFinished();
    }

    void HandleDieResult(int value)
    {
        if (!isGameLocked || hasAttacked) return;
        currentRunningDiceScore += value;
        if (roundScoreText != null)
        {
            roundScoreText.text = currentRunningDiceScore.ToString();
            StartCoroutine(PunchUI(roundScoreText.transform, 1.2f));
        }
    }

    bool CheckIfRerollBlocked()
    {
        if (jokerSlots == null) return false;
        foreach (var js in jokerSlots) { if (js != null && js.IsOccupied && js.occupant != null && js.occupant.lockRerollAbility) return true; }
        return false;
    }

    void OnAllDiceFinished()
    {
        isRolling = false; hasRolled = true;
        if (mainInfoText != null) mainInfoText.text = "PLAY or REROLL";
        if (CheckIfRerollBlocked()) SetButtonState(lockButton, lockButtonText, "NO REROLL", lockButtonColor_Disabled, false);
        else SetButtonState(lockButton, lockButtonText, "REROLL", lockButtonColor_Reroll, true);
        SetButtonState(actionButton, actionButtonText, "PLAY", actionButtonColor_Play, true);
    }

    void TryReroll() { if (CheckIfRerollBlocked()) return; if (currentRerollCount <= 0 || sectionFinished) return; currentRerollCount--; UpdateUI(); hasRolled = false; StartRollSequence(false); }
    void ConfirmAttack() { if (attackSequenceRoutine != null) return; attackSequenceRoutine = StartCoroutine(AttackSequence()); }

    IEnumerator AttackSequence()
    {
        hasAttacked = true;
        SetButtonState(actionButton, actionButtonText, "...", actionButtonColor_Disabled, false);
        SetButtonState(lockButton, lockButtonText, "...", lockButtonColor_Disabled, false);
        if (mainInfoText != null) mainInfoText.text = "CALCULATING...";

        currentRunningDiceScore = 0;
        foreach (var d in activeBattleDice)
        {
            if (d != null) currentRunningDiceScore += GetDiceValue(d);
        }
        if (roundScoreText != null) roundScoreText.text = currentRunningDiceScore.ToString();

        if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Separator);
        if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.System);

        logicChips = currentRunningDiceScore; logicMult = 1f; int totalMoneyGain = 0; yield return new WaitForSeconds(0.2f);

        var diceToProcess = new List<DiceThrow>(activeBattleDice);
        foreach (var dice in diceToProcess)
        {
            if (dice != null && dice.activeEnchantment != null)
            {
                var ench = dice.activeEnchantment;
                bool triggered = false;
                switch (ench.type)
                {
                    case DiceEnchantment.EnchantType.Void:
                        if (dice.currentRig != DiceThrow.RiggedType.None || dice.isTemporaryClone) break;
                        yield return StartCoroutine(ProcessVoidChain(dice));
                        triggered = true;
                        break;
                    case DiceEnchantment.EnchantType.Hologram:
                        if (dice.currentRig != DiceThrow.RiggedType.None || dice.isTemporaryClone) break;
                        yield return StartCoroutine(ProcessHologramEffect(dice));
                        triggered = true;
                        break;
                }
                if (triggered) yield return new WaitForSeconds(0.2f);
            }
        }

        if (roundScoreText != null) roundScoreText.text = logicChips.ToString();

        if (jokerSlots != null)
        {
            foreach (var js in jokerSlots)
            {
                if (js == null || !js.IsOccupied) continue;
                var coin = js.occupant;
                if (coin == null) continue;

                if (coin.CheckCloneCondition(activeBattleDice.Count))
                {
                    if (activeBattleDice.Count > 0)
                    {
                        var original = activeBattleDice[0];
                        GameObject copy = Instantiate(original.gameObject, original.transform.position, Quaternion.identity, null);
                        var newDice = copy.GetComponent<DiceThrow>();
                        newDice.purchasePrice = original.purchasePrice; newDice.sellValue = original.sellValue; newDice.defaultFaceValue = original.defaultFaceValue; newDice.activeEnchantment = original.activeEnchantment;
                        RegisterNewDice(newDice); copy.SetActive(false);
                        SpawnStationaryPopup(coin.transform.position, "CLONED!", Color.white);
                        if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Clone, coin.jokerTitle);
                        yield return StartCoroutine(coin.PlayJokerActivationAnim());
                    }
                }

                bool triggered; int prevChips = logicChips; float prevMult = logicMult;
                int rerollGain = 0; int roundGain = 0;

                coin.CalculateJokerEffect(activeBattleDice, currentRound, globalMaxRounds, currentRerollCount, ref logicChips, ref logicMult, ref totalMoneyGain, ref rerollGain, ref roundGain, out triggered);

                if (triggered)
                {
                    yield return StartCoroutine(coin.PlayJokerActivationAnim());
                    string jokerName = string.IsNullOrEmpty(coin.jokerTitle) ? "Joker" : coin.jokerTitle;

                    if (logicChips > prevChips)
                    {
                        int diff = logicChips - prevChips;
                        if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Chip, jokerName, diff);
                        StartCoroutine(AnimateNumber(roundScoreText, prevChips, logicChips, true));
                    }
                    if (!Mathf.Approximately(logicMult, prevMult))
                    {
                        bool isXMult = (coin.multiplier > 1.01f || (coin.useDiceValueAsXMult && coin.triggerType != CoinDragSnap.TriggerType.AlwaysActive));
                        float val = isXMult ? (logicMult / prevMult) : (logicMult - prevMult);
                        string valStr = isXMult ? $"x{val:F1}" : $"+{val:F1}";
                        if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Mult, $"{jokerName}: {valStr}");
                        StartCoroutine(AnimateNumberFloat(multText, prevMult, logicMult, true));
                    }
                    if (totalMoneyGain > 0 && coin.moneyReward > 0)
                    {
                        if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Money, jokerName, coin.moneyReward);
                        GainMoney(coin.moneyReward, true);
                    }
                    if (rerollGain > 0) { currentRerollCount += rerollGain; UpdateUI(); }
                    if (roundGain > 0) { currentRound += roundGain; UpdateUI(); }

                    yield return new WaitForSeconds(0.2f);
                    if (coin.consumeOnTrigger) { Destroy(coin.gameObject); js.Clear(); }
                }
                else { StartCoroutine(coin.PlayFailureAnim()); yield return new WaitForSeconds(failureSequenceDelay); }
            }
        }

        yield return new WaitForSeconds(0.3f); int finalScore = Mathf.RoundToInt(logicChips * logicMult);

        if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Damage, "", finalScore);

        if (multText != null) multText.text = "x1.0";
        if (roundScoreText != null) { yield return StartCoroutine(AnimateNumber(roundScoreText, logicChips, finalScore, false)); StartCoroutine(PunchUI(roundScoreText.transform, 1.5f)); }

        lastOverkillDamage = 0;
        if (healthManager != null)
        {
            int currentHP = healthManager.currentHealth;
            if (finalScore > currentHP) lastOverkillDamage = finalScore - currentHP;
            healthManager.TakeDamage((float)finalScore);
        }
        else Debug.LogError("HATA: DiceManager'da HealthManager bağlı değil!");

        if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Separator);

        yield return new WaitForSeconds(0.8f);

        CleanUpTemporaryDice();

        if (jokerSlots != null) foreach (var js in jokerSlots) { if (js != null && js.occupant != null && js.occupant.consumeOnTrigger) { Destroy(js.occupant.gameObject); js.Clear(); } }
        yield return new WaitForSeconds(0.1f); if (healthManager != null && healthManager.IsDead) { if (!sectionFinished) OnEnemyDied(); attackSequenceRoutine = null; yield break; }
        if (sectionFinished) { attackSequenceRoutine = null; yield break; }
        if (currentRound <= 0) { sectionFinished = true; if (mainInfoText != null && defeatMessages != null && defeatMessages.Length > 0) mainInfoText.text = defeatMessages[GameSeedManager.Instance.GetRandomInt(0, defeatMessages.Length)]; var uiManager = FindFirstObjectByType<GameUIManager>(); if (uiManager != null) { string reason = "Rounds Depleted"; if (defeatMessages != null && defeatMessages.Length > 0) reason = defeatMessages[GameSeedManager.Instance.GetRandomInt(0, defeatMessages.Length)]; uiManager.ShowGameOver(false, reason); } attackSequenceRoutine = null; yield break; }
        if (mainInfoText != null) mainInfoText.text = "HAND COMPLETE"; SetButtonState(actionButton, actionButtonText, "NEXT", actionButtonColor_Next, true); SetButtonState(lockButton, lockButtonText, "-", lockButtonColor_Disabled, false); attackSequenceRoutine = null;
    }

    IEnumerator AnimateNumber(TextMeshProUGUI txt, int start, int end, bool punch) { if (txt == null) yield break; if (punch) StartCoroutine(PunchUI(txt.transform, chipsPunchScale)); float t = 0f; while (t < countUpDuration) { t += Time.deltaTime; float p = t / countUpDuration; int val = Mathf.RoundToInt(Mathf.Lerp(start, end, p)); txt.text = val.ToString(); yield return null; } txt.text = end.ToString(); }
    IEnumerator AnimateNumberFloat(TextMeshProUGUI txt, float start, float end, bool shake) { if (txt == null) yield break; if (shake) StartCoroutine(ShakeUI(txt.transform)); float t = 0f; while (t < countUpDuration) { t += Time.deltaTime; float p = t / countUpDuration; float val = Mathf.Lerp(start, end, p); txt.text = "x" + val.ToString("F1"); yield return null; } txt.text = "x" + end.ToString("F1"); }

    public void SpawnStationaryPopup(Vector3 worldPos, string text, Color color, float durationOverride = -1f, float fontSize = -1f, TMP_FontAsset fontOverride = null)
    {
        if (popupTextPrefab == null) return;
        Transform parent = uiParent;
        if (parent == null) { if (rootCanvas == null) rootCanvas = FindFirstObjectByType<Canvas>(); if (rootCanvas != null) parent = rootCanvas.transform; }
        if (parent == null) return;
        GameObject popup = Instantiate(popupTextPrefab, parent); popup.SetActive(true);

        
        activePopups.Add(popup);

        TextMeshProUGUI tmp = popup.GetComponent<TextMeshProUGUI>();
        if (tmp != null) { tmp.text = text; if (fontOverride != null) tmp.font = fontOverride; if (fontSize > 0) tmp.fontSize = fontSize; tmp.color = color; tmp.alpha = 1f; }
        Vector3 startScale = popup.transform.localScale; Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPos);
        RectTransform parentRect = parent.GetComponent<RectTransform>(); Vector2 localPos; Camera uiCamera = (rootCanvas.worldCamera != null) ? rootCanvas.worldCamera : Camera.main; RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out localPos);
        RectTransform popupRect = popup.GetComponent<RectTransform>(); popupRect.anchoredPosition = localPos; popup.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        float finalDuration = (durationOverride > 0f) ? durationOverride : popupDuration;
        StartCoroutine(PopupRoutine(popup.transform, tmp, startScale, finalDuration));
    }

    IEnumerator PopupRoutine(Transform targetT, TextMeshProUGUI txt, Vector3 baseScale, float duration)
    {
        targetT.localScale = Vector3.zero; float popInTime = 0.2f; float popOutTime = 0.2f; float stayTime = duration - (popInTime + popOutTime); if (stayTime < 0) stayTime = 0f;
        float t = 0f; while (t < popInTime) { t += Time.deltaTime; float p = t / popInTime; targetT.localScale = Vector3.Lerp(Vector3.zero, baseScale * popupPunchScale, p); yield return null; }
        targetT.localScale = baseScale * popupPunchScale;
        yield return new WaitForSeconds(stayTime);
        t = 0f; while (t < popOutTime) { t += Time.deltaTime; float p = t / popOutTime; targetT.localScale = Vector3.Lerp(baseScale * popupPunchScale, Vector3.zero, Mathf.SmoothStep(0f, 1f, p)); if (txt != null) txt.alpha = 1f - p; yield return null; }

        
        if (targetT != null)
        {
            if (activePopups.Contains(targetT.gameObject)) activePopups.Remove(targetT.gameObject);
            Destroy(targetT.gameObject);
        }
    }

    IEnumerator ShakeUI(Transform target) { Vector3 orig = target.localPosition; float t = 0f; while (t < 0.2f) { t += Time.deltaTime; target.localPosition = orig + (Vector3)(UnityEngine.Random.insideUnitCircle * multShakeMagnitude); yield return null; } target.localPosition = orig; }
    IEnumerator PunchUI(Transform target, float scale) { Vector3 baseScale = Vector3.one; float t = 0f; while (t < 0.2f) { t += Time.unscaledDeltaTime; float s = Mathf.Sin((t / 0.2f) * Mathf.PI); target.localScale = baseScale * (1f + (scale - 1f) * s); yield return null; } target.localScale = baseScale; }
    public void RegisterNewDice(DiceThrow dice) { if (dice == null) return; dice.OnResult += HandleDieResult; var drag = dice.GetComponent<diceDragScript>(); if (drag != null) { drag.diceManager = this; if (snapPoints != null && snapPoints.Length > 0) drag.snapPoints = snapPoints; } if (!allDice.Contains(dice)) allDice.Add(dice); }
    public void UnregisterAndDestroyDice(DiceThrow dice) { if (dice == null) return; dice.OnResult -= HandleDieResult; allDice.Remove(dice); Destroy(dice.gameObject); }
    public void ReturnAllDiceToTray() { if (snapPoints != null) foreach (var sp in snapPoints) { var slot = sp.GetComponent<SnapSlots>(); if (slot != null) slot.occupant = null; } int slotIndex = 0; foreach (var dice in allDice) { if (dice == null) continue; dice.gameObject.SetActive(true); if (snapPoints != null && slotIndex < snapPoints.Length) { Transform targetSlot = snapPoints[slotIndex]; var drag = dice.GetComponent<diceDragScript>(); if (drag != null) { var rb = dice.GetComponent<Rigidbody2D>(); if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; rb.bodyType = RigidbodyType2D.Kinematic; } drag.ForceSnapToSlot(targetSlot); } else dice.transform.position = targetSlot.position; slotIndex++; } else dice.gameObject.SetActive(false); if (dice != null) dice.ResetToDefaultFace(); } }
    void UpdateUI() { if (blueRerollText != null) blueRerollText.text = currentRerollCount.ToString(); if (purpleRoundText != null) purpleRoundText.text = currentRound.ToString(); }
    void UpdateMoneyUI() { if (moneyText != null) { moneyText.text = "$" + money; moneyText.alpha = 1f; moneyText.gameObject.SetActive(true); } }
    public bool TrySpendMoney(int amount, bool showWarning = true) { if (amount <= 0) return true; if (money < amount) { if (showWarning) ShowVisualWarning(warningMsgNoMoney); return false; } money -= amount; UpdateMoneyUI(); return true; }
    public void GainMoney(int amount, bool animate = true) { if (amount <= 0) return; money += amount; UpdateMoneyUI(); if (animate) { if (moneyAnimRoutine != null) StopCoroutine(moneyAnimRoutine); moneyAnimRoutine = StartCoroutine(MoneyPulse()); } }
    IEnumerator MoneyPulse() { if (moneyText == null) yield break; Vector3 b = moneyText.transform.localScale; Color c = moneyDefaultColor; float t = 0f; while (t < moneyAnimDuration) { t += Time.deltaTime; float p = Mathf.Clamp01(t / moneyAnimDuration); float s = 1f + (moneyPunchScale - 1f) * Mathf.Sin(p * Mathf.PI); moneyText.transform.localScale = b * s; moneyText.color = Color.Lerp(c, moneyGainColor, Mathf.Sin(p * Mathf.PI)); yield return null; } moneyText.transform.localScale = b; moneyText.color = c; UpdateMoneyUI(); moneyAnimRoutine = null; }
    public void AutoPlaceAllFreeDiceIntoRollSlots() { if (snapPoints == null || snapPoints.Length == 0) return; foreach (var sp in snapPoints) { var slot = sp.GetComponent<SnapSlots>(); if (slot != null) slot.occupant = null; } int slotIndex = 0; for (int i = 0; i < allDice.Count; i++) { var dice = allDice[i]; if (dice == null) continue; if (!dice.gameObject.activeInHierarchy) continue; if (slotIndex >= snapPoints.Length) break; Transform targetSlot = snapPoints[slotIndex]; var drag = dice.GetComponent<diceDragScript>(); if (drag != null) drag.ForceSnapToSlot(targetSlot); else dice.SetLockedPosition(targetSlot.position, true); slotIndex++; } }
    public Transform GetFirstFreeRollSlotTransform() { if (snapPoints == null || snapPoints.Length == 0) return null; for (int i = 0; i < snapPoints.Length; i++) { Transform sp = snapPoints[i]; if (sp == null) continue; var slot = sp.GetComponent<SnapSlots>(); if (slot == null) continue; if (!slot.isRollSlot) continue; if (!slot.IsOccupied) return sp; } return null; }
    public bool HasFreeRollSlot() => GetFirstFreeRollSlotTransform() != null;

    public DiceThrow SpawnDiceDirectToFirstFreeRollSlot(GameObject prefab, int purchasePrice = -1, Transform parentOverride = null)
    {
        if (prefab == null) return null;
        Transform slotT = GetFirstFreeRollSlotTransform();
        if (slotT == null) { ShowVisualWarning("NO SLOT!"); return null; }
        Transform parent = parentOverride != null ? parentOverride : null;
        GameObject go = Instantiate(prefab, slotT.position, slotT.rotation, parent); go.transform.localScale = Vector3.one;
        var rb = go.GetComponent<Rigidbody2D>(); if (rb != null) rb.simulated = true;
        var dice = go.GetComponent<DiceThrow>();
        if (dice == null) { Destroy(go); return null; }
        dice.diceManager = this;
        if (purchasePrice != -1) dice.purchasePrice = purchasePrice;
        dice.sellValue = Mathf.Max(1, dice.purchasePrice / 2);
        var drag = go.GetComponent<diceDragScript>();
        if (drag != null) { drag.diceManager = this; drag.snapPoints = snapPoints; drag.isPreview = false; drag.ForceSnapToSlot(slotT); } else { dice.SetLockedPosition(slotT.position, true); }
        RegisterNewDice(dice);
        dice.AppearOnScene(dicePopDuration);
        return dice;
    }

    public void OnEnemyDied() { if (sectionFinished) return; sectionFinished = true; OnSectionFinished(); }

    void OnSectionFinished()
    {
        StartCoroutine(CheckoutRoutine());
    }

    IEnumerator CheckoutRoutine()
    {
        
        SetButtonState(actionButton, actionButtonText, "<size=32>CHECKOUT</size>", actionButtonColor_Disabled, false);
        SetButtonState(lockButton, lockButtonText, "-", lockButtonColor_Disabled, false);

       
        if (isBossSectionActive)
        {
            if (ProgressionManager.Instance != null)
            {
                if (!ProgressionManager.Instance.IsBossRewardClaimed(currentSectionIndex))
                {
                    ProgressionManager.Instance.AddTickets(1);
                    ProgressionManager.Instance.MarkBossRewardClaimed(currentSectionIndex);

                    if (GameLogManager.Instance != null)
                    {
                        GameLogManager.Instance.Log(GameLogManager.LogCategory.Separator);
                        GameLogManager.Instance.Log(GameLogManager.LogCategory.Checkout, "<color=yellow>BOSS DEFEATED!</color>");
                        GameLogManager.Instance.Log(GameLogManager.LogCategory.Checkout, "Ticket Gained: +1");
                    }
                }
                else
                {
                    if (GameLogManager.Instance != null)
                    {
                        GameLogManager.Instance.Log(GameLogManager.LogCategory.Separator);
                        GameLogManager.Instance.Log(GameLogManager.LogCategory.Checkout, "<color=grey>BOSS DEFEATED</color>");
                        GameLogManager.Instance.Log(GameLogManager.LogCategory.Checkout, "Ticket Already Claimed.");
                    }
                }
            }
        }

        int rewardTotal = 0;

        
        int emptyJokers = 0;
        if (jokerSlots != null) foreach (var js in jokerSlots) if (js != null && !js.IsOccupied) emptyJokers++;
        int jokerReward = emptyJokers * moneyPerEmptyJoker;

        
        int roundReward = Mathf.Max(0, currentRound) * moneyPerRemainingRound;

       
        int rerollReward = Mathf.Max(0, currentRerollCount) * moneyPerRemainingReroll;

        
        int overkillMoney = lastOverkillDamage;

        
        int interestEarned = 0;
        int interestCap = 0;

        if (ProgressionManager.Instance != null)
        {
           
            interestCap = ProgressionManager.Instance.GetTotalInterestCap();

           
            if (interestCap > 0)
            {
                int threshold, rewardPerChunk;
                ProgressionManager.Instance.GetInterestRates(out threshold, out rewardPerChunk);

                if (threshold > 0)
                {
                    int chunks = money / threshold;
                    interestEarned = chunks * rewardPerChunk;
                    if (interestEarned > interestCap) interestEarned = interestCap; 
                }
            }
        }
        

        
        if (GameLogManager.Instance != null)
        {
            GameLogManager.Instance.Log(GameLogManager.LogCategory.Separator);
            GameLogManager.Instance.Log(GameLogManager.LogCategory.Checkout, "--- CHECKOUT ---");
            yield return new WaitForSeconds(0.3f);

            if (jokerReward > 0) { GameLogManager.Instance.Log(GameLogManager.LogCategory.Checkout, $"Empty Jokers ({emptyJokers}): ${jokerReward}"); yield return new WaitForSeconds(0.3f); }
            if (roundReward > 0) { GameLogManager.Instance.Log(GameLogManager.LogCategory.Checkout, $"Hands Left ({currentRound}): ${roundReward}"); yield return new WaitForSeconds(0.3f); }
            if (rerollReward > 0) { GameLogManager.Instance.Log(GameLogManager.LogCategory.Checkout, $"Rerolls Left ({currentRerollCount}): ${rerollReward}"); yield return new WaitForSeconds(0.3f); }
            if (overkillMoney > 0) { GameLogManager.Instance.Log(GameLogManager.LogCategory.Checkout, $"OVERKILL (+{lastOverkillDamage}): ${overkillMoney}"); yield return new WaitForSeconds(0.3f); }

            
            if (interestEarned > 0)
            {
                GameLogManager.Instance.Log(GameLogManager.LogCategory.Checkout, $"Interest (Max ${interestCap}): ${interestEarned}");
                yield return new WaitForSeconds(0.3f);
            }

            
            rewardTotal = jokerReward + roundReward + rerollReward + overkillMoney + interestEarned;

            GameLogManager.Instance.Log(GameLogManager.LogCategory.Checkout, $"TOTAL PAYOUT: ${rewardTotal}");
        }
        else
        {
            rewardTotal = jokerReward + roundReward + rerollReward + overkillMoney + interestEarned;
        }

        GainMoney(rewardTotal, true);
        yield return new WaitForSeconds(0.5f);

        waitingForNextSection = true;
        if (enableShopBetweenSections && shopManager != null)
            SetButtonState(actionButton, actionButtonText, "SHOP", actionButtonColor_Next, true);
        else
            SetButtonState(actionButton, actionButtonText, "NEXT", actionButtonColor_Next, true);

        if (mainInfoText != null && sectionClearMessages != null && sectionClearMessages.Length > 0)
            mainInfoText.text = sectionClearMessages[GameSeedManager.Instance.GetRandomInt(0, sectionClearMessages.Length)];
    }



    public void StartNextSectionFromShop() => StartNextSection();
    void StartNextSection() { PrepareNextSection(); ReturnAllDiceToTray(); foreach (var dice in allDice) { if (dice != null) { dice.gameObject.SetActive(true); dice.transform.localScale = Vector3.zero; } } StartNewRoundState(); if (mainInfoText != null) mainInfoText.text = "Select Dice"; if (autoPlaceFreeDiceOnStart) AutoPlaceAllFreeDiceIntoRollSlots(); StartCoroutine(SpawnDiceWithPopAnimation()); }
    void PrepareNextSection() { currentSectionIndex++; sectionFinished = false; waitingForNextSection = false; LoadSectionData(currentSectionIndex); }
    void RestoreAllDiceVisibility() { foreach (var dice in allDice) if (dice != null) dice.gameObject.SetActive(true); }
    public void ReturnSingleDiceToTray(diceDragScript diceDrag) { if (snapPoints == null) return; Transform target = GetFirstFreeRollSlotTransform(); if (target != null) diceDrag.ForceSnapToSlot(target); else Debug.LogWarning("Zar için boş yer bulunamadı!"); }
    public int GetAllDiceCount() { return allDice != null ? allDice.Count : 0; }

    public void ShowVisualWarning(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
            warningText.alpha = 1f;
            
            warningText.gameObject.SetActive(true);
        }

        GameObject targetObj = (warningPanel != null) ? warningPanel : ((warningText != null) ? warningText.gameObject : null);
        if (targetObj == null) return;

        targetObj.SetActive(true);
        targetObj.transform.localScale = Vector3.zero;

        if (warningRoutine != null) StopCoroutine(warningRoutine);
        warningRoutine = StartCoroutine(WarningPopupAnimation(targetObj.transform));
    }

    IEnumerator WarningPopupAnimation(Transform targetTransform)
    {
        float timer = 0f;
        while (timer < warningPopDuration)
        {
            timer += Time.unscaledDeltaTime; float t = timer / warningPopDuration;
            float smooth = Mathf.SmoothStep(0f, 1f, t); float punch = Mathf.Sin(t * Mathf.PI) * 0.1f;
            targetTransform.localScale = Vector3.one * (smooth + punch); yield return null;
        }
        targetTransform.localScale = Vector3.one;
        yield return new WaitForSeconds(warningStayDuration);
        timer = 0f;
        while (timer < warningFadeDuration)
        {
            timer += Time.unscaledDeltaTime; float t = timer / warningFadeDuration;
            targetTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t); yield return null;
        }
        targetTransform.gameObject.SetActive(false);
    }

    
    int GetDiceValue(DiceThrow dice)
    {
        if (dice == null || dice.diceText == null) return 0;

        if (int.TryParse(dice.diceText.text, out int val))
        {
            
            if (isBossSectionActive && activeBossProfile != null && activeBossProfile.debuffedNumber > 0)
            {
                
                if (val == activeBossProfile.debuffedNumber)
                {
                    return 0;
                }
            }
            return val;
        }
        return 0;
    }

    public void LockAllInputs()
    {
        if (actionButton != null) actionButton.interactable = false;
        if (lockButton != null) lockButton.interactable = false;
        if (actionButtonText != null) actionButtonText.text = "WAIT...";
        if (lockButtonText != null) lockButtonText.text = "WAIT...";
    }

    IEnumerator ProcessVoidChain(DiceThrow sourceDice)
    {
        bool chainActive = true;
        List<DiceThrow> voidedHistory = new List<DiceThrow>();
        voidedHistory.Add(sourceDice);

        string actText = "VOID ACTIVATED!";
        Color actColor = Color.magenta;
        if (sourceDice.activeEnchantment != null)
        {
            if (!string.IsNullOrEmpty(sourceDice.activeEnchantment.activationText))
                actText = sourceDice.activeEnchantment.activationText;
            actColor = sourceDice.activeEnchantment.activationTextColor;
        }

        if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Enchantment, actText, 0, 0, actColor);

        yield return new WaitForSeconds(0.2f);

        while (chainActive)
        {
            DiceThrow targetDice = GetLowestDiceExcluding(voidedHistory);
            if (targetDice == null) { chainActive = false; break; }
            int currentVal = GetDiceValue(targetDice);
            if (currentVal > (targetDice.maxRange * 0.5f))
            {
                if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Enchantment, "Target > 50%. Void Stopped.", 0, 0, actColor);
                chainActive = false; break;
            }
            voidedHistory.Add(targetDice);
            int oldValue = GetDiceValue(targetDice);
            if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Enchantment, $"Void target: {oldValue}...", 0, 0, actColor);

            System.Action onSuck = () => { logicChips -= oldValue; if (roundScoreText != null) { roundScoreText.text = logicChips.ToString(); StartCoroutine(PunchUI(roundScoreText.transform, 0.9f)); } };
            System.Action onSpit = () => { int newVal = GetDiceValue(targetDice); logicChips += newVal; if (roundScoreText != null) { roundScoreText.text = logicChips.ToString(); StartCoroutine(PunchUI(roundScoreText.transform, 1.3f)); } };

            yield return StartCoroutine(AnimateVoidSuckAndSpit(targetDice, onSuck, onSpit));
            int newValue = GetDiceValue(targetDice);
            if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Enchantment, $"Result: {newValue}", 0, 0, actColor);

            yield return new WaitForSeconds(0.2f);
            if (newValue > oldValue) { chainActive = true; if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Enchantment, "CHAIN REACTION! (+)", 0, 0, actColor); }
            else { chainActive = false; if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Enchantment, "Void Chain Stopped.", 0, 0, actColor); }
        }
    }

    IEnumerator AnimateVoidSuckAndSpit(DiceThrow dice, System.Action onSuck = null, System.Action onSpit = null)
    {
        if (blackHolePrefab == null) { dice.Roll(); yield return new WaitForSeconds(0.5f); yield break; }
        Rigidbody2D rb = dice.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
        Vector3 originalScale = Vector3.one; Vector3 originalPos = dice.transform.position; Vector3 targetSmallScale = Vector3.zero;
        GameObject hole = Instantiate(blackHolePrefab, originalPos, Quaternion.identity);
        if (blackHoleMaterial != null) { SpriteRenderer sr = hole.GetComponentInChildren<SpriteRenderer>(); if (sr != null) sr.material = blackHoleMaterial; }
        hole.transform.localScale = Vector3.zero;

        float t = 0; while (t < blackHoleAppearDuration) { t += Time.deltaTime; float p = Mathf.SmoothStep(0f, 1f, t / blackHoleAppearDuration); hole.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.5f, p); yield return null; }
        hole.transform.localScale = Vector3.one * 1.5f;

        if (onSuck != null) onSuck.Invoke();

        t = 0; while (t < voidSuckDuration) { t += Time.deltaTime; float p = t / voidSuckDuration; float easeP = p * p; dice.transform.position = Vector3.Lerp(originalPos, hole.transform.position, easeP); dice.transform.localScale = Vector3.Lerp(originalScale, targetSmallScale, easeP); dice.transform.Rotate(0, 0, 360 * Time.deltaTime * voidRotationSpeed); yield return null; }
        dice.transform.position = hole.transform.position; dice.transform.localScale = targetSmallScale;

        dice.Roll();
        float waitTimer = 0f; while (!dice.HasResult && waitTimer < 4.0f) { waitTimer += Time.deltaTime; if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; } dice.transform.position = hole.transform.position; dice.transform.localScale = targetSmallScale; yield return null; }
        if (GetDiceValue(dice) == 0) { int fallback = UnityEngine.Random.Range(1, dice.maxRange + 1); if (dice.diceText != null) dice.diceText.text = fallback.ToString(); }

        float holdTimer = 0f; while (holdTimer < voidHoldDuration) { holdTimer += Time.deltaTime; dice.transform.position = hole.transform.position; dice.transform.localScale = targetSmallScale; yield return null; }

        if (onSpit != null) onSpit.Invoke();

        t = 0; dice.transform.position = originalPos; Vector3 startSpitPos = hole.transform.position;
        while (t < voidSpitDuration) { t += Time.deltaTime; float p = t / voidSpitDuration; float elastic = Mathf.Sin(-13f * (p + 1f) * Mathf.PI * 0.5f) * Mathf.Pow(2f, -10f * p) + 1f; dice.transform.localScale = Vector3.Lerp(targetSmallScale, originalScale, elastic); dice.transform.position = Vector3.Lerp(startSpitPos, originalPos, p); yield return null; }
        dice.transform.localScale = originalScale; dice.transform.position = originalPos;

        t = 0; while (t < blackHoleAppearDuration) { t += Time.deltaTime; float p = Mathf.SmoothStep(0f, 1f, t / blackHoleAppearDuration); hole.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.zero, p); yield return null; }
        Destroy(hole); if (rb != null) rb.simulated = true;
    }

    IEnumerator ProcessHologramEffect(DiceThrow sourceDice)
    {
        string actText = "HOLOGRAM!"; Color actColor = Color.cyan;
        if (sourceDice.activeEnchantment != null) { if (!string.IsNullOrEmpty(sourceDice.activeEnchantment.activationText)) actText = sourceDice.activeEnchantment.activationText; actColor = sourceDice.activeEnchantment.activationTextColor; }
        if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Enchantment, actText, 0, 0, actColor);

        Vector3 spawnPos = sourceDice.transform.position + hologramSpawnOffset;
        GameObject cloneObj = Instantiate(sourceDice.gameObject, spawnPos, Quaternion.identity, diceParent);
        DiceThrow cloneDice = cloneObj.GetComponent<DiceThrow>();

        if (cloneDice != null)
        {
            cloneDice.activeEnchantment = null; cloneDice.isTemporaryClone = true;
            SpriteRenderer[] renderers = cloneObj.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers) { if (hologramMaterial != null) r.material = hologramMaterial; Color c = r.color; c.a = hologramAlpha; r.color = c; }
            diceDragScript drag = cloneObj.GetComponent<diceDragScript>(); if (drag != null) Destroy(drag);
            Rigidbody2D cloneRb = cloneObj.GetComponent<Rigidbody2D>(); if (cloneRb != null) { cloneRb.bodyType = RigidbodyType2D.Kinematic; cloneRb.linearVelocity = Vector2.zero; }
            int val = GetDiceValue(sourceDice); if (cloneDice.diceText != null) cloneDice.diceText.text = val.ToString();
            cloneDice.UpdateVisuals(); cloneDice.AppearOnScene(hologramAppearDuration);
            logicChips += val; if (roundScoreText != null) { roundScoreText.text = logicChips.ToString(); StartCoroutine(PunchUI(roundScoreText.transform, 1.1f)); }
            if (GameLogManager.Instance != null) GameLogManager.Instance.Log(GameLogManager.LogCategory.Enchantment, $"Hologram Copy: +{val}", 0, 0, actColor);
            temporaryDiceList.Add(cloneDice);
        }
        yield return new WaitForSeconds(0.3f);
    }

    void CleanUpTemporaryDice() { if (temporaryDiceList.Count > 0) { foreach (var dice in temporaryDiceList) { if (dice != null) Destroy(dice.gameObject); } temporaryDiceList.Clear(); } }
    DiceThrow GetLowestDiceExcluding(List<DiceThrow> excludedDiceList)
    {
        DiceThrow lowest = null; int lowestVal = 999;
        foreach (var d in activeBattleDice)
        {
            if (d == null) continue; if (excludedDiceList.Contains(d)) continue;
            if (d.currentRig != DiceThrow.RiggedType.None) continue;
            int val = GetDiceValue(d); if (val >= d.maxRange) continue;
            if (val < lowestVal) { lowestVal = val; lowest = d; }
        }
        return lowest;
    }
    public void SetWarningPanelPosition(bool toSkillTree)
    {
        if (warningPanel == null) return;

        RectTransform rt = warningPanel.GetComponent<RectTransform>();

        if (toSkillTree)
        {
           
            rt.anchoredPosition = new Vector2(-2500f, warningOriginalPos.y);
        }
        else
        {
            
            rt.anchoredPosition = warningOriginalPos;
        }
    }
}