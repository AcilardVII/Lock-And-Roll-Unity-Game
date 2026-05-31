using UnityEngine;

public class DiceSlot : MonoBehaviour
{
    [Header("Yuva Ayarlarý")]
    [Tooltip("Ýþaretliyse: Bu bir Savaþ Alaný slotudur (Zar burada atýlýr).\nÝþaretli Deðilse: Bu bir Envanter/Bekleme slotudur.")]
    public bool isBattleSlot = false;

    [Header("Durum")]
    public diceDragScript occupant;

    public bool IsOccupied => occupant != null;

    public void SetOccupant(diceDragScript d)
    {
        occupant = d;
    }

    public void ClearOccupant()
    {
        occupant = null;
    }
}