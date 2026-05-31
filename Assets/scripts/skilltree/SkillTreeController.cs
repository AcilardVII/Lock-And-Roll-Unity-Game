using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SkillTreeController : MonoBehaviour
{
    [Header("UI Referansları")]
    public RectTransform skillTreePanel;
    public TextMeshProUGUI ticketCountText;
    public TextMeshProUGUI equipCountText;

    [Header("Çizgi Ayarları (Connections)")]
    public GameObject linePrefab;
    public Transform lineParent;
    public Color lockedLineColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color unlockedLineColor = new Color(0f, 1f, 1f, 1f);
    public float lineWidth = 5f;

    [Header("Animasyon Ayarları")]
    public float slideDuration = 0.5f;
    public Vector2 onScreenPos = Vector2.zero;
    public Vector2 offScreenPos = new Vector2(1920f, 0f);

    [Header("Butonlar")]
    public List<SkillNodeRef> allSkillNodes;

    [Header("Renkler (Butonlar)")]
    public Color colorLocked = Color.gray;
    public Color colorAffordable = Color.yellow;
    public Color colorTooExpensive = new Color(1f, 0.5f, 0.5f);
    public Color colorUnlocked = Color.white;
    public Color colorEquipped = Color.cyan;

    private bool isOpen = false;
    private Coroutine slideRoutine;
    private DiceManager diceManager;
    private SingleSceneBackground backgroundManager;

    void Start()
    {
        diceManager = FindFirstObjectByType<DiceManager>();
        backgroundManager = FindFirstObjectByType<SingleSceneBackground>();

        if (skillTreePanel != null) skillTreePanel.anchoredPosition = offScreenPos;

        DrawConnections();
        UpdateVisuals();
    }

    public void OpenSkillTree()
    {
        isOpen = true;
        skillTreePanel.gameObject.SetActive(true);
        StartSlide(onScreenPos);

        if (backgroundManager != null)
            backgroundManager.SetSkillTreeMode(true, slideDuration);

        UpdateVisuals();

        if (diceManager == null) diceManager = FindFirstObjectByType<DiceManager>();
        if (diceManager != null) diceManager.SetWarningPanelPosition(true);
    }

    public void CloseSkillTree()
    {
        isOpen = false;
        StartSlide(offScreenPos);

        if (backgroundManager != null)
            backgroundManager.SetSkillTreeMode(false, slideDuration);

        if (diceManager == null) diceManager = FindFirstObjectByType<DiceManager>();
        if (diceManager != null) diceManager.SetWarningPanelPosition(false);

        MainMenuController menu = FindFirstObjectByType<MainMenuController>();
        if (menu != null) menu.OnReturnFromSkillTree();
    }

    [Header("Reset Onay Kutusu")]
    public GameObject resetConfirmPanel; 

    
    public void OnResetButtonPressed()
    {
        
        if (resetConfirmPanel != null)
        {
            resetConfirmPanel.SetActive(true);
        }
    }

    
    public void OnConfirmReset()
    {
        if (ProgressionManager.Instance != null)
        {
            
            ProgressionManager.Instance.ResetAllProgress();

            
            UpdateVisuals();

            
            if (ticketCountText != null) ticketCountText.text = "0";
            if (equipCountText != null) equipCountText.text = "EQUIPPED: 0/" + ProgressionManager.Instance.maxEquipSlots;

            
            DrawConnections();
        }

        
        CloseResetPanel();
    }

    
    public void OnCancelReset()
    {
        CloseResetPanel();
    }

    private void CloseResetPanel()
    {
        if (resetConfirmPanel != null)
        {
            resetConfirmPanel.SetActive(false);
        }
    }

    public void OnNodeClicked(SkillNodeRef node)
    {
        if (node == null || node.data == null) return;
        if (diceManager == null) diceManager = FindFirstObjectByType<DiceManager>();

        string id = node.data.skillID;
        bool isUnlocked = ProgressionManager.Instance.IsSkillUnlocked(id);

        if (isUnlocked)
        {
            
            ProgressionManager.Instance.ToggleEquipSkill(id);
            UpdateVisuals();
        }
        else
        {
            
            if (AreAllParentsUnlocked(node.data))
            {
                
                if (ProgressionManager.Instance.TrySpendTickets(node.data.ticketCost))
                {
                    ProgressionManager.Instance.UnlockSkill(id);
                    UpdateVisuals();
                }
                else
                {
                    if (diceManager != null) diceManager.ShowVisualWarning("NOT ENOUGH TICKETS!");
                }
            }
            else
            {
                if (diceManager != null) diceManager.ShowVisualWarning("LOCKED!");
            }
        }
    }

    
    private bool AreAllParentsUnlocked(SkillData data)
    {
        
        if (data.requiredParentSkills == null || data.requiredParentSkills.Count == 0)
            return true;

        
        foreach (var parent in data.requiredParentSkills)
        {
            if (parent == null) continue;
            
            if (!ProgressionManager.Instance.IsSkillUnlocked(parent.skillID))
            {
                return false;
            }
        }

        
        return true;
    }

    public void UpdateVisuals()
    {
        if (ticketCountText != null)
            ticketCountText.text = ProgressionManager.Instance.currentTickets.ToString();

        int equippedCount = ProgressionManager.Instance.equippedSkillIDs.Count;
        int max = ProgressionManager.Instance.maxEquipSlots;

        if (equipCountText) equipCountText.text = $"EQUIPPED: {equippedCount}/{max}";

        foreach (var node in allSkillNodes)
        {
            if (node == null || node.data == null) continue;

            string id = node.data.skillID;
            Image img = node.GetComponent<Image>();

            bool isUnlocked = ProgressionManager.Instance.IsSkillUnlocked(id);
            bool isEquipped = ProgressionManager.Instance.IsSkillEquipped(id);

            
            bool parentsUnlocked = AreAllParentsUnlocked(node.data);

            if (isEquipped)
            {
                img.color = colorEquipped;
                node.UpdateText("EQUIPPED");
            }
            else if (isUnlocked)
            {
                img.color = colorUnlocked;
                node.UpdateText("OWNED");
            }
            else if (parentsUnlocked)
            {
                
                if (ProgressionManager.Instance.currentTickets >= node.data.ticketCost)
                    img.color = colorAffordable;
                else
                    img.color = colorTooExpensive;

                string costText = node.data.ticketCost == 0 ? "FREE" : $"BUY ({node.data.ticketCost})";
                node.UpdateText(costText);
            }
            else
            {
                
                img.color = colorLocked;
                node.UpdateText("LOCKED");
            }
        }

        UpdateConnectionColors();
    }

    

    void DrawConnections()
    {
        
        foreach (Transform child in lineParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var node in allSkillNodes)
        {
            
            if (node.data != null && node.data.requiredParentSkills != null)
            {
                foreach (var requiredParent in node.data.requiredParentSkills)
                {
                    if (requiredParent == null) continue;

                    
                    SkillNodeRef parentNodeRef = allSkillNodes.Find(x => x.data == requiredParent);

                    if (parentNodeRef != null)
                    {
                        CreateLine(parentNodeRef, node);
                    }
                }
            }
        }
    }

    void CreateLine(SkillNodeRef startNode, SkillNodeRef endNode)
    {
        if (linePrefab == null || lineParent == null) return;

        GameObject lineObj = Instantiate(linePrefab, lineParent);
        Image lineImage = lineObj.GetComponent<Image>();
        lineImage.raycastTarget = false;

        RectTransform rect = lineObj.GetComponent<RectTransform>();
        Vector3 startPos = startNode.transform.position;
        Vector3 endPos = endNode.transform.position;

        rect.position = (startPos + endPos) / 2f;

        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;
        rect.sizeDelta = new Vector2(distance / lineParent.lossyScale.x, lineWidth);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0, 0, angle);

       
        lineObj.name = $"Line_{startNode.data.skillID}_to_{endNode.data.skillID}";
    }

    void UpdateConnectionColors()
    {
        if (lineParent == null) return;

        foreach (Transform child in lineParent)
        {
            Image img = child.GetComponent<Image>();
            if (img == null) continue;

            
            string objName = child.name;

            
            string[] parts = objName.Split(new string[] { "_to_" }, System.StringSplitOptions.None);

            if (parts.Length > 0)
            {
                
                string startID = parts[0].Replace("Line_", "");

                
                bool isParentUnlocked = ProgressionManager.Instance.IsSkillUnlocked(startID);

                img.color = isParentUnlocked ? unlockedLineColor : lockedLineColor;
            }
        }
    }

    private void StartSlide(Vector2 targetPos)
    {
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlideRoutine(targetPos));
    }

    private IEnumerator SlideRoutine(Vector2 targetPos)
    {
        Vector2 startPos = skillTreePanel.anchoredPosition;
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / slideDuration);
            skillTreePanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, p);
            yield return null;
        }
        skillTreePanel.anchoredPosition = targetPos;
        if (!isOpen) skillTreePanel.gameObject.SetActive(false);
    }
}