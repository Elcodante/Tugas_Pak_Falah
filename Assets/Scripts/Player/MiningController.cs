using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;
using UnityEngine.SceneManagement; // [TAMBAHKAN] Library untuk mendeteksi scene

[System.Serializable]
public class BlockMapping
{
    public TileBase blockTile;
    public GameObject dropPrefab;
}

public class MiningController : NetworkBehaviour
{
    [Header("Pengaturan Mining")]
    public float maxMineDistance = 1.5f;
    public float timeToMine = 3.0f;

    [Header("Daftar Drop Item")]
    public BlockMapping[] blockMappings;

    [Header("Nama Objek Tilemap di Scene")]
    [Tooltip("Ketik persis nama GameObject Tilemap yang ada di Hierarchy (contoh: Mining)")]
    public string tilemapName = "Mining";

    private Tilemap targetTilemap;
    private float mineTimer = 0f;
    private bool isMining = false;
    private Vector3Int currentMiningCell;
    private MovementController moveControl;
    private Animator anim;

    private void Awake()
    {
        moveControl = GetComponent<MovementController>();
        anim = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        FindTilemapAutomatically();

        // Daftarkan alarm saat layar berpindah Scene
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnNetworkDespawn()
    {
        // Matikan alarm jika pemain keluar game
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Jika masuk ke scene permainan (bukan Main Menu index 0), cari Tilemap lagi!
        if (scene.buildIndex != 0)
        {
            FindTilemapAutomatically();
        }
    }

    private void FindTilemapAutomatically()
    {
        GameObject tilemapObj = GameObject.Find(tilemapName);
        if (tilemapObj != null)
        {
            targetTilemap = tilemapObj.GetComponent<Tilemap>();
            Debug.Log($"[Mining] Tilemap '{tilemapName}' berhasil terhubung ke Player {OwnerClientId}!");
        }
        else
        {
            // Jangan beri peringatan jika sedang di Main Menu
            if (SceneManager.GetActiveScene().buildIndex != 0)
            {
                Debug.LogError($"[Mining] Gagal! Tidak ada objek bernama '{tilemapName}' di Hierarchy Scene.");
            }
        }
    }

    private void Update()
    {
        if (!IsOwner || targetTilemap == null) return;

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
                    if (mineTimer >= timeToMine)
                    {
                        MineBlockServerRpc(cellPos);
                        ResetMining();
                    }
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

    // --- LOGIKA SERVER ---
    [Rpc(SendTo.Server)]
    private void MineBlockServerRpc(Vector3Int cellPosition, RpcParams rpcParams = default)
    {
        if (targetTilemap == null) return;

        TileBase minedTile = targetTilemap.GetTile(cellPosition);
        if (minedTile == null) return;

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
            GameObject droppedItem = Instantiate(prefabToDrop, spawnPos, Quaternion.identity);

            NetworkObject netObj = droppedItem.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
            else
            {
                Debug.LogWarning("[Mining] GAGAL SPAWN ITEM: Prefab Drop Item belum dipasangi komponen 'NetworkObject'!");
            }
        }

        ulong minerClientId = rpcParams.Receive.SenderClientId;
        BreakBlockRpc(cellPosition, minerClientId);
    }

    // --- LOGIKA CLIENT (Semua Layar) ---
    [Rpc(SendTo.Everyone)]
    private void BreakBlockRpc(Vector3Int cellPosition, ulong minerClientId)
    {
        if (targetTilemap != null)
        {
            targetTilemap.SetTile(cellPosition, null);
        }

        if (NetworkManager.Singleton.LocalClientId == minerClientId)
        {
            QuestManager quest = FindFirstObjectByType<QuestManager>();
            if (quest != null)
            {
                quest.AddProgress();
            }
        }
    }

    private void ResetMining()
    {
        isMining = false;
        mineTimer = 0f;
    }
}