using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BossBullet : NetworkBehaviour
{
    public float speed = 10f;
    public float damage = 20f;
    public float lifetime = 5f; // Peluru hancur sendiri dalam 5 detik

    private Transform homingTarget;
    private bool isHoming = false;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        // Hanya Server yang menghitung pergerakan peluru
        if (IsServer)
        {
            Invoke(nameof(DestroyBullet), lifetime);
        }
    }

    // Fungsi ini dipanggil oleh Bos saat menembak
    public void Initialize(Vector2 direction, bool homing, Transform target = null)
    {
        isHoming = homing;
        homingTarget = target;
        rb.linearVelocity = direction.normalized * speed;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return; // Client hanya menonton

        // Logika Peluru Mengejar
        if (isHoming && homingTarget != null)
        {
            Vector2 direction = (homingTarget.position - transform.position).normalized;
            // Berbelok perlahan menuju target
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, direction * speed, Time.fixedDeltaTime * 2f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return; // Hanya server yang mengecek tabrakan

        // Abaikan jika mengenai sesama bos atau peluru lain
        if (collision.CompareTag("Enemy") || collision.CompareTag("Bullet")) return;

        // Jika terkena player
        if (collision.CompareTag("Player"))
        {
            PlayerHealth ph = collision.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                float hitDirX = (collision.transform.position.x - transform.position.x > 0) ? 1f : -1f;
                ph.TakeDamage(damage, hitDirX);
            }
        }

        // Hancurkan peluru jika mengenai apapun (Player atau Tanah/Ground)
        DestroyBullet();
    }

    private void DestroyBullet()
    {
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(); // Hapus dari jaringan
        }
    }
}