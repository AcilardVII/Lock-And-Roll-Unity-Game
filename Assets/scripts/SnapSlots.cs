using UnityEngine;

public class SnapSlots : MonoBehaviour
{
    public bool isRollSlot = false;

    [Header("Runtime Occupancy")]
    public diceDragScript occupant;

    public bool IsOccupied => occupant != null;

    public void SetOccupant(diceDragScript d)
    {
        occupant = d;
    }

    public void ClearOccupant(diceDragScript d)
    {
        if (occupant == d)
            occupant = null;
    }
}
