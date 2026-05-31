using UnityEngine;
using TMPro;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("UI & Seed")]
    public TMP_InputField seedInputField;
    public RectTransform menuPanel;
    public GameObject gameOverPanel;

    [Header("Menu Animasyon Ayarları")]
    public Vector2 onScreenPosition = Vector2.zero;
    public Vector2 offScreenPosition = new Vector2(-1500f, 0f); 
    public Vector2 skillTreeSlidePosition = new Vector2(1500f, 0f); 
    public float slideDuration = 0.5f;

    [Header("Diğer Yöneticiler")]
    public SingleSceneBackground backgroundManager;
    public SingleSceneGameUI gameUIAnimation;
    public SkillTreeController skillTreeController; 

    private bool isBooting = false;

    void Start()
    {
        Time.timeScale = 1f;
        if (menuPanel != null) onScreenPosition = menuPanel.anchoredPosition;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (gameUIAnimation == null) gameUIAnimation = FindFirstObjectByType<SingleSceneGameUI>();
        if (skillTreeController == null) skillTreeController = FindFirstObjectByType<SkillTreeController>();
    }

    
    public void OnSkillTreeButtonPressed()
    {
        if (isBooting) return;
        
        StartCoroutine(SlideMenu(onScreenPosition, skillTreeSlidePosition, false));

        
        if (skillTreeController != null) skillTreeController.OpenSkillTree();
    }

    public void OnReturnFromSkillTree()
    {
        
        StartCoroutine(SlideMenu(skillTreeSlidePosition, onScreenPosition, true));
    }
    

    public void OnPlayButtonPressed()
    {
        if (isBooting) return;
        StartCoroutine(BootSequenceRoutine());
    }

    IEnumerator BootSequenceRoutine()
    {
        isBooting = true;
        Debug.Log("🚀 Boot Sequence Başladı...");

        GameUIManager uiManager = FindFirstObjectByType<GameUIManager>();
        if (uiManager != null) uiManager.ResetPanels();

        DiceManager dm = FindFirstObjectByType<DiceManager>();
        if (dm != null)
        {
            dm.FullReset();
            dm.LockAllInputs();
        }

        if (GameSeedManager.Instance != null && seedInputField != null)
        {
            if (!string.IsNullOrEmpty(seedInputField.text))
            {
                GameSeedManager.Instance.seedString = seedInputField.text;
                GameSeedManager.Instance.InitializeSeed();
            }
        }

        if (gameUIAnimation) gameUIAnimation.SlideIn();
        if (backgroundManager) backgroundManager.StartColorChange();

        Debug.Log("⏳ Menü kayıyor...");
        yield return StartCoroutine(SlideMenu(onScreenPosition, offScreenPosition, false));

        if (GameLogManager.Instance != null)
        {
            yield return StartCoroutine(GameLogManager.Instance.PlayBootSequence());
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        yield return new WaitForSeconds(0.4f);

        if (dm != null)
        {
            dm.StartGameLoop();
        }

        isBooting = false;
    }

    public void OnReturnToMenuPressed()
    {
        Time.timeScale = 1f;
        isBooting = false;
        if (gameUIAnimation) gameUIAnimation.SlideOut();
        if (backgroundManager) backgroundManager.ResetColor();
        GameUIManager uiManager = FindFirstObjectByType<GameUIManager>();
        if (uiManager != null) uiManager.ResetPanels();
        StartCoroutine(SlideMenu(offScreenPosition, onScreenPosition, true));
        ResetGameData();
    }

    private void ResetGameData()
    {
        DiceManager dm = FindFirstObjectByType<DiceManager>();
        if (dm != null) dm.FullReset();
    }

    private IEnumerator SlideMenu(Vector2 start, Vector2 end, bool setActiveAtEnd)
    {
        if (menuPanel == null) yield break;
        menuPanel.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < slideDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / slideDuration);
            menuPanel.anchoredPosition = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        menuPanel.anchoredPosition = end;

        
        if (!setActiveAtEnd && end == offScreenPosition) menuPanel.gameObject.SetActive(false);
    }

    public void OnQuitButtonPressed()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnTutorialButtonPressed()
    {
        
        TutorialController tutorial = FindFirstObjectByType<TutorialController>();
        if (tutorial != null)
        {
            tutorial.OpenTutorial();
        }
        else
        {
            Debug.LogError("Sahneye TutorialController koymayı unuttun!");
        }
    }
}