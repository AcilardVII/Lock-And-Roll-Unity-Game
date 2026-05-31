using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class diceDragScript : MonoBehaviour
{
    public DiceManager diceManager;
    public Transform[] snapPoints;
    public float hoverHeight = 0.5f;
    public float swaySpeed = 2.0f;
    public float swayAmplitude = 0.1f;
    public int shopValue = 0;

 
    private ShopForgeSlot targetForgeSlot;

    public Transform CurrentSlotTransform => (currentSlot != null) ? currentSlot.transform : ((targetForgeSlot != null && currentSlot == null) ? targetForgeSlot.transform : null);

    private SnapSlots currentSlot;
    private bool isSelected = false;
    private Vector3 positionBeforeSelection;
    private Rigidbody2D rb;
    private DiceThrow dice;
    public bool isPreview = false;


    void Start()
    {
        if (diceManager == null)
        {
            diceManager = FindFirstObjectByType<DiceManager>();
        }
    }

    void Awake() { rb = GetComponent<Rigidbody2D>(); dice = GetComponent<DiceThrow>(); }

    void Update()
    {
        if (rb.bodyType == RigidbodyType2D.Dynamic || (dice != null && dice.IsResultAnimating)) return;

        if (isSelected)
        {
           
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; 
            transform.position = Vector3.Lerp(transform.position, mousePos, Time.deltaTime * 15f);

            if (Input.GetMouseButtonUp(0)) DropDice();
        }
    }

    void OnMouseDown()
    {
        if (isPreview) return;

        if (rb.bodyType == RigidbodyType2D.Dynamic) return;

       
        if (dice != null) dice.ForceWakeUp();

        SelectDice();
    }

    public void SelectDice()
    {
        positionBeforeSelection = transform.position;
        isSelected = true;

        if (targetForgeSlot != null && targetForgeSlot.currentDice == this)
        {
            targetForgeSlot.ClearSlot();
            targetForgeSlot = null;
        }

        if (currentSlot != null)
        {
            currentSlot.ClearOccupant(this);
            currentSlot = null;
        }

        if (SellButtonUI.Instance != null) SellButtonUI.Instance.ShowSellButton(this);
    }

    public void DropDice()
    {
        isSelected = false;

       
        if (SellButtonUI.Instance != null && SellButtonUI.Instance.IsMouseOverSellZone())
        {
            if (diceManager != null && diceManager.GetAllDiceCount() > 1)
            {
                SellSelf();
                SellButtonUI.Instance.HideSellButton();
                return;
            }
            else
            {
               
                if (diceManager != null) diceManager.ShowVisualWarning(diceManager.warningMsgLastDice);

                SnapBackToCurrentSlot();
                SellButtonUI.Instance.HideSellButton();
                return;
            }
        }

        if (SellButtonUI.Instance != null) SellButtonUI.Instance.HideSellButton();

       
        if (diceManager != null && diceManager.IsShopOpen)
        {
            var shop = diceManager.shopManager;
            if (shop != null && shop.forgeSlot != null)
            {
                float dist = Vector2.Distance(transform.position, shop.forgeSlot.transform.position);
                if (dist < 1.5f && (!shop.forgeSlot.IsOccupied || shop.forgeSlot.currentDice == this))
                {
                    SnapToForgeSlot(shop.forgeSlot);
                    return;
                }
            }
        }

       
        float bestDist = float.MaxValue;
        SnapSlots bestSlot = null;

        if (snapPoints != null)
        {
            foreach (var t in snapPoints)
            {
                var slot = t.GetComponent<SnapSlots>();
                if (slot != null && !slot.IsOccupied && !slot.isRollSlot)
                {
                    float d = Vector2.Distance(transform.position, t.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestSlot = slot;
                    }
                }
            }
        }

        if (bestSlot != null && bestDist < 1.0f)
        {
            ForceSnapToSlot(bestSlot.transform);
        }
        else
        {
            diceManager.ReturnSingleDiceToTray(this);
        }
    }

    public void SnapBackToCurrentSlot()
    {
        isSelected = false;
        if (CurrentSlotTransform != null)
        {
            transform.position = CurrentSlotTransform.position;
        }
        else
        {
            if (diceManager != null) diceManager.ReturnSingleDiceToTray(this);
        }
    }

    public void SnapToForgeSlot(ShopForgeSlot slot)
    {
        targetForgeSlot = slot;
        slot.OnDiceDropped(this);
        transform.position = slot.transform.position;
        if (SellButtonUI.Instance != null) SellButtonUI.Instance.HideSellButton();
    }

    public void SellSelf()
    {
       
        if (diceManager != null && diceManager.GetAllDiceCount() <= 1)
        {
            SnapBackToCurrentSlot();
            return;
        }

        if (currentSlot != null) currentSlot.ClearOccupant(this);
        if (targetForgeSlot != null) targetForgeSlot.ClearSlot();

        diceManager.GainMoney(shopValue, true);
        diceManager.UnregisterAndDestroyDice(dice);
    }

    public void ForceSnapToSlot(Transform slotT)
    {
        targetForgeSlot = null;
        var slot = slotT.GetComponent<SnapSlots>();
        if (slot != null) { slot.SetOccupant(this); currentSlot = slot; }
        transform.position = slotT.position;
        if (dice != null) dice.SetLockedPosition(slotT.position, slot != null && slot.isRollSlot);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}