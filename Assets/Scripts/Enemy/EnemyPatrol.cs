using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Statistik Musuh")]
    [SerializeField] private float hp = 50f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float damage = 10f;

    [Header("Sensor Tembok")]
    [SerializeField] private Transform wallCheck;      // Masukkan objek WallCheck ke sini
    [SerializeField] private LayerMask groundLayer;    // Pilih layer "Ground" di Inspector
    [SerializeField] private float wallCheckRadius = 0.2f; // Ukuran lingkaran sensor

    private Rigidbody2D rb;
    private bool movingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
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

    // Fungsi visual agar kita bisa melihat lingkaran sensor di Unity Editor
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
        // Membalikkan status arah
        movingRight = !movingRight;

        // Membalikkan visual sprite musuh (menghadap ke arah sebaliknya)
        // Karena WallCheck adalah child dari musuh, posisinya akan ikut berbalik otomatis
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    public void TakeDamage(float amount)
    {
        hp -= amount;
        Debug.Log("Musuh terkena damage! HP tersisa: " + hp);

        if (hp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Hapus objek dari scene ketika HP habis
        Destroy(gameObject);
    }
}