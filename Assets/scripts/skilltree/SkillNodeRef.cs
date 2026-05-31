using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using System.Text.RegularExpressions; 

public class SkillNodeRef : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public SkillData data;
    public TextMeshProUGUI statusText;
    public Image iconImage;

    private Button btn;
    private SkillTreeController controller;

    void Start()
    {
        btn = GetComponent<Button>();
        controller = FindFirstObjectByType<SkillTreeController>();

        if (btn != null && controller != null)
        {
            btn.onClick.AddListener(() => controller.OnNodeClicked(this));
        }

        if (data != null && iconImage != null)
        {
            iconImage.sprite = data.icon;
        }
    }

    public void UpdateText(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (data == null || TooltipManager.Instance == null) return;

        string header = data.skillName;

        
        string content = FormatDescription(data.description);

        
        TooltipManager.Instance.ShowTooltip(header, content, CoinDragSnap.JokerRarity.Common, false);
    }

    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }

   
    private string FormatDescription(string rawText)
    {
        if (string.IsNullOrEmpty(rawText) || TooltipManager.Instance == null) return rawText;

        string processed = rawText;

       
        string colHighlight = TooltipManager.Instance.ToHex(TooltipManager.Instance.customHighlightColor);
        string colPoints = TooltipManager.Instance.ToHex(TooltipManager.Instance.pointsColor);
        string colAddMult = TooltipManager.Instance.ToHex(TooltipManager.Instance.addMultColor);
        string colXMult = TooltipManager.Instance.ToHex(TooltipManager.Instance.xMultColor);
        string colMoney = TooltipManager.Instance.ToHex(TooltipManager.Instance.moneyColor);
        string colReroll = TooltipManager.Instance.ToHex(TooltipManager.Instance.rerollColor);
        string colRound = "#FFFFFF";
        try { colRound = TooltipManager.Instance.ToHex(TooltipManager.Instance.roundColor); } catch { }

       
        processed = Regex.Replace(processed, @"//(.*?)//", $"<color={colHighlight}>$1</color>");

        
        string tokenKey = "___XM_TOKEN___";
        processed = Regex.Replace(processed, @"(?i)x\s*Mult", tokenKey); 
        processed = Regex.Replace(processed, @"(?i)Mult", $"<color={colAddMult}>Mult</color>");
        processed = processed.Replace(tokenKey, $"<color={colXMult}>xMult</color>");

        processed = Regex.Replace(processed, @"(?i)Points", $"<color={colPoints}>Points</color>");
        processed = Regex.Replace(processed, @"(?i)Money", $"<color={colMoney}>Money</color>");
        processed = processed.Replace("$", $"<color={colMoney}>$</color>");
        processed = Regex.Replace(processed, @"(?i)Reroll", $"<color={colReroll}>Reroll</color>");
        processed = Regex.Replace(processed, @"(?i)Round", $"<color={colRound}>Round</color>");

        return processed;
    }
}