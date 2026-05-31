using UnityEngine;

public class JokerSlot : MonoBehaviour
{
    
    public CoinDragSnap occupant;

    public bool isLocked = false;

    
    public bool IsOccupied => occupant != null;

    public void SetOccupant(CoinDragSnap coin)
    {
        occupant = coin;
    }

    public void Clear()
    {
        occupant = null;
    }
}