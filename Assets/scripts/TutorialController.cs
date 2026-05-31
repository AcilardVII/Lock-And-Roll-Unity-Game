using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TutorialController : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        public string title;       
        [TextArea] public string description; 
        public Sprite visual;      
    }

    [Header("Ýçerik")]
    public List<TutorialStep> steps; 

    [Header("UI Referanslarý")]
    public GameObject tutorialPanel;
    public Image displayImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI pageCounterText; 

    [Header("Butonlar")]
    public Button nextButton;
    public Button prevButton;
    public Button closeButton;

    private int currentIndex = 0;

    void Start()
    {
       
        nextButton.onClick.AddListener(NextStep);
        prevButton.onClick.AddListener(PrevStep);
        closeButton.onClick.AddListener(CloseTutorial);

        
        tutorialPanel.SetActive(false);
    }

    public void OpenTutorial()
    {
        tutorialPanel.SetActive(true);
        currentIndex = 0;
        UpdateUI();
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
    }

    public void NextStep()
    {
        if (currentIndex < steps.Count - 1)
        {
            currentIndex++;
            UpdateUI();
        }
        else
        {
            
            CloseTutorial();
        }
    }

    public void PrevStep()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (steps.Count == 0) return;

        TutorialStep currentStep = steps[currentIndex];

       
        titleText.text = currentStep.title;
        descriptionText.text = currentStep.description;
        pageCounterText.text = $"{currentIndex + 1} / {steps.Count}";

       
        if (currentStep.visual != null)
        {
            displayImage.gameObject.SetActive(true);
            displayImage.sprite = currentStep.visual;
            displayImage.preserveAspect = true; 
        }
        else
        {
            displayImage.gameObject.SetActive(false); 
        }

        
        prevButton.interactable = (currentIndex > 0); 

        
        TextMeshProUGUI nextBtnText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
        if (nextBtnText != null)
        {
            nextBtnText.text = (currentIndex == steps.Count - 1) ? "FINISH" : "NEXT";
        }
    }
}