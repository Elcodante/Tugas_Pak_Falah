using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Statistik Musuh")]
    [SerializeField] private float hp = 50f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float damage = 10f;

    [Header("Sensor Tembok")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float wallCheckRadius = 0.2f;

    private Rigidbody2D rb;
    private bool movingRight = true;

    // Tambahan untuk logika Stun/Knockback
    private bool isStunned = false;
    private float stunTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Jika sedang terkena hit (Stun), kurangi waktu stun dan hentikan patroli
        if (isStunned)
        {
            stunTimer -= Time.fixedDeltaTime;
            if (stunTimer <= 0)
            {
                isStunned = false;
            }
            return; // Keluar dari FixedUpdate agar musuh tidak maksa jalan
        }

        // 1. Cek apakah di depan ada tembok/tilemap
        bool isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, groundLayer);

        if (isTouchingWall)
        {
            Flip();
        }

        // 2. Gerakkan musuh
        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
    }

    private void OnDrawGizmos()
    {
        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
        }
    }

    private void Flip()
    {
        movingRight = !movingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    // FUNGSI DIPERBARUI: Menerima input dari pedang Player
    public void TakeDamage(float amount, float hitDirection, float knockbackForce)
    {
        hp -= amount;
        Debug.Log("Musuh terkena damage! HP tersisa: " + hp);

        isStunned = true;
        stunTimer = 0.3f; 

        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(new Vector2(hitDirection * knockbackForce, knockbackForce * 0.5f), ForceMode2D.Impulse);

        if (hp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            NetworkObject playerNetObj = collision.gameObject.GetComponent<NetworkObject>();

            if (playerNetObj != null && playerNetObj.IsOwner)
            {
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {

                    float hitDirectionX = (collision.transform.position.x - transform.position.x > 0) ? 1f : -1f;
                    playerHealth.TakeDamage(damage, hitDirectionX);
                }
            }
        }
    }
}