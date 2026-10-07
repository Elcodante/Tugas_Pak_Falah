using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class BlockMapping
{
    public TileBase blockTile;
    public GameObject dropPrefab;
}

public class MiningController : MonoBehaviour
{
    [Header("Pengaturan Mining")]
    public float maxMineDistance = 1.5f;
    public Tilemap targetTilemap;
    public float timeToMine = 1.0f;

    [Header("Daftar Drop Item")]
    public BlockMapping[] blockMappings;

    [Header("Simulasi Tas")]
    public int blockCount;
    public SpriteRenderer heldItemVisual;
    private TileBase currentHeldTile;

    private float mineTimer = 0f;
    private bool isMining = false;
    private Vector3Int currentMiningCell;
    private MovementController moveControl;

    private void Start()
    {
        moveControl = GetComponent<MovementController>();
        UpdateHeldVisual(null);
    }

    private void Update()
    {
        // KLIK KANAN: Menghancurkan Kristal
        if (Input.GetMouseButton(1))
        {
            ProcessMining();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            ResetMining();
        }

        // TOMBOL B: Melempar Item
        if (Input.GetKeyDown(KeyCode.B))
        {
            DropItem();
        }
    }

    private void ProcessMining()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        if (Vector2.Distance(transform.position, mouseWorldPos) <= maxMineDistance)
        {
            Vector3Int cellPos = targetTilemap.WorldToCell(mouseWorldPos);
            if (targetTilemap.GetTile(cellPos) != null)
            {
                if (isMining && cellPos == currentMiningCell)
                {
                    mineTimer += Time.deltaTime;
                    if (mineTimer >= timeToMine) BreakBlock(cellPos);
                }
                else
                {
                    isMining = true;
                    currentMiningCell = cellPos;
                    mineTimer = 0f;
                }
            }
            else { ResetMining(); }
        }
        else { ResetMining(); }
    }

    private void BreakBlock(Vector3Int cellPosition)
    {
        TileBase minedTile = targetTilemap.GetTile(cellPosition);
        Vector3 spawnPos = targetTilemap.GetCellCenterWorld(cellPosition);

        GameObject prefabToDrop = null;

        foreach (BlockMapping mapping in blockMappings)
        {
            if (mapping.blockTile == minedTile)
            {
                prefabToDrop = mapping.dropPrefab;
                break;
            }
        }

        if (prefabToDrop != null)
        {
            Instantiate(prefabToDrop, spawnPos, Quaternion.identity);
        }

        targetTilemap.SetTile(cellPosition, null);
        ResetMining();
    }

    private void ResetMining()
    {
        isMining = false;
        mineTimer = 0f;
    }

    public bool AddItem(FloatingItem pickedItem)
    {
        if (blockCount >= 1) return false;

        blockCount++;
        currentHeldTile = pickedItem.itemTile;
        UpdateHeldVisual(pickedItem.itemSprite);

        return true;
    }

    // --- FUNGSI BARU: MELEMPAR ITEM ---
    private void DropItem()
    {
        if (blockCount <= 0) return;

        GameObject prefabToDrop = null;

        // Mencari Prefab yang sesuai dengan Tile yang sedang dipegang
        foreach (BlockMapping mapping in blockMappings)
        {
            if (mapping.blockTile == currentHeldTile)
            {
                prefabToDrop = mapping.dropPrefab;
                break;
            }
        }

        if (prefabToDrop != null)
        {
            float arahX = 1f;
            SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();

            if (playerSprite != null && playerSprite.flipX)
            {
                arahX = -1f;
            }
            else if (transform.localScale.x < 0)
            {
                arahX = -1f;
            }

            // ATUR JARAK 1 BLOK: Menggunakan 1.0f agar posisinya pas 1 unit/blok di depan player
            float jarakSatuBlok = 1.0f;
            Vector3 spawnPos = transform.position + new Vector3(arahX * jarakSatuBlok, 0.2f, 0);

            GameObject droppedItem = Instantiate(prefabToDrop, spawnPos, Quaternion.identity);

            Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Lemparan kecil / dorongan ringan agar jatuh mulus di jarak 1 blok
                Vector2 throwDirection = new Vector2(arahX, 0.5f).normalized;
                rb.AddForce(throwDirection * 4f, ForceMode2D.Impulse);
            }
        }

        // Kosongkan tas dan matikan visual di atas kepala
        blockCount = 0;
        currentHeldTile = null;
        UpdateHeldVisual(null);
    }

    private void UpdateHeldVisual(Sprite newSprite)
    {
        if (heldItemVisual != null)
        {
            if (blockCount > 0 && newSprite != null)
            {
                heldItemVisual.sprite = newSprite;
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
    }
}