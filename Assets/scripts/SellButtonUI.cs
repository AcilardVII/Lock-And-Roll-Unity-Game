using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SellButtonUI : MonoBehaviour
{
    public static SellButtonUI Instance;

    [Header("UI References")]
    public GameObject sellPanelRoot;
    public Button sellButton; 
    public RectTransform dropZoneRect; 
    public TextMeshProUGUI sellPriceText;

    public DiceManager diceManager;
    private diceDragScript currentSelectedDice;
    private CoinDragSnap currentSelectedJoker;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        sellPanelRoot.SetActive(false);

        
        if (dropZoneRect == null && sellButton != null)
            dropZoneRect = sellButton.GetComponent<RectTransform>();
    }

    void Update()
    {
        if ((currentSelectedDice != null || currentSelectedJoker != null) && sellPanelRoot.activeSelf)
            sellButton.interactable = diceManager.IsShopOpen;
    }

    public void ShowSellButton(diceDragScript dice)
    {
        currentSelectedJoker = null;
        currentSelectedDice = dice;
        sellPanelRoot.SetActive(true);
        sellPriceText.text = "+$" + dice.shopValue;
    }

    public void ShowSellButtonForJoker(CoinDragSnap joker)
    {
        currentSelectedDice = null;
        currentSelectedJoker = joker;
        sellPanelRoot.SetActive(true);
        sellPriceText.text = "+$" + Mathf.Max(1, joker.shopPrice / 2);
    }

    public void HideSellButton()
    {
        currentSelectedDice = null;
        currentSelectedJoker = null;
        sellPanelRoot.SetActive(false);
    }

    
    public bool IsMouseOverSellZone()
    {
        if (!sellPanelRoot.activeSelf || dropZoneRect == null) return false;

        
        return RectTransformUtility.RectangleContainsScreenPoint(dropZoneRect, Input.mousePosition, Camera.main);
    }
}