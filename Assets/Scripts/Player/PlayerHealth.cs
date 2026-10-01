using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.SceneManagement; // [TAMBAHKAN] Library untuk mendeteksi perpindahan scene

public class PlayerHealth : NetworkBehaviour
{
    [Header("Statistics Player")]
    [SerializeField] private float maxHealth = 100f;

    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("UI References")]
    [SerializeField] private HealthUI healthUI;

    [Header("Efek Visual")]
    [SerializeField] private float knockbackForceX = 5f;
    [SerializeField] private float knockbackForceY = 3f;
    [SerializeField] private float flashDuration = 1.5f;
    [SerializeField] private int numberOfFlashes = 6;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private MovementController movementController;

    private bool isInvulnerable = false;
    private bool isDead = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        movementController = GetComponent<MovementController>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            FindAndInitializeUI(); // Coba cari UI saat pertama kali spawn

            // Daftarkan alarm saat layar berpindah Scene
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += (oldHealth, newHealth) =>
        {
            if (IsOwner && healthUI != null)
            {
                healthUI.UpdateHealthBar(newHealth, maxHealth);
            }
            Debug.Log($"Player {OwnerClientId} Health: {newHealth}");
        };
    }

    public override void OnNetworkDespawn()
    {
        // Matikan alarm jika pemain keluar game
        if (IsOwner)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Jika masuk ke scene permainan (bukan Main Menu index 0), cari UI Nyawa!
        if (scene.buildIndex != 0)
        {
            FindAndInitializeUI();
        }
    }

    private void FindAndInitializeUI()
    {
        // Cari objek yang memiliki komponen HealthUI di scene aktif
        healthUI = Object.FindFirstObjectByType<HealthUI>();

        if (healthUI != null)
        {
            healthUI.UpdateHealthBar(currentHealth.Value, maxHealth);
            Debug.Log($"[PlayerHealth] UI Nyawa berhasil ditemukan untuk Player {OwnerClientId}!");
        }
        else
        {
            // Jangan beri peringatan jika sedang di Main Menu
            if (SceneManager.GetActiveScene().buildIndex != 0)
            {
                Debug.LogWarning("[PlayerHealth] HealthUI tidak ditemukan! Pastikan Panel_HUD aktif dan memiliki script HealthUI.");
            }
        }
    }

    public void TakeDamage(float damageAmount, float hitDirectionX)
    {
        if (isInvulnerable || isDead) return;
        TakeDamageRpc(damageAmount, hitDirectionX);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TakeDamageRpc(float damageAmount, float hitDirectionX)
    {
        if (isInvulnerable || isDead) return;

        currentHealth.Value -= damageAmount;

        if (currentHealth.Value <= 0)
        {
            DieRpc(hitDirectionX);
        }
        else
        {
            ApplyKnockbackRpc(hitDirectionX);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ApplyKnockbackRpc(float hitDirectionX)
    {
        ApplyKnockback(hitDirectionX);
        StartCoroutine(DamageFlasher());
    }

    [Rpc(SendTo.Everyone)]
    private void DieRpc(float hitDirectionX)
    {
        isDead = true;
        if (movementController != null) movementController.SetDead(true);

        anim.SetBool("isDead", true);
        ApplyKnockback(hitDirectionX);

        StartCoroutine(RespawnRoutine());
    }

    private void ApplyKnockback(float hitDirectionX)
    {
        rb.linearVelocity = Vector2.zero;
        Vector2 knockback = new Vector2(hitDirectionX * knockbackForceX, knockbackForceY);
        rb.AddForce(knockback, ForceMode2D.Impulse);
    }

    private IEnumerator DamageFlasher()
    {
        isInvulnerable = true;
        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRenderer.color = new Color(1f, 0f, 0f, 0.5f);
            yield return new WaitForSeconds(flashDuration / (numberOfFlashes * 2));
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(flashDuration / (numberOfFlashes * 2));
        }
        spriteRenderer.color = Color.white;
        isInvulnerable = false;
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(2f);

        isDead = false;
        if (movementController != null) movementController.SetDead(false);
        anim.SetBool("isDead", false);
        isInvulnerable = false;
        spriteRenderer.color = Color.white;

        if (IsOwner)
        {
            if (SpawnManager.Instance != null)
            {
                transform.position = SpawnManager.Instance.GetSpawnPosition(OwnerClientId);
            }

            RestoreHealthRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void RestoreHealthRpc()
    {
        currentHealth.Value = maxHealth;
    }
}