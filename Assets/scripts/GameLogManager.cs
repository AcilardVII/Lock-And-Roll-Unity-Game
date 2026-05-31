using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class GameLogManager : MonoBehaviour
{
    public static GameLogManager Instance;

    public enum LogCategory
    {
        System, Hack, Damage, Money, Mult, Chip, Reroll, Round, Clone, Enchantment, Locked,
        Separator,
        Checkout 
    }

    [Header("UI References")]
    public Transform logContentParent;
    public GameObject logItemPrefab;
    public ScrollRect scrollRect;

    [Header("Settings")]
    public int maxLogCount = 30;

    [Header("Boot Sequence")]
    public float bootLineDelay = 0.05f;
    public string[] bootLines = new string[]
    {
        "DETECTING PRIMITIVES... DONE",
        "CHECKING NVRAM.. OK",
        "loading kernel...",
        "ROOT ACCESS: GRANTED",
        "SYSTEM READY."
    };

    [Header("Random Flavor Texts")]
    public string[] systemMessages = { "System Optimal.", "Scanning...", "Ping: 1ms", "Memory: OK" };
    public string[] hackMessages = { "Bypassed firewall.", "Injecting payload...", "Brute-force success.", "Rootkit installed." };

    [Header("PREFIX COLORS (Başlıklar)")]
    public Color pColor_System = Color.green;
    public Color pColor_Rigged = new Color(1f, 0.2f, 0.2f);
    public Color pColor_Joker = new Color(0.6f, 0.2f, 1f);
    public Color pColor_Forge = new Color(1f, 0.5f, 0f);
    public Color pColor_Damage = Color.red;

    [Header("BODY COLORS (İçerik)")]
    public Color bColor_Default = Color.white;
    public Color bColor_Money = new Color(1f, 0.9f, 0.2f);
    public Color bColor_Chip = new Color(0.2f, 0.8f, 1f);
    public Color bColor_MultPlus = new Color(1f, 0.3f, 0.3f);
    public Color bColor_MultTimes = new Color(1f, 0.6f, 0f);
    public Color bColor_AddRound = new Color(0.4f, 0.4f, 1f);
    public Color bColor_AddReroll = new Color(0f, 1f, 1f);
    public Color bColor_Locked = Color.gray;
    public Color bColor_Damage = new Color(1f, 0.8f, 0.8f);

    
    public Color bColor_Checkout = new Color(0.2f, 1f, 0.2f);

    private List<GameObject> activeLogs = new List<GameObject>();

    
    private bool lastLogWasSeparator = false;

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    public IEnumerator PlayBootSequence()
    {
        ClearLogs();
        string realDate = DateTime.Now.ToString("MM/dd/yy HH:mm:ss");
        string cpuName = SystemInfo.processorType;
        string ramSize = SystemInfo.systemMemorySize + "MB";

        List<string> fullBootSequence = new List<string>();
        fullBootSequence.Add($"BIOS DATE {realDate} VER 1.0.2");
        fullBootSequence.Add($"CPU: {cpuName}");
        fullBootSequence.Add($"MEMORY: {ramSize} OK");
        fullBootSequence.AddRange(bootLines);

        foreach (var line in fullBootSequence)
        {
            string pColor = ColorUtility.ToHtmlStringRGB(pColor_System);
            string bColor = ColorUtility.ToHtmlStringRGB(bColor_Default);
            SpawnLogItem($"<color=#{pColor}>[SYSTEM]</color> <color=#{bColor}>{line}</color>");
            yield return new WaitForSeconds(bootLineDelay);
        }

        LogSeparator(); 
        yield return new WaitForSeconds(0.2f);
    }

    public void LogSeparator()
    {
        
        if (activeLogs.Count == 0) return;

        
        if (lastLogWasSeparator) return;

        SpawnLogItem("<color=#666666>--------------------------------</color>");

        
        lastLogWasSeparator = true;
    }

    public void Log(LogCategory category, string textOverride = "", int val1 = 0, int val2 = 0, Color? colorOverride = null)
    {
        if (category == LogCategory.Separator)
        {
            LogSeparator();
            return;
        }

        
        lastLogWasSeparator = false;

        string prefixText = "";
        string message = "";

        Color currentPrefixColor = pColor_System;
        Color currentBodyColor = bColor_Default;

        switch (category)
        {
            case LogCategory.System:
            case LogCategory.Clone:
                prefixText = "[SYSTEM]";
                currentPrefixColor = pColor_System;
                currentBodyColor = bColor_Default;
                message = string.IsNullOrEmpty(textOverride) ? GetRandom(systemMessages) : textOverride;
                if (category == LogCategory.Clone) message = $"Cloned: {textOverride}";
                break;

            case LogCategory.Hack:
                prefixText = "[RIGGED]";
                currentPrefixColor = pColor_Rigged;
                currentBodyColor = bColor_Default;
                if (!string.IsNullOrEmpty(textOverride)) message = textOverride; else message = GetRandom(hackMessages);
                break;

            case LogCategory.Enchantment:
                prefixText = "[FORGE]";
                currentPrefixColor = pColor_Forge;
                currentBodyColor = bColor_Default;
                message = textOverride;
                break;

            case LogCategory.Damage:
                prefixText = "[DMG]";
                currentPrefixColor = pColor_Damage;
                currentBodyColor = bColor_Damage;
                message = string.IsNullOrEmpty(textOverride) ? $"Dealt {val1} Damage" : $"{textOverride} ({val1})";
                break;

            case LogCategory.Money:
                prefixText = "[JOKER]";
                currentPrefixColor = pColor_Joker;
                currentBodyColor = bColor_Money;
                message = string.IsNullOrEmpty(textOverride) ? $"Gained ${val1}" : $"{textOverride} (+${val1})";
                break;

            case LogCategory.Chip:
                prefixText = "[JOKER]";
                currentPrefixColor = pColor_Joker;
                currentBodyColor = bColor_Chip;
                message = string.IsNullOrEmpty(textOverride) ? $"+{val1} Chips" : $"{textOverride} (+{val1})";
                break;

            case LogCategory.Mult:
                prefixText = "[JOKER]";
                currentPrefixColor = pColor_Joker;
                message = string.IsNullOrEmpty(textOverride) ? $"Mult {textOverride}" : $"{textOverride}";
                if (message.Contains("x") || message.Contains("X")) currentBodyColor = bColor_MultTimes; else currentBodyColor = bColor_MultPlus;
                break;

            case LogCategory.Reroll:
                prefixText = "[JOKER]";
                currentPrefixColor = pColor_Joker;
                currentBodyColor = bColor_AddReroll;
                message = $"{textOverride} (+{val1})";
                break;

            case LogCategory.Round:
                prefixText = "[JOKER]";
                currentPrefixColor = pColor_Joker;
                currentBodyColor = bColor_AddRound;
                message = $"{textOverride} (+{val1})";
                break;

            case LogCategory.Locked:
                prefixText = "[JOKER]";
                currentPrefixColor = pColor_Joker;
                currentBodyColor = bColor_Locked;
                message = textOverride;
                break;

            case LogCategory.Checkout:
                prefixText = "";
                currentBodyColor = bColor_Checkout;
                message = textOverride;
                break;
        }

        if (colorOverride.HasValue) currentPrefixColor = colorOverride.Value;

        string pHex = ColorUtility.ToHtmlStringRGB(currentPrefixColor);
        string bHex = ColorUtility.ToHtmlStringRGB(currentBodyColor);

        string finalLog = "";

        if (string.IsNullOrEmpty(prefixText))
        {
            finalLog = $"<color=#{bHex}>{message}</color>";
        }
        else
        {
            finalLog = $"<color=#{pHex}>{prefixText}</color> <color=#{bHex}>{message}</color>";
        }

        SpawnLogItem(finalLog);
    }

    void SpawnLogItem(string fullText)
    {
        if (logItemPrefab == null || logContentParent == null) return;
        GameObject newLog = Instantiate(logItemPrefab, logContentParent);
        TextMeshProUGUI tmp = newLog.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = fullText;
        activeLogs.Add(newLog);
        if (activeLogs.Count > maxLogCount) { if (activeLogs[0] != null) Destroy(activeLogs[0]); activeLogs.RemoveAt(0); }
        StartCoroutine(ScrollToBottom());
    }

    public void ClearLogs()
    {
        foreach (var log in activeLogs) if (log != null) Destroy(log);
        activeLogs.Clear();
        lastLogWasSeparator = false; 
    }

    IEnumerator ScrollToBottom() { yield return new WaitForEndOfFrame(); if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f; }
    string GetRandom(string[] list) { if (list == null || list.Length == 0) return ""; return list[UnityEngine.Random.Range(0, list.Length)]; }
}