using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    // Membuat Singleton agar bisa diakses dari script Player
    public static SpawnManager Instance;

    [Header("Titik Kemunculan Pemain")]
    public Transform[] spawnPoints; // Array untuk menyimpan 4 koordinat

    private void Awake()
    {
        Instance = this;
    }

    public Vector3 GetSpawnPosition(ulong clientId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return Vector3.zero;

        // Membagi ID Pemain (0, 1, 2, 3) dengan jumlah titik menggunakan Modulo (%)
        // Ini memastikan pemain ke-1 ke titik 1, pemain ke-2 ke titik 2, dst.
        int index = (int)(clientId % (ulong)spawnPoints.Length);

        return spawnPoints[index].position;
    }
}