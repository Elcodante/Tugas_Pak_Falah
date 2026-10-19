using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D), typeof(NetworkObject))]
public class FloatingItem : NetworkBehaviour
{
    [Header("Identitas Item")]
    public Sprite itemSprite;
    public TileBase itemTile;

    [Header("Pengaturan Animasi Mengambang")]
    public float amplitude = 0.2f;
    public float speed = 2f;

    private Vector3 floatStartPos;
    private bool isBeingPickedUp = false;
    private Transform playerTarget;
    private ulong targetClientId = ulong.MaxValue;

    private bool isClaimedOnServer = false;
    private bool hasAddedToInventory = false;

    private Rigidbody2D rb;
    private float spawnTime;

    // --- KUNCI PERBAIKAN ---
    // isFalling diubah menjadi NetworkVariable agar semua layar tahu kapan item ini mendarat
    public NetworkVariable<bool> isFallingNet = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server // Hanya Server yang mendeteksi tabrakan dengan tanah
    );

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnTime = Time.time;

        // Pastikan hanya Server yang menggunakan gravitasi fisik asli
        if (IsServer)
        {
            rb.gravityScale = 1f;
        }
    }

    public override void OnNetworkSpawn()
    {
        // 1. Jika pemain (Client) baru masuk ruangan dan item TERNYATA sudah mendarat sebelumnya
        if (!isFallingNet.Value && !IsServer)
        {
            floatStartPos = transform.position;
            GetComponent<Collider2D>().isTrigger = true;
        }

        // 2. Alarm jika Server mengabarkan item baru saja mendarat
        isFallingNet.OnValueChanged += (oldVal, newVal) => {
            if (newVal == false && !IsServer)
            {
                floatStartPos = transform.position;
                GetComponent<Collider2D>().isTrigger = true;
            }
        };
    }

    void Update()
    {
        if (isBeingPickedUp && playerTarget != null)
        {
            // Efek magnet
            transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, 8f * Time.deltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 15f * Time.deltaTime);

            if (Vector3.Distance(transform.position, playerTarget.position) < 0.2f)
            {
                // HANYA komputer milik pemenang yang memasukkan barang ke tasnya
                if (NetworkManager.Singleton.LocalClientId == targetClientId && !hasAddedToInventory)
                {
                    hasAddedToInventory = true;

                    InventoryManager inventory = playerTarget.GetComponent<InventoryManager>();
                    if (inventory != null)
                    {
                        inventory.AddItem(itemTile, itemSprite);
                    }

                    DespawnItemServerRpc();
                }
            }
        }
        else if (!isFallingNet.Value && !isBeingPickedUp)
        {
            // Melayang santai menggunakan titik awal yang sudah disinkronkan
            float newY = floatStartPos.y + Mathf.Sin(Time.time * speed) * amplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // PENGAMAN: Karena memakai NetworkRigidbody2D, HANYA SERVER yang boleh memproses benturan fisik!
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Ground") && isFallingNet.Value)
        {
            isFallingNet.Value = false; // Memicu OnValueChanged di semua Client

            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            floatStartPos = transform.position;
            GetComponent<Collider2D>().isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryInitiatePickup(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryInitiatePickup(collision);
    }

    private void TryInitiatePickup(Collider2D collision)
    {
        // PENGAMAN: Ganti isFalling biasa menjadi isFallingNet.Value
        if (!IsSpawned || isBeingPickedUp || isFallingNet.Value || (Time.time - spawnTime <= 0.5f)) return;

        if (collision.CompareTag("Player"))
        {
            NetworkObject playerNetObj = collision.GetComponentInParent<NetworkObject>();

            // Hanya proses jika yang menyentuh adalah karakter kita sendiri di komputer ini
            if (playerNetObj != null && playerNetObj.IsOwner)
            {
                InventoryManager inv = playerNetObj.GetComponent<InventoryManager>();

                // Cek apakah tas masih muat
                if (CanFitInInventory(inv))
                {
                    isBeingPickedUp = true;
                    RequestPickupServerRpc(playerNetObj.OwnerClientId);
                }
            }
        }
    }

    private bool CanFitInInventory(InventoryManager inv)
    {
        if (inv == null) return false;
        for (int i = 0; i < inv.slots.Length; i++)
        {
            if (inv.slots[i].IsEmpty()) return true;
            if (inv.slots[i].itemTile != null && itemTile != null &&
                inv.slots[i].itemTile.name == itemTile.name && !inv.slots[i].IsFull())
            {
                return true;
            }
        }
        return false;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPickupServerRpc(ulong clientId)
    {
        // Anti-Duplikasi Server
        if (isClaimedOnServer || !IsSpawned) return;

        isClaimedOnServer = true;
        StartMagnetClientRpc(clientId);
    }

    [Rpc(SendTo.Everyone)]
    private void StartMagnetClientRpc(ulong winnerClientId)
    {
        targetClientId = winnerClientId;
        isBeingPickedUp = true;

        InventoryManager[] allPlayers = FindObjectsByType<InventoryManager>(FindObjectsSortMode.None);
        foreach (InventoryManager playerInv in allPlayers)
        {
            if (playerInv.OwnerClientId == winnerClientId)
            {
                playerTarget = playerInv.transform;
                break;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DespawnItemServerRpc()
    {
        if (IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}