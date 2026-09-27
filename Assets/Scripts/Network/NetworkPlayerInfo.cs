using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class NetworkPlayerInfo : NetworkBehaviour
{
    [Header("UI References")]
    public GameObject nameCanvas;       // Tarik Canvas World Space ke sini
    public TextMeshProUGUI nameText;    // Tarik Text-TMP nama ke sini

    // Variabel jaringan khusus string. Hanya server yang boleh mengubahnya, tapi semua boleh membaca.
    public NetworkVariable<FixedString32Bytes> networkPlayerName = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        // Event ini otomatis dipanggil KETIKA variabel jaringan berubah di server
        networkPlayerName.OnValueChanged += (oldValue, newValue) =>
        {
            nameText.text = newValue.ToString();
        };
    }

    public override void OnNetworkSpawn()
    {
        // 1. Pengecekan Single-player
        if (PlayerPrefs.GetInt("IsMultiplayer", 0) == 0)
        {
            nameCanvas.SetActive(false); // Sembunyikan UI Nickname jika Single-player
            return;
        }

        // 2. Jika ini adalah karakter milik kita (player kita sendiri)
        if (IsOwner)
        {
            // Ambil nama dari PlayerPrefs yang disimpan di Main Menu
            string myName = PlayerPrefs.GetString("PlayerNickname", "Guest");

            // Perintahkan Server untuk memperbarui nama kita di jaringan
            SubmitNameServerRpc(myName);
        }
        else
        {
            // Untuk karakter pemain lain yang baru muncul di layar kita, 
            // pastikan teksnya langsung membaca nilai dari jaringan
            nameText.text = networkPlayerName.Value.ToString();
        }

        // --- Logika Update Nama di Panel Host Room ---
        if (RelayManager.Instance != null)
        {
            RelayManager.Instance.AddPlayerToUI(OwnerClientId);
        }
    }

    // Fungsi ini hanya dieksekusi di sisi Server (Host)
    [ServerRpc]
    private void SubmitNameServerRpc(string newName, ServerRpcParams rpcParams = default)
    {
        networkPlayerName.Value = new FixedString32Bytes(newName);
    }
}