using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Pengaturan Gerak")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isFacingRight = true;

    void Start()
    {
        // Mengambil komponen Rigidbody2D yang terpasang pada karakter
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Mengecek arah untuk membalikkan hadap karakter
        if (horizontalInput > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (horizontalInput < 0 && isFacingRight)
        {
            Flip();
        }
    }

    void FixedUpdate()
    {
        // Mengaplikasikan kecepatan pada sumbu X, membiarkan sumbu Y (gravitasi) tetap normal
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Flip()
    {
        // Membalikkan status arah hadap
        isFacingRight = !isFacingRight;

        // Membalikkan skala X pada Transform
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }
}