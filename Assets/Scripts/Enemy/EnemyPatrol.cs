using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class EnemyPatrol : NetworkBehaviour // UBAH KE NETWORK BEHAVIOUR
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
    private SpriteRenderer spriteRenderer;
    private bool movingRight = true;

    // Status Stun disinkronkan ke seluruh pemain agar musuh tidak melukai siapa pun saat terpental
    public NetworkVariable<bool> isStunned = new NetworkVariable<bool>(false);
    private float stunTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        // PENTING: Hanya Server yang boleh menggerakkan fisik musuh!
        if (!IsServer) return;

        // Jika sedang terkena hit (Stun)
        if (isStunned.Value)
        {
            stunTimer -= Time.fixedDeltaTime;
            if (stunTimer <= 0)
            {
                isStunned.Value = false; // Stun selesai
            }
            return;
        }

        bool isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, groundLayer);
        if (isTouchingWall) Flip();

        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
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
        hp -= amount;
        isStunned.Value = true;
        stunTimer = 0.3f;

        // Efek Knockback (Dihitung oleh Server, dilihat oleh semua)
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(hitDirection * knockback, knockback * 0.5f), ForceMode2D.Impulse);

        // Perintahkan semua pemain untuk memutar efek kedap-kedip
        FlashEffectRpc();

        if (hp <= 0)
        {
            NetworkObject.Despawn(false); // Hancurkan musuh dari seluruh jaringan
            gameObject.SetActive(false); // Nonaktifkan musuh di scene
        }
    }

    [Rpc(SendTo.Everyone)]
    private void FlashEffectRpc()
    {
        StartCoroutine(DamageFlasher());
    }

    private IEnumerator DamageFlasher()
    {
        if (spriteRenderer == null) yield break;

        // Kedap-kedip: Alpha 0 -> 1 -> 0 -> 1
        for (int i = 0; i < 2; i++)
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, 0f); // Tembus pandang
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f); // Terlihat
            yield return new WaitForSeconds(0.1f);
        }
    }

    // --- SISTEM MEMBERIKAN DAMAGE KE PLAYER (ANTI-GAGAL) ---
    private void OnCollisionEnter2D(Collision2D col) { TryDamagePlayer(col.gameObject); }
    private void OnCollisionStay2D(Collision2D col) { TryDamagePlayer(col.gameObject); }
    private void OnTriggerEnter2D(Collider2D col) { TryDamagePlayer(col.gameObject); }
    private void OnTriggerStay2D(Collider2D col) { TryDamagePlayer(col.gameObject); }

    private void TryDamagePlayer(GameObject targetObject)
    {
        // Jangan melukai player jika musuh sedang terpental
        if (isStunned.Value) return;

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