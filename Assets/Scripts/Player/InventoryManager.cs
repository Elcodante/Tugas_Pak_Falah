using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using TMPro;
using System.Collections;

[System.Serializable]
public class InventorySlot
{
    public TileBase itemTile;
    public Sprite itemSprite;
    public int count = 0;
    public int maxStack = 5;

    public bool IsEmpty() { return count == 0; }
    public bool IsFull() { return count >= maxStack; }
}

public class InventoryManager : MonoBehaviour
{
    [Header("Data Inventori")]
    public InventorySlot[] slots = new InventorySlot[12];
    public int activeSlotIndex = 0;

    [Header("Referensi Visual Karakter (Pegang Item)")]
    public SpriteRenderer heldItemVisual;
    private MovementController moveControl;
    private MiningController miningControl;

    [Header("Referensi UI (Posisi & Bingkai)")]
    public GameObject mainInventoryPanel;
    public RectTransform highlightBox;
    public RectTransform[] hotbarUI;

    [Header("Referensi Visual (Ikon & Angka)")]
    public Image[] slotIcons;
    public TMP_Text[] slotTexts;

    [Header("Sistem Drag & Drop")]
    public GameObject dragIconObject; // Objek Drag_Icon milikmu
    private Image dragIconImage;
    private int draggedSlotIndex = -1;
    private bool isDragging = false;

    IEnumerator Start()
    {
        moveControl = GetComponent<MovementController>();
        miningControl = GetComponent<MiningController>();

        if (dragIconObject != null)
        {
            dragIconImage = dragIconObject.GetComponent<Image>();
            dragIconObject.SetActive(false); // Sembunyikan ikon bayangan di awal
        }

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new InventorySlot();
        }

        yield return new WaitForEndOfFrame();
        SelectHotbar(0);
        UpdateAllSlotsUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) mainInventoryPanel.SetActive(!mainInventoryPanel.activeSelf);
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectHotbar(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectHotbar(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectHotbar(2);

        if (Input.GetKeyDown(KeyCode.B)) DropItem(activeSlotIndex); // Lempar dari kotak yang aktif
    }

    public void SelectHotbar(int index)
    {
        activeSlotIndex = index;
        if (highlightBox != null && hotbarUI.Length > index) highlightBox.position = hotbarUI[index].position;
        UpdateEquipVisual();
    }

    public bool AddItem(TileBase tile, Sprite sprite)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty() && slots[i].itemTile == tile && !slots[i].IsFull())
            {
                slots[i].count++;
                UpdateSlotUI(i);
                UpdateEquipVisual();
                return true;
            }
        }
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty())
            {
                slots[i].itemTile = tile;
                slots[i].itemSprite = sprite;
                slots[i].count = 1;
                UpdateSlotUI(i);
                UpdateEquipVisual();
                return true;
            }
        }
        return false;
    }

    public void BeginDrag(InventorySlotUI slotUI)
    {
        // Tolak jika menggeser kotak sampah atau kotak kosong
        if (slotUI.slotIndex < 0 || slots[slotUI.slotIndex].IsEmpty()) return;

        isDragging = true;
        draggedSlotIndex = slotUI.slotIndex;

        // Munculkan ikon bayangan yang mengikuti mouse
        dragIconImage.sprite = slots[draggedSlotIndex].itemSprite;
        dragIconObject.SetActive(true);
        dragIconObject.transform.position = Input.mousePosition;

        // Bikin gambar item di kotak asalnya jadi agak pudar (transparan) selama ditarik
        slotIcons[draggedSlotIndex].color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag()
    {
        if (!isDragging) return;
        // Posisikan ikon bayangan persis di ujung kursor mouse
        dragIconObject.transform.position = Input.mousePosition;
    }

    public void EndDrag()
    {
        if (!isDragging) return;
        isDragging = false;
        dragIconObject.SetActive(false); // Sembunyikan ikon bayangan

        // Update tampilan kotak asal agar warnanya tidak pudar lagi
        if (draggedSlotIndex != -1)
        {
            UpdateSlotUI(draggedSlotIndex);
            UpdateEquipVisual();
            draggedSlotIndex = -1; // Reset dilakukan di akhir sini!
        }
    }

    public void ReceiveDrop(InventorySlotUI targetSlotUI)
    {
        if (!isDragging || draggedSlotIndex == -1) return;

        int targetIndex = targetSlotUI.slotIndex;

        // Skenario Buang ke Kotak Sampah
        if (targetIndex == -1)
        {
            DropItem(draggedSlotIndex);
            return; // Hapus reset -1 di sini
        }

        if (targetIndex == draggedSlotIndex) return;

        InventorySlot sourceSlot = slots[draggedSlotIndex];
        InventorySlot targetSlot = slots[targetIndex];

        // Skenario A: Pindah ke Kotak Kosong
        if (targetSlot.IsEmpty())
        {
            slots[targetIndex] = sourceSlot;
            slots[draggedSlotIndex] = new InventorySlot();
        }
        // Skenario B: Tumpuk (Stack)
        else if (targetSlot.itemTile == sourceSlot.itemTile)
        {
            int total = targetSlot.count + sourceSlot.count;
            if (total <= targetSlot.maxStack)
            {
                targetSlot.count = total;
                slots[draggedSlotIndex] = new InventorySlot();
            }
            else
            {
                targetSlot.count = targetSlot.maxStack;
                sourceSlot.count = total - targetSlot.maxStack;
            }
        }
        // Skenario C: Tukar Posisi (Swap)
        else
        {
            InventorySlot temp = slots[targetIndex];
            slots[targetIndex] = sourceSlot;
            slots[draggedSlotIndex] = temp;
        }

        // Perbarui tampilan kedua kotak
        UpdateSlotUI(draggedSlotIndex);
        UpdateSlotUI(targetIndex);
        // Hapus reset -1 di sini
    }

    private void DropItem(int indexToDrop)
    {
        InventorySlot slotToDrop = slots[indexToDrop];
        if (slotToDrop.IsEmpty()) return;

        GetComponent<Animator>().SetTrigger("Drop");

        GameObject prefabToDrop = null;
        if (miningControl != null)
        {
            foreach (BlockMapping mapping in miningControl.blockMappings)
            {
                if (mapping.blockTile == slotToDrop.itemTile)
                {
                    prefabToDrop = mapping.dropPrefab;
                    break;
                }
            }
        }

        if (prefabToDrop != null)
        {
            float arahX = (transform.localScale.x < 0) ? -1f : 1f;
            Vector3 spawnPos = transform.position + new Vector3(arahX * 1.0f, 0.2f, 0);

            GameObject droppedItem = Instantiate(prefabToDrop, spawnPos, Quaternion.identity);
            Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 throwDirection = new Vector2(arahX, 0.5f).normalized;
                rb.AddForce(throwDirection * 5f, ForceMode2D.Impulse);
            }
        }

        // Kurangi jumlah
        slotToDrop.count--;
        if (slotToDrop.count <= 0)
        {
            slotToDrop.itemTile = null;
            slotToDrop.itemSprite = null;
        }

        UpdateSlotUI(indexToDrop);
        UpdateEquipVisual();
    }

    private void UpdateEquipVisual()
    {
        if (heldItemVisual == null) return;
        if (!slots[activeSlotIndex].IsEmpty())
        {
            heldItemVisual.sprite = slots[activeSlotIndex].itemSprite;
            heldItemVisual.gameObject.SetActive(true);
            if (moveControl != null) moveControl.isHoldingItem = true;
        }
        else
        {
            heldItemVisual.sprite = null;
            heldItemVisual.gameObject.SetActive(false);
            if (moveControl != null) moveControl.isHoldingItem = false;
        }
    }

    private void UpdateSlotUI(int index)
    {
        if (!slots[index].IsEmpty())
        {
            slotIcons[index].sprite = slots[index].itemSprite;
            slotIcons[index].color = new Color(1, 1, 1, 1);
            slotTexts[index].text = slots[index].count.ToString();
        }
        else
        {
            slotIcons[index].sprite = null;
            slotIcons[index].color = new Color(1, 1, 1, 0);
            slotTexts[index].text = "";
        }
    }

    private void UpdateAllSlotsUI()
    {
        for (int i = 0; i < slots.Length; i++) UpdateSlotUI(i);
    }
}