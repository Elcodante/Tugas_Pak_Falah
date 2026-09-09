using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Pengaturan Scene")]
    [Tooltip("Ketik nama scene tujuan Anda di sini (Pastikan scene sudah masuk ke Build Settings)")]
    public string namaSceneTujuan;

    // Fungsi ini dipanggil saat tombol diklik
    public void PindahScene()
    {
        if (!string.IsNullOrEmpty(namaSceneTujuan))
        {
            SceneManager.LoadScene(namaSceneTujuan);
        }
        else
        {
            Debug.LogWarning("Peringatan: Nama scene tujuan masih kosong! Silakan isi di Inspector.");
        }
    }
}
