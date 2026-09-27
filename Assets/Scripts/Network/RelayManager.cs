using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;

    [Header("UI Panels")]
    public GameObject panelLobbySelection;
    public GameObject panelHostRoom;
    public GameObject panelJoinRoom;

    [Header("UI Host Room")]
    public TextMeshProUGUI textRoomID;
    public TextMeshProUGUI[] textPlayerNames; // Array untuk 4 teks nama pemain

    [Header("UI Lobby")]
    public TMP_InputField inputNickname;

    [Header("UI Join Room")]
    public TMP_InputField inputJoinCode;

    public GameObject textWarningName;

    private int currentPlayerCount = 0;

    private void Awake()
    {
        Instance = this;
    }

    private async void Start()
    {
        if (textWarningName != null)
        {
            textWarningName.SetActive(false); // Sembunyikan peringatan nama kosong saat game dimulai
        }
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Login Unity Services berhasil.");
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // UBAH DI SINI: Sembunyikan semua panel jaringan saat game baru dimulai
        panelLobbySelection.SetActive(false);
        panelHostRoom.SetActive(false);
        panelJoinRoom.SetActive(false);
    }

    // TAMBAHKAN FUNGSI BARU INI:
    public void Button_OpenLobbySelection()
    {
        ShowPanel(panelLobbySelection);
    }

    // --- FUNGSI NAVIGASI UI ---
    public void Button_OpenHostPanel()
    {
        if (string.IsNullOrWhiteSpace(inputNickname.text))
        {
            Debug.LogWarning("GAGAL: Nickname harus diisi terlebih dahulu!");
            textWarningName.SetActive(true); // Tampilkan peringatan nama kosong
            return; 
        }

        SavePlayerData();

        CreateRelay();
        ShowPanel(panelHostRoom);
    }

    public void Button_OpenJoinPanel()
    {
        if (string.IsNullOrWhiteSpace(inputNickname.text))
        {
            Debug.LogWarning("GAGAL: Nickname harus diisi terlebih dahulu!");
            textWarningName.SetActive(true); // Tampilkan peringatan nama kosong
            return; 
        }

        ShowPanel(panelJoinRoom);
    }

    public void Button_ConnectAsClient()
    {
        SavePlayerData();
        JoinRelay(inputJoinCode.text);
    }

    private void ShowPanel(GameObject panelToShow)
    {
        panelLobbySelection.SetActive(false);
        panelHostRoom.SetActive(false);
        panelJoinRoom.SetActive(false);
        panelToShow.SetActive(true);
    }

    // --- LOGIKA JARINGAN RELAY ---
    // --- LOGIKA JARINGAN RELAY ---
    private async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3); // 3 Tamu + 1 Host = 4 Player
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            textRoomID.text = "Room ID: " + joinCode; // Tampilkan kode di UI Host

            // PERBAIKAN: Menggunakan extension method SetRelayServerData dari UTP secara langsung
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Gagal Host: " + e);
        }
    }

    private async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // PERBAIKAN: Menggunakan extension method SetRelayServerData dari UTP secara langsung
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            ShowPanel(panelHostRoom); // Pindah ke panel Host (ruang tunggu) setelah connect
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Gagal Join: " + e);
        }
    }

    // --- LOGIKA UPDATE NAMA UI ---
    public void AddPlayerToUI(ulong clientId)
    {
        if (currentPlayerCount < textPlayerNames.Length)
        {
            textPlayerNames[currentPlayerCount].text = (clientId == 0) ? "Player 1 (Host)" : "Player " + (clientId + 1);
            currentPlayerCount++;
        }
    }

    public void Button_StartMultiplayerGame()
    {
        if(NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("In_Game", LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("Hanya host yang dapat memulai permainan.");
        }
    }

    private void SavePlayerData()
    {
        string playerName = string.IsNullOrEmpty(inputNickname.text) ? "Player" : inputNickname.text;
        PlayerPrefs.SetString("PlayerNickname", playerName);
        PlayerPrefs.SetInt("IsMultiplayer", 1); // Penanda mode multiplayer
        PlayerPrefs.Save();
    }

    public void Button_StartSinglePlayer()
    {
        PlayerPrefs.SetInt("IsMultiplayer", 0); // Penanda mode single-player
        PlayerPrefs.Save();
        // Masukkan logika load scene single player milikmu di sini...
    }
}