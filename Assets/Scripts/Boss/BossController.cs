using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class BossController : NetworkBehaviour
{
    [Header("Statistik Dasar")]
    public float baseMaxHealth = 500f;
    public float baseAttackCooldown = 3f;

    // UI Darah Bos tersinkron
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(500f);
    private float actualMaxHealth;
    private float currentAttackCooldown;

    [Header("Arena Setup")]
    public Transform shootPoint;
    public GameObject bulletPrefab;

    // Daftar pemain di dalam arena
    private List<Transform> playersInArena = new List<Transform>();
    private bool isFighting = false;
    private float attackTimer = 0f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            actualMaxHealth = baseMaxHealth;
            currentHealth.Value = actualMaxHealth;
        }
    }

    // --- LOGIKA ARENA (HANYA SERVER) ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.CompareTag("Player"))
        {
            if (!playersInArena.Contains(collision.transform))
            {
                playersInArena.Add(collision.transform);
                CalculateDifficulty();
            }

            if (!isFighting)
            {
                StartFight();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.CompareTag("Player"))
        {
            playersInArena.Remove(collision.transform);
            CalculateDifficulty();

            if (playersInArena.Count == 0)
            {
                ResetBoss();
            }
        }
    }

    // --- LOGIKA KESULITAN (SCALING) ---
    private void CalculateDifficulty()
    {
        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        // Misal: Tiap pemain ekstra menambah HP 50% dan mempercepat serangan
        actualMaxHealth = baseMaxHealth * (1f + (0.5f * (playerCount - 1)));
        currentAttackCooldown = baseAttackCooldown * Mathf.Pow(0.85f, playerCount - 1);

        Debug.Log($"[Boss] Skala diatur! Pemain: {playerCount} | Max HP: {actualMaxHealth} | CD: {currentAttackCooldown}");
    }

    private void StartFight()
    {
        isFighting = true;
        attackTimer = currentAttackCooldown;
    }

    private void ResetBoss()
    {
        isFighting = false;
        currentHealth.Value = actualMaxHealth; // Nyawa penuh kembali
        Debug.Log("[Boss] Pemain kabur, mereset nyawa bos!");
    }

    private void Update()
    {
        if (!IsServer || !isFighting || playersInArena.Count == 0) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            ExecuteRandomAttack();
            attackTimer = currentAttackCooldown;
        }
    }

    // --- POLA SERANGAN ---
    private void ExecuteRandomAttack()
    {
        int attackType = Random.Range(1, 4); // Random 1, 2, atau 3
        Transform target = GetRandomTarget();
        if (target == null) return;

        switch (attackType)
        {
            case 1:
                SingleShot(target);
                break;
            case 2:
                SpreadShot(target);
                break;
            case 3:
                HomingShot(target);
                break;
        }
    }

    private Transform GetRandomTarget()
    {
        playersInArena.RemoveAll(item => item == null || !item.gameObject.activeInHierarchy);
        if (playersInArena.Count == 0) return null;
        return playersInArena[Random.Range(0, playersInArena.Count)];
    }

    private void SingleShot(Transform target)
    {
        Vector2 dir = (target.position - shootPoint.position).normalized;
        SpawnBullet(dir, false, null);
    }

    private void SpreadShot(Transform target)
    {
        Vector2 baseDir = (target.position - shootPoint.position).normalized;
        float[] angles = { -30f, -15f, 0f, 15f, 30f }; // 5 arah tembakan

        foreach (float angle in angles)
        {
            Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;
            SpawnBullet(dir, false, null);
        }
    }

    private void HomingShot(Transform target)
    {
        Vector2 dir = (target.position - shootPoint.position).normalized;
        SpawnBullet(dir, true, target);
    }

    private void SpawnBullet(Vector2 direction, bool homing, Transform target)
    {
        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        NetworkObject netObj = bullet.GetComponent<NetworkObject>();
        netObj.Spawn(); // Munculkan di jaringan

        bullet.GetComponent<BossBullet>().Initialize(direction, homing, target);
    }

    // --- SISTEM DAMAGE ---
    public void TakeDamage(float amount)
    {
        if (!IsServer || !isFighting) return;

        currentHealth.Value -= amount;
        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isFighting = false;
        NetworkObject.Despawn(); // Bos mati dan hancur
    }
}