using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerInfo : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // Fungsi ini otomatis berjalan saat prefab Player ini di-spawn (muncul) di jaringan

        // Cari RelayManager di scene, lalu suruh dia menambahkan nama kita ke UI
        if (RelayManager.Instance != null)
        {
            RelayManager.Instance.AddPlayerToUI(OwnerClientId);
        }
    }
}