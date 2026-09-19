using UnityEngine;
using UnityEngine.Tilemaps;

public class FloatingItem : MonoBehaviour
{
    [Header("Identitas Item")]
    public Sprite itemSprite;
    public TileBase itemTile;

    [Header("Pengaturan Animasi Mengambang")]
    public float amplitude = 0.2f;
    public float speed = 2f;

    private Vector3 startPos;
    private bool isBeingPickedUp = false;
    private Transform playerTarget; 

    void Start()
    {
        startPos = transform.position;
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
        else
        {
            float newY = startPos.y + Mathf.Sin(Time.time * speed) * amplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isBeingPickedUp)
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