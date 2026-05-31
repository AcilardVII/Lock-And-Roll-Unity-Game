using UnityEngine;
using System.Text.RegularExpressions; 

public class CoinSlot : MonoBehaviour
{
    
    public CoinDragSnap occupant;

    
    public int slotID = 999;

    
    public bool IsOccupied => occupant != null;

    void Awake()
    {
        
        string numbers = Regex.Replace(gameObject.name, "[^0-9]", "");

        if (int.TryParse(numbers, out int result))
        {
            slotID = result;
        }
    }

    public void SetOccupant(CoinDragSnap coin)
    {
        occupant = coin;
    }

    public void Clear()
    {
        occupant = null;
    }
}