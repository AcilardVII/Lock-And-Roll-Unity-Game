using UnityEngine;

public class ShopForgeSlot : MonoBehaviour
{
    public diceDragScript currentDice;
    public ShopManager shopManager;

    
    public bool IsOccupied => currentDice != null;

    public void OnDiceDropped(diceDragScript dice)
    {
        currentDice = dice;
        
        if (shopManager != null)
            shopManager.OnForgeSlotOccupied(dice);
    }

    public void ClearSlot()
    {
        currentDice = null;
        if (shopManager != null)
            shopManager.OnForgeSlotCleared();
    }
}