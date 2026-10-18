using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class EnemyPatrol : NetworkBehaviour
{
    [Header("Statistik Musuh")]
    [SerializeField] private float hp = 50f;
    [SerializeField] private float damage = 10f;

    [Header("Gerakan Dash-Step")]
    [SerializeField] private float dashSpeed = 6f;       // Kecepatan saat melangkah
    [SerializeField] private float dashDuration = 0.25f; // Lama tiap langkah (detik)
    [SerializeField] private float stepDelay = 0.7f;     // Jeda diam antar langkah (detik)

    [Header("Sensor Tembok")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float wallCheckRadius = 0.2f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool movingRight = true;

    // Status step
    private bool isDashing = false;
    private float stepTimer = 0f;

    // Status Stun disinkronkan ke seluruh pemain
    public NetworkVariable<bool> isStunned = new NetworkVariable<bool>(false);
    private float stunTimer = 0f;
    private bool isDead = false; // Pengaman tambahan agar tidak mati 2x

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stepTimer = stepDelay;
    }

    // [KUNCI PERBAIKAN] Otomatis terpanggil di Server DAN semua Client saat Despawn!
    public override void OnNetworkDespawn()
    {
        isDead = true;
        StopAllCoroutines(); // Hentikan efek kedap-kedip jika sedang berjalan
        gameObject.SetActive(false); // Matikan fisik & visual musuh di layar SEMUA pemain!
    }

    private void FixedUpdate()
    {
        if (!IsServer || isDead) return;

        if (isStunned.Value)
        {
            stunTimer -= Time.fixedDeltaTime;
            if (stunTimer <= 0)
            {
                isStunned.Value = false;
                isDashing = false;
                stepTimer = stepDelay;
            }
            return;
        }

        stepTimer -= Time.fixedDeltaTime;

        if (isDashing)
        {
            if (Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, groundLayer))
                Flip();

            float direction = movingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * dashSpeed, rb.linearVelocity.y);

            if (stepTimer <= 0f)
            {
                isDashing = false;
                stepTimer = stepDelay;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (stepTimer <= 0f)
            {
                isDashing = true;
                stepTimer = dashDuration;
            }
        }
    }

    private void Flip()
    {
        movingRight = !movingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    // --- SISTEM MENERIMA DAMAGE ---
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageRpc(float amount, float hitDirection, float knockback)
    {
        if (isDead) return;

        hp -= amount;

        // Cek kematian terlebih dahulu sebelum memicu efek visual/fisika
        if (hp <= 0)
        {
            isDead = true;
            // Memanggil Despawn(false) akan otomatis memicu OnNetworkDespawn() di semua layar pemain
            NetworkObject.Despawn(false);
            return;
        }

        isStunned.Value = true;
        stunTimer = 0.3f;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(hitDirection * knockback, knockback * 0.5f), ForceMode2D.Impulse);

        FlashEffectRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void FlashEffectRpc()
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(DamageFlasher());
    }

    private IEnumerator DamageFlasher()
    {
        if (spriteRenderer == null) yield break;

        for (int i = 0; i < 2; i++)
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSeconds(0.1f);
        }
    }

    // --- SISTEM MEMBERIKAN DAMAGE KE PLAYER ---
    private void OnCollisionEnter2D(Collision2D col) { TryDamagePlayer(col.gameObject); }
    private void OnCollisionStay2D(Collision2D col) { TryDamagePlayer(col.gameObject); }
    private void OnTriggerEnter2D(Collider2D col) { TryDamagePlayer(col.gameObject); }
    private void OnTriggerStay2D(Collider2D col) { TryDamagePlayer(col.gameObject); }

    private void TryDamagePlayer(GameObject targetObject)
    {
        // Pengaman ganda: jika sedang stun, sudah mati, atau sudah tidak terhubung ke jaringan, batalkan damage!
        if (isDead || !IsSpawned || isStunned.Value) return;

        PlayerHealth playerHealth = targetObject.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            NetworkObject playerNetObj = playerHealth.GetComponent<NetworkObject>();
            if (playerNetObj != null && playerNetObj.IsOwner)
            {
                float hitDirectionX = (targetObject.transform.position.x - transform.position.x > 0) ? 1f : -1f;
                playerHealth.TakeDamage(damage, hitDirectionX);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
        }
    }
}