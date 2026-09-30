using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine; // [Unity 6/Cinemachine 3.x] Pastikan ini sesuai dengan versimu. Jika error, gunakan: using Cinemachine;

public class NetworkCameraController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // PENTING: Hanya jalankan ini jika karakter tersebut milik pemain lokal (layar kita sendiri)
        if (IsOwner)
        {
            // Mencari Virtual Camera di dalam scene yang sedang aktif
            // Catatan: Di Unity 6 dengan Cinemachine 3, komponennya bernama CinemachineCamera. 
            // Jika kamu menggunakan versi lama, ubah menjadi CinemachineVirtualCamera.
            CinemachineCamera virtualCamera = Object.FindFirstObjectByType<CinemachineCamera>();

            if (virtualCamera != null)
            {
                // Mengatur agar kamera mengikuti dan menyorot karakter ini
                virtualCamera.Follow = this.transform;
                Debug.Log($"[NetworkCameraController] Kamera sekarang mengikuti pemain {OwnerClientId}");
            }
            else
            {
                Debug.LogWarning("[NetworkCameraController] Tidak dapat menemukan Cinemachine Camera di Scene!");
            }
        }
    }
}