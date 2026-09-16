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
    private SpriteRenderer spriteRenderer;
    private bool isInvulnerable = false;

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (healthUI != null)
        {
            healthUI.UpdateHealthBar(currentHealth, maxHealth);
            Debug.Log("Health UI Terhubung dengan PlayerHealth.");
        }
    }
    
    public void TakeDamage(float damageAmount, float hitDirectionX)
    {
        if (isInvulnerable)
        {
            return;
        }

        currentHealth -= damageAmount;
        Debug.Log("Player Health: " + currentHealth);

        if (healthUI != null)
        {
            healthUI.UpdateHealthBar(currentHealth, maxHealth);
        }

        ApplyKnockback(hitDirectionX);
        StartCoroutine(DamageFlasher());

        if (currentHealth <= 0)
        {
            Die();
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
            spriteRenderer.color = new Color(1f, 0f, 0f, 0.5f); // Set to red with 50% opacity
            yield return new WaitForSeconds(flashDuration / (numberOfFlashes * 2));

            spriteRenderer.color = Color.white; // Reset to original color
            yield return new WaitForSeconds(flashDuration / (numberOfFlashes * 2));
        }

        spriteRenderer.color = Color.white; // Ensure the color is reset to white at the end
        isInvulnerable = false;
    }

    private void Die()
    {
        Debug.Log("Player has died.");
        gameObject.SetActive(false);
    }
}
