using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class BlockMapping
{
    public TileBase blockTile;      // Gambar blok yang ada di tanah (Tilemap)
    public GameObject dropPrefab;   // Prefab item mengambang yang akan jatuh
}

public class MiningController : MonoBehaviour
{
    [Header("Pengaturan Mining")]
    public float maxMineDistance = 1.5f;
    public Tilemap targetTilemap;
    public float timeToMine = 1.0f;

    [Header("Daftar Drop Item")]
    public BlockMapping[] blockMappings;

    [Tooltip("Pengaturan Placing")]
    public float MaxPlaceDistance = 2.0f;

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
        if (Input.GetMouseButton(1)) ProcessMining();
        else if (Input.GetMouseButtonUp(1)) ResetMining();

        if (Input.GetMouseButtonDown(0)) ProcessPlacing();
    }

    private void ProcessPlacing()
    {
        if (blockCount <= 0 || currentHeldTile == null) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        float distance = Vector2.Distance(transform.position, mouseWorldPos);

        if (distance <= MaxPlaceDistance)
        {
            Vector3Int cellPosition = targetTilemap.WorldToCell(mouseWorldPos);

            if (targetTilemap.GetTile(cellPosition) == null)
            {
                Vector3 cellCenter = targetTilemap.GetCellCenterWorld(cellPosition);
                Collider2D hit = Physics2D.OverlapBox(cellCenter, new Vector2(0.9f, 0.9f), 0f);

                if (hit != null && hit.CompareTag("Player")) return;

                targetTilemap.SetTile(cellPosition, currentHeldTile);
                blockCount--;
                UpdateHeldVisual(null);
            }
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
        else
        {
            Debug.LogWarning("Blok dihancurkan, tapi tidak ada drop item yang didaftarkan untuk blok ini!");
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