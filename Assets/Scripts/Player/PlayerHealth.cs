using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Statistics Player")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

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
    private MovementController movementController; // [TAMBAHKAN] Referensi ke script Movement

    private bool isInvulnerable = false;
    private bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        movementController = GetComponent<MovementController>(); // [TAMBAHKAN]

        if (healthUI != null)
        {
            healthUI.UpdateHealthBar(currentHealth, maxHealth);
            Debug.Log("Health UI Terhubung dengan PlayerHealth.");
        }
    }

    public void TakeDamage(float damageAmount, float hitDirectionX)
    {
        if (isInvulnerable || isDead)
        {
            return;
        }

        currentHealth -= damageAmount;
        Debug.Log("Player Health: " + currentHealth);

        if (healthUI != null)
        {
            healthUI.UpdateHealthBar(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            // Panggil Die dengan menyertakan arah serangan agar knockback terakhir tetap ada
            Die(hitDirectionX);
        }
        else
        {
            ApplyKnockback(hitDirectionX);
            StartCoroutine(DamageFlasher());
        }
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

    // [UBAH] Fungsi Die menerima arah pukulan
    private void Die(float hitDirectionX)
    {
        isDead = true;
        Debug.Log("Player has died.");

        // 1. Beri tahu script Movement bahwa player mati (ini akan mematikan logika Jumping)
        if (movementController != null)
        {
            movementController.SetDead(true);
        }

        // 2. Jalankan animasi mati
        anim.SetBool("isDead", true);

        // 3. Terapkan knockback kematian (karena MovementController sudah mati, knockback ini tidak akan memicu animasi Jump lagi)
        ApplyKnockback(hitDirectionX);

        StartCoroutine(DisableAfterDeath());
    }

    private IEnumerator DisableAfterDeath()
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }
}