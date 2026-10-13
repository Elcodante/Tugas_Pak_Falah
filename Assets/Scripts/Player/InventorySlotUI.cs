using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode; // Wajib ditambahkan

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Identitas Kotak")]
    public int slotIndex = -1;

    // Fungsi cerdas untuk selalu mencari Inventory milik KITA SENDIRI, bukan milik pemain lain
    private InventoryManager GetLocalInventory()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                return NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<InventoryManager>();
            }
        }
        return null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        GetLocalInventory()?.BeginDrag(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        GetLocalInventory()?.OnDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GetLocalInventory()?.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        GetLocalInventory()?.ReceiveDrop(this);
    }
}