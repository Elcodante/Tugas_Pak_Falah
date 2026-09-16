using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Referensi Player")]
    // Ubah dari MonoBehaviour menjadi tipe spesifik MovementController
    public MovementController movementController;

    private Rigidbody2D playerRb;

    private string[] currentLines;
    private int currentLineIndex;
    private bool isDialogueActive = false;

    private void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Cari otomatis JIKA kolom di Inspector masih kosong
        if (movementController == null)
        {
            // Gunakan tipe MovementController secara eksplisit
            movementController = FindFirstObjectByType<MovementController>();
        }

        // Pastikan kita selalu mengambil Rigidbody2D, baik dicari otomatis maupun diisi dari Inspector
        if (movementController != null)
        {
            playerRb = movementController.GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        if (isDialogueActive && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            DisplayNextSentence();
        }
    }

    public void StartDialogue(string[] lines)
    {
        currentLines = lines;
        currentLineIndex = 0;
        isDialogueActive = true;

        dialoguePanel.SetActive(true);

        if (movementController != null)
        {
            movementController.enabled = false;
        }

        // Karena playerRb sekarang pasti terisi, kecepatan akan sukses di-nol-kan
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }

        DisplayNextSentence();
    }

    private void DisplayNextSentence()
    {
        if (currentLineIndex < currentLines.Length)
        {
            dialogueText.text = currentLines[currentLineIndex];
            currentLineIndex++;
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        if (movementController != null)
        {
            movementController.enabled = true;
        }
    }
}