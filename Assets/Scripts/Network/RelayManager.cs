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
    public TextMeshProUGUI[] textPlayerNames;

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
        if (textWarningName != null) textWarningName.SetActive(false);

        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Login Unity Services berhasil.");
        };

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        panelLobbySelection.SetActive(false);
        panelHostRoom.SetActive(false);
        panelJoinRoom.SetActive(false);
    }

    public void Button_OpenLobbySelection()
    {
        ShowPanel(panelLobbySelection);
    }

    public void Button_OpenHostPanel()
    {
        if (string.IsNullOrWhiteSpace(inputNickname.text))
        {
            Debug.LogWarning("GAGAL: Nickname harus diisi terlebih dahulu!");
            textWarningName.SetActive(true);
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
            textWarningName.SetActive(true);
            return;
        }

        ShowPanel(panelJoinRoom);
    }

    public void Button_ConnectAsClient()
    {
        string joinCodeInput = inputJoinCode.text.Trim();
        if (string.IsNullOrEmpty(joinCodeInput) || joinCodeInput.Length < 6)
        {
            Debug.LogError("Gagal: Kode ruangan tidak valid! Harus minimal 6 karakter.");
            return;
        }

        SavePlayerData();
        JoinRelay(joinCodeInput);
    }

    private void ShowPanel(GameObject panelToShow)
    {
        panelLobbySelection.SetActive(false);
        panelHostRoom.SetActive(false);
        panelJoinRoom.SetActive(false);
        panelToShow.SetActive(true);
    }

    private async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            textRoomID.text = "Room ID: " + joinCode;

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
            Debug.LogError("Gagal Host: " + e.Message);
            ShutdownRelaySafely();
        }
    }

    private async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Mencoba bergabung dengan kode: " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            bool isSuccess = NetworkManager.Singleton.StartClient();

            if (isSuccess)
            {
                ShowPanel(panelHostRoom);
            }
            else
            {
                Debug.LogError("Gagal menyalakan mesin Client!");
                ShutdownRelaySafely();
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Room tidak ditemukan atau ID salah! Pesan error: " + e.Message);
            ShutdownRelaySafely();
        }
    }

    // --- FUNGSI PEMBERSIH KONEKSI (MENGATASI ERROR BURST COMPILER) ---
    private void ShutdownRelaySafely()
    {
        if (NetworkManager.Singleton != null)
        {
            // Jika status terhubung, putus koneksi dulu agar tidak meninggalkan jejak hantu
            if (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsServer)
            {
                // DisconnectClient memutus secara lembut
                NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
            }

            // Tunggu sedikit sebelum mematikan mesin total untuk mencegah Burst error
            Invoke(nameof(ForceShutdownNetwork), 0.2f);
        }
    }

    private void ForceShutdownNetwork()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    public void Button_KeluarKeMainMenu()
    {
        ShutdownRelaySafely();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Refresh scene saat ini (Main_Menu)
    }

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
        if (NetworkManager.Singleton.IsServer)
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
        PlayerPrefs.SetInt("IsMultiplayer", 1);
        PlayerPrefs.Save();
    }

    public void Button_StartSinglePlayer()
    {
        PlayerPrefs.SetInt("IsMultiplayer", 0);
        PlayerPrefs.Save();

        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            bool isHostStarted = NetworkManager.Singleton.StartHost();

            if (!isHostStarted)
            {
                Debug.LogError("Gagal memulai Host Lokal. Port 7777 sedang dipakai oleh proses lain.");
                return;
            }
        }

        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("In_Game", LoadSceneMode.Single);
        }
    }
}