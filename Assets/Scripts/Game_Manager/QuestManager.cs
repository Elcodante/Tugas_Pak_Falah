using UnityEngine;
using TMPro;

public class QuestManager : MonoBehaviour
{
    [Header("Referensi UI Misi")]
    public GameObject questPanel;
    public TextMeshProUGUI questText;
    public bool isQuestActive = false;
    public bool isReadyToReport = false;
    public bool isQuestFinishedCompletely = false;

    private int currentCrystal = 0;
    private int targetCrystal = 0;

    void Start()
    {
        if (questPanel != null) questPanel.SetActive(false);
    }

    public void GenerateNewQuest()
    {
        if (isQuestActive || isReadyToReport || isQuestFinishedCompletely) return;

        targetCrystal = Random.Range(5, 9);
        currentCrystal = 0;
        isQuestActive = true;

        UpdateUI();
        questPanel.SetActive(true);
    }

    public void AddProgress()
    {
        if (!isQuestActive) return;

        currentCrystal++;
        UpdateUI();

        if (currentCrystal >= targetCrystal)
        {
            isQuestActive = false;
            isReadyToReport = true; // Ubah status jadi siap lapor
            questText.text = "Lapor NPC!";
        }
    }

    public void ReportQuest()
    {
        isReadyToReport = false;
        isQuestFinishedCompletely = true; // Misi tamat permanen
        questPanel.SetActive(false); // Matikan UI Misi
    }

    private void UpdateUI()
    {
        if (questText != null)
        {
            questText.text = currentCrystal + " / " + targetCrystal + " :";
        }
    }
}