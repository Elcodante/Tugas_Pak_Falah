using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class FloatingItem : MonoBehaviour
{
    [Header("Identitas Item")]
    public Sprite itemSprite;
    public TileBase itemTile;

    [Header("Pengaturan Animasi Mengambang")]
    public float amplitude = 0.2f;
    public float speed = 2f;

    private Vector3 floatStartPos;
    private bool isBeingPickedUp = false;
    private Transform playerTarget;

    private Rigidbody2D rb;
    private bool isFalling = true;
    private float spawnTime; // Waktu saat item pertama kali diciptakan

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        isFalling = true;
        spawnTime = Time.time; // Catat waktu spawn
    }

    void Update()
    {
        if (isBeingPickedUp && playerTarget != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, 8f * Time.deltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 15f * Time.deltaTime);

            if (Vector3.Distance(transform.position, playerTarget.position) < 0.2f)
            {
                MiningController playerMining = playerTarget.GetComponent<MiningController>();
                if (playerMining != null)
                {
                    bool success = playerMining.AddItem(this);
                    if (success) Destroy(gameObject);
                }
            }
        }
        else if (!isFalling)
        {
            float newY = floatStartPos.y + Mathf.Sin(Time.time * speed) * amplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") && isFalling)
        {
            isFalling = false;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            floatStartPos = transform.position;
            GetComponent<Collider2D>().isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isBeingPickedUp && !isFalling && (Time.time - spawnTime > 0.5f))
        {
            MiningController playerMining = collision.GetComponent<MiningController>();

            if (playerMining != null && playerMining.blockCount >= 1)
            {
                return;
            }

            isBeingPickedUp = true;
            playerTarget = collision.transform;
        }
    }
}