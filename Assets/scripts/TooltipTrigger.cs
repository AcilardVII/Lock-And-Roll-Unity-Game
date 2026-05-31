using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Text.RegularExpressions;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Settings")]
    [Range(0f, 2f)]
    public float hoverDelay = 0.5f;

    [Header("Content")]
    public bool useComponentData = true;
    public string header;
    [TextArea] public string content;
    public int price;

    private Coroutine delayCoroutine;

    
    private CoinDragSnap externalJokerData;
    private DiceThrow externalDiceData;

    
    public void SetJokerDataFromPrefab(CoinDragSnap prefabScript)
    {
        externalJokerData = prefabScript;
        externalDiceData = null;
        useComponentData = false;
    }

    
    public void SetDiceDataFromPrefab(DiceThrow prefabScript)
    {
        externalDiceData = prefabScript;
        externalJokerData = null;
        useComponentData = false;

        if (prefabScript != null)
        {
            var prefabTooltip = prefabScript.GetComponent<TooltipTrigger>();
            if (prefabTooltip != null)
            {
                this.header = prefabTooltip.header;
                
            }
            else
            {
                this.header = "DICE";
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { StartHoverSequence(); }
    public void OnPointerExit(PointerEventData eventData) { StopHoverSequence(); }
    public void OnPointerClick(PointerEventData eventData) { StopHoverSequence(); }

    private void OnMouseEnter()
    {
        if (externalDiceData == null && externalJokerData == null)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        }
        StartHoverSequence();
    }
    private void OnMouseExit() { StopHoverSequence(); }
    private void OnMouseDown() { StopHoverSequence(); }

    private void StartHoverSequence() { StopCheck(); delayCoroutine = StartCoroutine(WaitAndShow()); }
    private void StopHoverSequence() { StopCheck(); if (TooltipManager.Instance != null) TooltipManager.Instance.HideTooltip(); }
    private void StopCheck() { if (delayCoroutine != null) { StopCoroutine(delayCoroutine); delayCoroutine = null; } }

    IEnumerator WaitAndShow()
    {
        yield return new WaitForSeconds(hoverDelay);
        if (TooltipManager.Instance == null) yield break;

        string finalHeader = header;
        string finalContent = content;

        CoinDragSnap.JokerRarity currentRarity = CoinDragSnap.JokerRarity.Common;
        bool shouldShowRarity = false;

       
        CoinDragSnap jokerScript = externalJokerData;
        if (jokerScript == null && useComponentData) jokerScript = GetComponent<CoinDragSnap>();

        if (jokerScript != null)
        {
            
            finalHeader = jokerScript.jokerTitle;
            currentRarity = jokerScript.rarity;
            shouldShowRarity = true;
            finalContent = GenerateStatsText(jokerScript);
        }
        else
        {
            
            DiceThrow diceScript = externalDiceData;
            if (diceScript == null && useComponentData) diceScript = GetComponent<DiceThrow>();

            if (diceScript != null)
            {
                shouldShowRarity = false;

                
                finalContent = "";

                
                if (diceScript.currentRig != DiceThrow.RiggedType.None)
                {
                    finalHeader = GetRiggedHeader(diceScript, finalHeader);
                    finalContent = GetRiggedContent(diceScript);
                }
            }
            else
            {
                
                finalContent = FormatDescription(finalContent);
            }
        }

        
        DiceManager dm = FindFirstObjectByType<DiceManager>();
        if (dm != null && dm.IsShopOpen)
        {
            
            if (externalJokerData == null && externalDiceData == null && useComponentData)
            {
                if (GetComponent<CoinDragSnap>() != null || GetComponent<DiceThrow>() != null)
                {
                    string colHighlight = "#FFAA00";
                    if (TooltipManager.Instance != null)
                        colHighlight = TooltipManager.Instance.ToHex(TooltipManager.Instance.customHighlightColor);

                    
                    if (string.IsNullOrEmpty(finalContent))
                        finalContent = $"<size=85%>Press <color={colHighlight}>E</color> to Sell</size>";
                    else
                        finalContent += $"\n\n<size=85%>Press <color={colHighlight}>E</color> to Sell</size>";
                }
            }
        }

        TooltipManager.Instance.ShowTooltip(finalHeader, finalContent, currentRarity, shouldShowRarity);
    }

    // --- YARDIMCI FONKSİYONLAR ---

    private string GetRiggedHeader(DiceThrow dice, string defaultHeader)
    {
        if (dice == null || dice.currentRig == DiceThrow.RiggedType.None) return defaultHeader;

        switch (dice.currentRig)
        {
            case DiceThrow.RiggedType.AlwaysSpecific: return $"Rigged \"{dice.rigValue}\"";
            case DiceThrow.RiggedType.AddValue: return $"Rigged \"+{dice.rigValue}\"";
            case DiceThrow.RiggedType.AlwaysMax: return $"Rigged \"{dice.maxRange}\"";
            case DiceThrow.RiggedType.AlwaysMin: return $"Rigged \"{dice.minRange}\"";
            default: return defaultHeader;
        }
    }

    private string GetRiggedContent(DiceThrow dice)
    {
        if (dice == null || dice.currentRig == DiceThrow.RiggedType.None) return "";

        string colorHex = "#FF4444";
        
        string desc = "";

        switch (dice.currentRig)
        {
            case DiceThrow.RiggedType.AlwaysSpecific:
                desc = $"<color={colorHex}>This dice always results in {dice.rigValue}.</color>";
                break;
            case DiceThrow.RiggedType.AddValue:
                desc = $"<color={colorHex}>Always adds +{dice.rigValue} to the roll result.</color>";
                break;
            case DiceThrow.RiggedType.AlwaysMax:
                desc = $"<color={colorHex}>Always rolls the highest possible value ({dice.maxRange}).</color>";
                break;
            case DiceThrow.RiggedType.AlwaysMin:
                desc = $"<color={colorHex}>Always rolls the lowest possible value ({dice.minRange}).</color>";
                break;
        }

        return desc;
    }

    private string GenerateStatsText(CoinDragSnap script)
    {
        string stats = "";
        if (!string.IsNullOrEmpty(script.shopDescription))
            stats += FormatDescription(script.shopDescription);
        return stats.Trim();
    }

    private string FormatDescription(string rawText)
    {
        if (string.IsNullOrEmpty(rawText) || TooltipManager.Instance == null) return rawText;

        string processed = rawText;
        processed = processed.Replace("Chips", "Points");

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