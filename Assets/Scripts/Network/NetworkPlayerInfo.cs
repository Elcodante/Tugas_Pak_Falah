using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class NetworkPlayerInfo : NetworkBehaviour
{
    [Header("UI References")]
    public GameObject nameCanvas;
    public TextMeshProUGUI nameText;

    public NetworkVariable<FixedString32Bytes> networkPlayerName = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // --- REFERENSI KOMPONEN UNTUK DIMATIKAN DI LOBBY ---
    private Rigidbody2D rb;
    private MovementController movementController;
    private SpriteRenderer[] allSprites;

    private void Awake()
    {
        // Ambil komponen-komponen fisik dan gambar dari karakter ini
        rb = GetComponent<Rigidbody2D>();
        movementController = GetComponent<MovementController>();

        // GetComponentsInChildren mengambil sprite tubuh utama dan semua anak (seperti item/senjata)
        allSprites = GetComponentsInChildren<SpriteRenderer>();

        networkPlayerName.OnValueChanged += (oldValue, newValue) =>
        {
            nameText.text = newValue.ToString();
        };
    }

    public override void OnNetworkSpawn()
    {
        if (PlayerPrefs.GetInt("IsMultiplayer", 0) == 0)
        {
            nameCanvas.SetActive(false);
        }
        else
        {
            if (IsOwner)
            {
                string myName = PlayerPrefs.GetString("PlayerNickname", "Guest");
                SubmitNameServerRpc(myName);
            }
            else
            {
                nameText.text = networkPlayerName.Value.ToString();
            }

            if (RelayManager.Instance != null)
            {
                RelayManager.Instance.AddPlayerToUI(OwnerClientId);
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        // [BARU] Cek apakah kita sedang berada di Main Menu (Asumsi Main Menu berada di Build Index 0)
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            SetPlayerState(false); // Tidurkan fisik dan visual karakter
        }
        else
        {
            SetPlayerState(true);  // Bangunkan karakter
            TryTeleportToSpawnPoint();
        }
    }

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Jika scene yang baru dimuat BUKAN Main Menu (Masuk ke goa)
        if (scene.buildIndex != 0)
        {
            SetPlayerState(true); // Bangunkan karakter
            TryTeleportToSpawnPoint();
        }
    }

    private void TryTeleportToSpawnPoint()
    {
        if (SpawnManager.Instance != null)
        {
            transform.position = SpawnManager.Instance.GetSpawnPosition(OwnerClientId);
        }
    }

    // --- FUNGSI UNTUK MENIDURKAN / MEMBANGUNKAN KARAKTER ---
    private void SetPlayerState(bool isActive)
    {
        // 1. rb.simulated = false akan mematikan efek gravitasi dan benturan seketika
        if (rb != null) rb.simulated = isActive;

        // 2. Matikan kontrol input
        if (movementController != null) movementController.enabled = isActive;

        // 3. Sembunyikan semua gambar karakter
        foreach (SpriteRenderer sr in allSprites)
        {
            sr.enabled = isActive;
        }

        // 4. Sembunyikan canvas nama saat di lobby, tampilkan saat main
        if (PlayerPrefs.GetInt("IsMultiplayer", 0) == 1)
        {
            nameCanvas.SetActive(isActive);
        }
    }

    [ServerRpc]
    private void SubmitNameServerRpc(string newName, ServerRpcParams rpcParams = default)
    {
        networkPlayerName.Value = new FixedString32Bytes(newName);
    }
}