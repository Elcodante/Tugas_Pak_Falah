using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    [Header("Referensi Dialogue")]
    public string[] dialogueLines;

    public DialogueManager dialogueManager;
    private bool isPlayerNearby = false;
    public GameObject UIIndikator;

    void Start()
    {
        UIIndikator.SetActive(false);
        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        }
    }
    private void Update()
    {
        if(isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            dialogueManager.StartDialogue(dialogueLines);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            isPlayerNearby = true;
            UIIndikator.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            isPlayerNearby = false;
            UIIndikator.SetActive(false);
        }
    }
}
