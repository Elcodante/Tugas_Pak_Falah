using UnityEngine;
using TMPro;
using Unity.Netcode;

public class QuestManager : NetworkBehaviour
{
    [Header("Referensi UI Misi")]
    public GameObject questPanel;
    public TextMeshProUGUI questText;

    [Header("Pengaturan Item Misi")]
    [Tooltip("Masukkan GAMBAR (Sprite) kristal yang ada di UI Inventory, BUKAN TileBase!")]
    public Sprite questItemSprite; // PERBAIKAN: Berubah jadi Sprite

    // --- STATUS MISI BERSAMA (TERSINKRONISASI) ---
    public NetworkVariable<bool> isQuestActive = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isReadyToReport = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isQuestFinishedCompletely = new NetworkVariable<bool>(false);

    public NetworkVariable<int> currentCrystal = new NetworkVariable<int>(0);
    public NetworkVariable<int> targetCrystal = new NetworkVariable<int>(0);

    // Variabel internal untuk penarikan item berantai
    private int remainingToDeduct = 0;
    private ulong[] clientIds;
    private int currentClientIndex = 0;

    void Start()
    {
        if (questPanel != null) questPanel.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        currentCrystal.OnValueChanged += (oldVal, newVal) => UpdateUI();
        targetCrystal.OnValueChanged += (oldVal, newVal) => UpdateUI();
        isReadyToReport.OnValueChanged += (oldVal, newVal) => UpdateUI();

        isQuestActive.OnValueChanged += (oldVal, newVal) => HandlePanelVisibility();
        isQuestFinishedCompletely.OnValueChanged += (oldVal, newVal) => HandlePanelVisibility();

        HandlePanelVisibility();
        UpdateUI();
    }

    private void HandlePanelVisibility()
    {
        if (isQuestFinishedCompletely.Value)
        {
            questPanel.SetActive(false);
        }
        else if (isQuestActive.Value || isReadyToReport.Value)
        {
            questPanel.SetActive(true);
            UpdateUI();
        }
    }

    public void GenerateNewQuest()
    {
        GenerateNewQuestRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void GenerateNewQuestRpc()
    {
        if (isQuestActive.Value || isReadyToReport.Value || isQuestFinishedCompletely.Value) return;

        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
        int baseTarget = Random.Range(5, 9);

        targetCrystal.Value = baseTarget * playerCount;
        currentCrystal.Value = 0;
        isQuestActive.Value = true;
    }

    public void AddProgress()
    {
        AddProgressRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void AddProgressRpc()
    {
        if (!isQuestActive.Value) return;

        currentCrystal.Value++;

        if (currentCrystal.Value >= targetCrystal.Value)
        {
            isQuestActive.Value = false;
            isReadyToReport.Value = true;
        }
    }

    // ==========================================
    // SISTEM PENARIKAN INVENTORI BERANTAI
    // ==========================================
    public void ReportQuest()
    {
        ReportQuestRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportQuestRpc()
    {
        if (isReadyToReport.Value)
        {
            isReadyToReport.Value = false; // Kunci agar NPC tidak diklik 2x

            remainingToDeduct = targetCrystal.Value;

            int count = NetworkManager.Singleton.ConnectedClientsIds.Count;
            clientIds = new ulong[count];
            for (int i = 0; i < count; i++)
            {
                clientIds[i] = NetworkManager.Singleton.ConnectedClientsIds[i];
            }

            currentClientIndex = 0;
            AskNextClientToDeduct(); // Mulai menagih item ke pemain pertama
        }
    }

    private void AskNextClientToDeduct()
    {
        // Jika tagihan sudah lunas (<=0) atau semua pemain sudah ditagih, akhiri misi
        if (remainingToDeduct <= 0 || currentClientIndex >= clientIds.Length)
        {
            isQuestFinishedCompletely.Value = true;
            return;
        }

        ulong nextClient = clientIds[currentClientIndex];
        currentClientIndex++;

        DeductItemClientRpc(nextClient, remainingToDeduct);
    }

    [Rpc(SendTo.Everyone)]
    private void DeductItemClientRpc(ulong targetClientId, int amountNeeded)
    {
        // PENGAMAN: Jika ini bukan komputer milik pemain yang dipanggil, batalkan!
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        int deducted = 0;

        InventoryManager[] allInvs = FindObjectsByType<InventoryManager>(FindObjectsSortMode.None);
        foreach (InventoryManager inv in allInvs)
        {
            if (inv.IsOwner)
            {
                deducted = inv.RemoveItem(questItemSprite, amountNeeded);
                Debug.Log($"[Quest] Berhasil menarik {deducted} Kristal dari Player {targetClientId}");
                break;
            }
        }

        // Lapor balik ke Server berapa banyak yang berhasil diambil dari tas ini
        ReplyDeductionServerRpc(deducted);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReplyDeductionServerRpc(int amountDeducted)
    {
        remainingToDeduct -= amountDeducted; // Kurangi tagihan total
        AskNextClientToDeduct(); // Lanjut tagih pemain berikutnya
    }

    private void UpdateUI()
    {
        if (questText != null)
        {
            if (isReadyToReport.Value)
            {
                questText.text = "Lapor NPC!";
            }
            else if (isQuestActive.Value)
            {
                questText.text = currentCrystal.Value + " / " + targetCrystal.Value + " :";
            }
        }
    }
}