using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    [Header("Referensi Dialogue")]
    public string[] dialogueLines; // Dialog awal untuk memberi misi

    [Header("Dialog Penutup (Opsional)")]
    public string[] dialogueSelesai; // Dialog saat player lapor misi selesai

    public DialogueManager dialogueManager;
    private bool isPlayerNearby = false;
    public GameObject UIIndikator;

    private QuestManager questManager;

    void Start()
    {
        UIIndikator.SetActive(false);
        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        }

        // Cari otomatis QuestManager di game
        questManager = FindFirstObjectByType<QuestManager>();
    }

    private void Update()
    {
        // GEMBOK PERMANEN: Kalau misi udah tamat, matikan interaksi total
        if (questManager != null && questManager.isQuestFinishedCompletely)
        {
            UIIndikator.SetActive(false);
            return;
        }

        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (questManager != null)
            {
                // SKENARIO 2: Player kembali untuk lapor misi
                if (questManager.isReadyToReport)
                {
                    questManager.ReportQuest(); // Hilangkan UI di kanan atas

                    // Mainkan dialog penutup jika kamu mengisinya di Inspector
                    if (dialogueSelesai != null && dialogueSelesai.Length > 0)
                    {
                        dialogueManager.StartDialogue(dialogueSelesai);
                    }
                    return;
                }

                // SKENARIO 3: Player ngajak ngobrol saat misi belum beres (kristal kurang)
                if (questManager.isQuestActive)
                {
                    // Bebas, untuk sekarang kita diamkan saja (tidak ada interaksi)
                    return;
                }
            }

            // SKENARIO 1: Belum ada misi sama sekali, mulai percakapan awal
            dialogueManager.StartDialogue(dialogueLines);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Jangan munculkan indikator tombol 'E' kalau misi sudah tamat
            if (questManager != null && questManager.isQuestFinishedCompletely) return;

            isPlayerNearby = true;
            UIIndikator.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = false;
            UIIndikator.SetActive(false);
        }
    }
}