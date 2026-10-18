using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode; // Wajib untuk sinkronisasi jaringan

[RequireComponent(typeof(Rigidbody2D), typeof(NetworkObject))]
public class FloatingItem : NetworkBehaviour // UBAH KE NetworkBehaviour
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

    // Gembok pengaman agar tidak diambil 2 kali
    private bool isClaimedOnServer = false;
    private bool hasAddedToInventory = false;

    private Rigidbody2D rb;
    private bool isFalling = true;
    private float spawnTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        isFalling = true;
        spawnTime = Time.time;
    }

    void Update()
    {
        if (isBeingPickedUp && playerTarget != null)
        {
            // Efek magnet menyedot ke arah player pemenang di semua layar
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

                    // Minta Server menghancurkan objek ini secara resmi dari jaringan
                    DespawnItemServerRpc();
                }
            }
        }
        else if (!isFalling && !isBeingPickedUp)
        {
            float newY = floatStartPos.y + Mathf.Sin(Time.time * speed) * amplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") && isFalling)
        {
            isFalling = false;
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

    // Tambahan OnTriggerStay2D agar jika player sudah berdiri di atas item sejak baru jatuh,
    // item langsung tersedot begitu jeda 0.5 detik selesai tanpa harus jalan keluar-masuk lagi.
    private void OnTriggerStay2D(Collider2D collision)
    {
        TryInitiatePickup(collision);
    }

    private void TryInitiatePickup(Collider2D collision)
    {
        if (!IsSpawned || isBeingPickedUp || isFalling || (Time.time - spawnTime <= 0.5f)) return;

        if (collision.CompareTag("Player"))
        {
            NetworkObject playerNetObj = collision.GetComponentInParent<NetworkObject>();

            // KUNCI 1: Hanya proses jika yang menyentuh adalah karakter milik KITA SENDIRI di layar ini
            if (playerNetObj != null && playerNetObj.IsOwner)
            {
                InventoryManager inv = playerNetObj.GetComponent<InventoryManager>();

                // Cek dulu apakah tas masih muat sebelum meminta izin ke Server
                if (CanFitInInventory(inv))
                {
                    isBeingPickedUp = true; // Kunci lokal sementara menunggu balasan Server
                    RequestPickupServerRpc(playerNetObj.OwnerClientId);
                }
            }
        }
    }

    // Mengecek apakah tas punya ruang kosong atau tumpukan yang belum penuh
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

    // --- LOGIKA SERVER (ANTI-DUPLIKASI) ---
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPickupServerRpc(ulong clientId)
    {
        // KUNCI 2: Jika sudah diklaim pemain lain sepersekian detik sebelumnya, tolak!
        if (isClaimedOnServer || !IsSpawned) return;

        isClaimedOnServer = true;

        // Umumkan ke semua layar bahwa item ini resmi diambil oleh clientId tersebut
        StartMagnetClientRpc(clientId);
    }

    [Rpc(SendTo.Everyone)]
    private void StartMagnetClientRpc(ulong winnerClientId)
    {
        targetClientId = winnerClientId;
        isBeingPickedUp = true;

        // Cari objek karakter pemenang di layar masing-masing agar efek sedotan terlihat oleh semua orang
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
            // KUNCI 3: Server melakukan Despawn dan Destroy secara resmi (Bebas Error Invalid Destroy!)
            NetworkObject.Despawn(true);
        }
    }
}