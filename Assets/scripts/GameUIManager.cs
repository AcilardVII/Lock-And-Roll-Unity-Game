using UnityEngine;
using TMPro;
using System.Collections;

public class GameUIManager : MonoBehaviour
{
    [Header("Oyun Sonu Paneli")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverTitleText;
    public TextMeshProUGUI gameOverReasonText;

    [Header("Ayarlar Paneli")]
    public GameObject settingsPanel;
    public GameObject shopPanel;
    public GameObject diceContainer;

    [Header("Global Ticket Göstergesi")]
    public TextMeshProUGUI globalTicketText;

    [Header("Animasyon Ayarları")]
    public float ticketPunchScale = 1.5f;
    public float ticketAnimDuration = 0.3f;
    public float popupDuration = 0.4f;
    public float popupPunchScale = 1.2f;

    private Coroutine ticketAnimRoutine;
    private Coroutine activeGameOverRoutine;
    private Coroutine activeSettingsRoutine;

    
    private Vector3 ticketOriginalScale;

    void Start()
    {
        
        if (globalTicketText != null)
            ticketOriginalScale = globalTicketText.transform.localScale;
        else
            ticketOriginalScale = Vector3.one;

        ResetPanels();
        UpdateTicketDisplay(false);

        if (ProgressionManager.Instance != null)
            ProgressionManager.Instance.OnTicketsChanged += OnTicketCountChanged;
    }

    void OnDestroy()
    {
        if (ProgressionManager.Instance != null)
            ProgressionManager.Instance.OnTicketsChanged -= OnTicketCountChanged;
    }

    void OnTicketCountChanged()
    {
        UpdateTicketDisplay(true);
    }

    void UpdateTicketDisplay(bool animate)
    {
        if (globalTicketText != null && ProgressionManager.Instance != null)
        {
            globalTicketText.text = ProgressionManager.Instance.currentTickets.ToString();

            if (animate)
            {
                if (ticketAnimRoutine != null) StopCoroutine(ticketAnimRoutine);
                ticketAnimRoutine = StartCoroutine(PunchTicketAnimation());
            }
        }
    }

    
    IEnumerator PunchTicketAnimation()
    {
        if (globalTicketText == null) yield break;

        Transform targetT = globalTicketText.transform;
        float timer = 0f;

        while (timer < ticketAnimDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / ticketAnimDuration);

           
            float punch = Mathf.Sin(t * Mathf.PI) * (ticketPunchScale - 1f);

            
            targetT.localScale = ticketOriginalScale * (1f + punch);

            yield return null;
        }

        
        targetT.localScale = ticketOriginalScale;
        ticketAnimRoutine = null;
    }

    public void ResetPanels()
    {
        if (activeGameOverRoutine != null) { StopCoroutine(activeGameOverRoutine); activeGameOverRoutine = null; }
        if (activeSettingsRoutine != null) { StopCoroutine(activeSettingsRoutine); activeSettingsRoutine = null; }

        if (gameOverPanel != null) { gameOverPanel.SetActive(false); gameOverPanel.transform.localScale = Vector3.one; }
        if (settingsPanel != null) { settingsPanel.SetActive(false); settingsPanel.transform.localScale = Vector3.one; }
    }

    public void ShowGameOver(bool isVictory, string reason = "")
    {
        shopPanel.SetActive(false);
        diceContainer.SetActive(false);
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (activeGameOverRoutine != null) StopCoroutine(activeGameOverRoutine);
            activeGameOverRoutine = StartCoroutine(AnimatePopupBackOut(gameOverPanel.transform));
        }

        if (isVictory)
        {
            if (gameOverTitleText != null) gameOverTitleText.text = "<color=yellow>VICTORY!</color>";
            if (gameOverReasonText != null) gameOverReasonText.text = "You conquered all sections!";
        }
        else
        {
            if (gameOverTitleText != null) gameOverTitleText.text = "<color=red>DEFEAT</color>";
            if (gameOverReasonText != null) gameOverReasonText.text = reason;
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null && settingsPanel.activeSelf) return;

        if (settingsPanel != null)
        {
            shopPanel.SetActive(false);
            diceContainer.SetActive(false);
            settingsPanel.SetActive(true);
            Time.timeScale = 0f;

            if (activeSettingsRoutine != null) StopCoroutine(activeSettingsRoutine);
            activeSettingsRoutine = StartCoroutine(AnimatePopupBackOut(settingsPanel.transform));
        }
    }

    public void CloseSettings()
    {
        Time.timeScale = 1f;

        if (settingsPanel != null)
        {
            StartCoroutine(AnimateCloseBackIn(settingsPanel));
            shopPanel.SetActive(true);
            diceContainer.SetActive(true);
        }
    }

    public void OnMenuButtonPressed()
    {
        Time.timeScale = 1f;
        shopPanel.SetActive(true);
        diceContainer.SetActive(true);
        MainMenuController menu = FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (menu != null)
        {
            menu.OnReturnToMenuPressed();
        }
    }

    public void OnRestartButtonPressed()
    {
        Time.timeScale = 1f;
        shopPanel.SetActive(true);
        diceContainer.SetActive(true);
        ResetPanels();

        DiceManager dm = FindFirstObjectByType<DiceManager>();
        if (dm != null)
        {
            dm.FullReset();
            dm.StartGameLoop();
        }
    }

    private IEnumerator AnimatePopupBackOut(Transform targetPanel)
    {
        targetPanel.localScale = Vector3.zero;
        float timer = 0f;
        float s = (popupPunchScale - 1f) * 2f;

        while (timer < popupDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / popupDuration);
            float p = t - 1f;
            float scaleValue = (p * p * ((s + 1f) * p + s) + 1f);

            if (targetPanel != null)
                targetPanel.localScale = Vector3.one * scaleValue;
            yield return null;
        }
        if (targetPanel != null) targetPanel.localScale = Vector3.one;
    }

    private IEnumerator AnimateCloseBackIn(GameObject targetPanelObj)
    {
        Transform targetInfo = targetPanelObj.transform;
        float timer = 0f;
        float s = (popupPunchScale - 1f) * 2f;

        while (timer < popupDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / popupDuration);
            float backInVal = t * t * ((s + 1f) * t - s);
            float scaleValue = 1f - backInVal;

            if (targetInfo != null)
                targetInfo.localScale = Vector3.one * scaleValue;
            yield return null;
        }

        if (targetPanelObj != null)
        {
            targetInfo.localScale = Vector3.zero;
            targetPanelObj.SetActive(false);
        }
    }
}