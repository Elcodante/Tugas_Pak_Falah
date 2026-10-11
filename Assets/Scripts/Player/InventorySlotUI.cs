using UnityEngine;
using UnityEngine.EventSystems; 

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Identitas Kotak")]
    public int slotIndex = -1;

    private InventoryManager inventoryManager;

    void Start()
    {
        // Mencari script InventoryManager yang ada di Player
        inventoryManager = FindFirstObjectByType<InventoryManager>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventoryManager != null)
        {
            inventoryManager.BeginDrag(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnDrag();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (inventoryManager != null)
        {
            inventoryManager.EndDrag();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (inventoryManager != null)
        {
            inventoryManager.ReceiveDrop(this);
        }
    }
}