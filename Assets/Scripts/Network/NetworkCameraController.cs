using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine; // Catatan: Ubah ke "using Cinemachine;" jika menggunakan versi lama
public class NetworkCameraController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // PENTING: Hanya jalankan ini jika karakter tersebut milik pemain lokal
        if (IsOwner)
        {
            // Coba cari kamera saat pertama kali spawn (berjaga-jaga jika uji coba langsung di scene Goa)
            FindAndAssignCamera();

            // Daftarkan event: "Tolong beritahu saya jika layar baru saja berpindah scene!"
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    public override void OnNetworkDespawn()
    {
        // Matikan alarm event jika pemain keluar agar tidak terjadi error memori (Memory Leak)
        if (IsOwner)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Jika scene yang baru dimuat BUKAN Main Menu (berarti kita masuk ke arena Goa)
        if (scene.buildIndex != 0)
        {
            FindAndAssignCamera();
        }
    }

    private void FindAndAssignCamera()
    {
        // Cari komponen kamera Cinemachine di scene yang sedang aktif
        CinemachineCamera virtualCamera = Object.FindFirstObjectByType<CinemachineCamera>();

        if (virtualCamera != null)
        {
            // Atur agar kamera mengikuti karakter ini
            virtualCamera.Follow = this.transform;
            Debug.Log($"[Kamera] Kamera sukses mengikuti Player ID: {OwnerClientId}");
        }
        else
        {
            // Hanya tampilkan peringatan jika kita BUKAN di Main Menu
            if (SceneManager.GetActiveScene().buildIndex != 0)
            {
                Debug.LogWarning("[Kamera] Cinemachine Camera tidak ditemukan di Scene ini!");
            }
        }
    }
}