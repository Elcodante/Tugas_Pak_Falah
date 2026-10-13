using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    // Pola Singleton agar Player bisa dengan mudah menemukan Canvas ini
    public static InventoryUIManager Instance;

    [Header("Referensi UI (Posisi & Bingkai)")]
    public GameObject mainInventoryPanel;
    public RectTransform highlightBox;
    public RectTransform[] hotbarUI;

    [Header("Referensi Visual (Ikon & Angka)")]
    public Image[] slotIcons;
    public TMP_Text[] slotTexts;

    [Header("Sistem Drag & Drop")]
    public GameObject dragIconObject;

    private void Awake()
    {
        Instance = this;

        // Pastikan panel tertutup di awal
        if (mainInventoryPanel != null) mainInventoryPanel.SetActive(false);
        if (dragIconObject != null) dragIconObject.SetActive(false);
    }
}