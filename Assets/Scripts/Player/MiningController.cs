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
    public float timeToMine = 3.0f;

    [Header("Daftar Drop Item")]
    public BlockMapping[] blockMappings;

    private float mineTimer = 0f;
    private bool isMining = false;
    private Vector3Int currentMiningCell;
    private MovementController moveControl;

    // REFERENSI ANIMATOR
    private Animator anim;

    private void Start()
    {
        moveControl = GetComponent<MovementController>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (moveControl != null && moveControl.isHoldingItem)
        {
            if (isMining) ResetMining();
            return;
        }

        if (Input.GetMouseButton(1))
        {
            ProcessMining();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            ResetMining();
        }

        if (anim != null)
        {
            anim.SetBool("isMining", isMining);
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
            QuestManager quest = FindFirstObjectByType<QuestManager>();
            if (quest != null)
            {
                quest.AddProgress();
            }
        }

        targetTilemap.SetTile(cellPosition, null);
        ResetMining();
    }

    private void ResetMining()
    {
        isMining = false;
        mineTimer = 0f;
    }
}