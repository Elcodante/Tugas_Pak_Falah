using UnityEngine;
using UnityEngine.Tilemaps;

public class MiningController : MonoBehaviour
{
    [Header("Pengaturan Mining")]
    public float maxMineDistance = 1.5f;
    public Tilemap targetTilemap;

    [Tooltip("Waktu yang dibutuhkan untuk menghancurkan blok (dalam detik)")]
    public float timeToMine = 1.0f;

    [Header("Pengaturan Drop Item")]
    public GameObject droppedItemPrefab;

    private float mineTimer = 0f;
    private bool isMining = false;
    private Vector3Int currentMiningCell; // Mengingat blok mana yang sedang digali

    private void Update()
    {
        // GetMouseButton(1) akan terus berjalan SELAMA tombol kanan ditahan
        if (Input.GetMouseButton(1))
        {
            ProcessMining();
        }
        // Jika tombol dilepas, reset semua proses penambangan
        else if (Input.GetMouseButtonUp(1))
        {
            ResetMining();
        }
    }

    private void ProcessMining()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.nearClipPlane;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        float distance = Vector2.Distance(transform.position, mouseWorldPos);

        if (distance <= maxMineDistance)
        {
            Vector3Int cellPosition = targetTilemap.WorldToCell(mouseWorldPos);
            TileBase targetTile = targetTilemap.GetTile(cellPosition);

            if (targetTile != null)
            {
                if (isMining && cellPosition == currentMiningCell)
                {
                    mineTimer += Time.deltaTime;
                    if (mineTimer >= timeToMine)
                    {
                        BreakBlock(cellPosition);
                    }
                }
                else
                {
                    isMining = true;
                    currentMiningCell = cellPosition;
                    mineTimer = 0f;
                }
            }
            else
            {
                ResetMining();
            }
        }
        else
        {
            ResetMining(); 
        }
    }

    private void BreakBlock(Vector3Int cellPosition)
    {
        Vector3 spawnPosition = targetTilemap.GetCellCenterWorld(cellPosition);

        if (droppedItemPrefab != null)
        {
            Instantiate(droppedItemPrefab, spawnPosition, Quaternion.identity);
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