using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;

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

public class InventoryManager : NetworkBehaviour
{
    [Header("Data Inventori Lokal")]
    public InventorySlot[] slots = new InventorySlot[12];
    public int activeSlotIndex = 0;

    [Header("Referensi Visual Karakter (Pegang Item)")]
    public SpriteRenderer heldItemVisual;
    private MovementController moveControl;
    private MiningController miningControl;

    // --- KUNCI PERBAIKAN: DATABASE MULTIPLAYER ---
    [Header("Database Item Multiplayer")]
    [Tooltip("Masukkan SEMUA gambar/sprite item yang ada di game ini ke sini agar bisa dilihat pemain lain")]
    public Sprite[] allGameItemSprites;

    // Sinkronisasi index gambar yang sedang dipegang (-1 berarti tangan kosong)
    public NetworkVariable<int> syncedHeldItemIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner // Hanya pemilik karakter yang boleh mengubah status tangannya
    );

    // Variabel UI internal
    private GameObject mainInventoryPanel;
    private RectTransform highlightBox;
    private RectTransform[] hotbarUI;
    private Image[] slotIcons;
    private TMP_Text[] slotTexts;
    private GameObject dragIconObject;
    private Image dragIconImage;

    private int draggedSlotIndex = -1;
    private bool isDragging = false;
    private bool isUIReady = false;

    private void Awake()
    {
        moveControl = GetComponent<MovementController>();
        miningControl = GetComponent<MiningController>();

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new InventorySlot();
        }

        if (heldItemVisual != null)
        {
            heldItemVisual.sprite = null;
            heldItemVisual.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        // Daftarkan event perubahan visual ke SEMUA pemain (Agar pemain lain bisa melihat tas kita)
        syncedHeldItemIndex.OnValueChanged += (oldIndex, newIndex) => {
            RefreshHeldItemVisual(newIndex);
        };

        // Segarkan saat pertama kali spawn (Berguna untuk pemain yang telat join agar langsung melihat item di tangan kita)
        RefreshHeldItemVisual(syncedHeldItemIndex.Value);

        if (IsOwner)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(ConnectToUI());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex != 0 && IsOwner) StartCoroutine(ConnectToUI());
    }

    private IEnumerator ConnectToUI()
    {
        isUIReady = false;

        yield return new WaitUntil(() => InventoryUIManager.Instance != null);

        InventoryUIManager ui = InventoryUIManager.Instance;
        mainInventoryPanel = ui.mainInventoryPanel;
        highlightBox = ui.highlightBox;
        hotbarUI = ui.hotbarUI;
        slotIcons = ui.slotIcons;
        slotTexts = ui.slotTexts;
        dragIconObject = ui.dragIconObject;

        if (dragIconObject != null)
        {
            dragIconImage = dragIconObject.GetComponent<Image>();
        }

        isUIReady = true;

        SelectHotbar(activeSlotIndex);
        UpdateAllSlotsUI();
        Debug.Log($"[Inventory] Player {OwnerClientId} berhasil terhubung ke UI Inventori!");
    }

    void Update()
    {
        if (!IsOwner || !isUIReady) return;

        if (Input.GetKeyDown(KeyCode.I)) mainInventoryPanel.SetActive(!mainInventoryPanel.activeSelf);
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectHotbar(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectHotbar(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectHotbar(2);

        if (Input.GetKeyDown(KeyCode.B)) DropItem(activeSlotIndex);
    }

    public void SelectHotbar(int index)
    {
        if (!isUIReady) return;
        activeSlotIndex = index;
        if (highlightBox != null && hotbarUI.Length > index) highlightBox.position = hotbarUI[index].position;
        UpdateEquipVisual();
    }

    public bool AddItem(TileBase tile, Sprite sprite)
    {
        if (!IsOwner) return false;

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

    // --- SISTEM DRAG & DROP ---
    public void BeginDrag(InventorySlotUI slotUI)
    {
        if (!IsOwner || !isUIReady || slotUI.slotIndex < 0 || slots[slotUI.slotIndex].IsEmpty()) return;

        isDragging = true;
        draggedSlotIndex = slotUI.slotIndex;
        dragIconImage.sprite = slots[draggedSlotIndex].itemSprite;
        dragIconObject.SetActive(true);
        dragIconObject.transform.position = Input.mousePosition;
        slotIcons[draggedSlotIndex].color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag()
    {
        if (!IsOwner || !isDragging) return;
        dragIconObject.transform.position = Input.mousePosition;
    }

    public void EndDrag()
    {
        if (!IsOwner || !isDragging) return;
        isDragging = false;
        dragIconObject.SetActive(false);

        if (draggedSlotIndex != -1)
        {
            UpdateSlotUI(draggedSlotIndex);
            UpdateEquipVisual();
            draggedSlotIndex = -1;
        }
    }

    public void ReceiveDrop(InventorySlotUI targetSlotUI)
    {
        if (!IsOwner || !isDragging || draggedSlotIndex == -1) return;

        int targetIndex = targetSlotUI.slotIndex;

        if (targetIndex == -1)
        {
            DropItem(draggedSlotIndex);
            return;
        }

        if (targetIndex == draggedSlotIndex) return;

        InventorySlot sourceSlot = slots[draggedSlotIndex];
        InventorySlot targetSlot = slots[targetIndex];

        if (targetSlot.IsEmpty())
        {
            slots[targetIndex] = sourceSlot;
            slots[draggedSlotIndex] = new InventorySlot();
        }
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
        else
        {
            InventorySlot temp = slots[targetIndex];
            slots[targetIndex] = sourceSlot;
            slots[draggedSlotIndex] = temp;
        }

        UpdateSlotUI(draggedSlotIndex);
        UpdateSlotUI(targetIndex);
    }

    // --- SISTEM DROP MULTIPLAYER ---
    private void DropItem(int indexToDrop)
    {
        if (!IsOwner) return;

        InventorySlot slotToDrop = slots[indexToDrop];
        if (slotToDrop.IsEmpty()) return;

        int mappingIndex = -1;
        if (miningControl != null)
        {
            for (int i = 0; i < miningControl.blockMappings.Length; i++)
            {
                if (miningControl.blockMappings[i].blockTile == slotToDrop.itemTile)
                {
                    mappingIndex = i;
                    break;
                }
            }
        }

        if (mappingIndex != -1)
        {
            float arahX = (transform.localScale.x < 0) ? -1f : 1f;
            Vector3 spawnPos = transform.position + new Vector3(arahX * 1.0f, 0.2f, 0);
            SpawnDropItemServerRpc(mappingIndex, spawnPos, arahX);
        }

        slotToDrop.count--;
        if (slotToDrop.count <= 0)
        {
            slotToDrop.itemTile = null;
            slotToDrop.itemSprite = null;
        }

        UpdateSlotUI(indexToDrop);
        UpdateEquipVisual();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnDropItemServerRpc(int mappingIndex, Vector3 spawnPos, float arahX)
    {
        if (miningControl == null || mappingIndex < 0 || mappingIndex >= miningControl.blockMappings.Length) return;

        GameObject prefabToDrop = miningControl.blockMappings[mappingIndex].dropPrefab;
        if (prefabToDrop != null)
        {
            GameObject droppedItem = Instantiate(prefabToDrop, spawnPos, Quaternion.identity);
            NetworkObject netObj = droppedItem.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 throwDirection = new Vector2(arahX, 0.5f).normalized;
                rb.AddForce(throwDirection * 5f, ForceMode2D.Impulse);
            }
        }
    }

    // --- SISTEM PENGURANGAN ITEM (UNTUK QUEST) ---
    public int RemoveItem(Sprite targetSprite, int amountToRemove)
    {
        if (targetSprite == null || amountToRemove <= 0) return 0;

        int removed = 0;

        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty() && slots[i].itemSprite != null && slots[i].itemSprite.name == targetSprite.name)
            {
                if (slots[i].count <= (amountToRemove - removed))
                {
                    removed += slots[i].count;
                    slots[i].count = 0;
                    slots[i].itemTile = null;
                    slots[i].itemSprite = null;
                }
                else
                {
                    slots[i].count -= (amountToRemove - removed);
                    removed = amountToRemove;
                }

                UpdateSlotUI(i);
                if (removed >= amountToRemove) break;
            }
        }

        UpdateEquipVisual();
        return removed;
    }

    // ==========================================
    // UI UPDATER LOKAL & JARINGAN (BARU)
    // ==========================================

    // Fungsi ini dipanggil HANYA oleh Pemilik Karakter
    private void UpdateEquipVisual()
    {
        if (!IsOwner) return;

        if (!slots[activeSlotIndex].IsEmpty() && slots[activeSlotIndex].itemSprite != null)
        {
            // Cari item ini ada di urutan ke berapa pada Database kita
            int foundIndex = -1;
            for (int i = 0; i < allGameItemSprites.Length; i++)
            {
                if (allGameItemSprites[i] != null && allGameItemSprites[i].name == slots[activeSlotIndex].itemSprite.name)
                {
                    foundIndex = i;
                    break;
                }
            }

            // Ubah NetworkVariable (Akan memicu RefreshHeldItemVisual secara otomatis ke SEMUA PEMAIN)
            syncedHeldItemIndex.Value = foundIndex;

            if (moveControl != null) moveControl.isHoldingItem = true;
        }
        else
        {
            // Tangan kosong
            syncedHeldItemIndex.Value = -1;
            if (moveControl != null) moveControl.isHoldingItem = false;
        }
    }

    // Fungsi ini dieksekusi oleh SEMUA KOMPUTER secara bersamaan
    private void RefreshHeldItemVisual(int itemIndex)
    {
        if (heldItemVisual == null) return;

        // Jika index valid, pasang gambarnya dan tampilkan tasnya
        if (itemIndex >= 0 && itemIndex < allGameItemSprites.Length && allGameItemSprites[itemIndex] != null)
        {
            heldItemVisual.sprite = allGameItemSprites[itemIndex];
            heldItemVisual.gameObject.SetActive(true);
        }
        else // Jika -1 (kosong), sembunyikan
        {
            heldItemVisual.sprite = null;
            heldItemVisual.gameObject.SetActive(false);
        }
    }

    private void UpdateSlotUI(int index)
    {
        if (!isUIReady) return;

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
        if (!isUIReady) return;
        for (int i = 0; i < slots.Length; i++) UpdateSlotUI(i);
    }
}