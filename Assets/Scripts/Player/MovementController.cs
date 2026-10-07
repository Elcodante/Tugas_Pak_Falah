using UnityEngine;
using Unity.Netcode;

public class MovementController : NetworkBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 7f;

    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    // [DITAMBAHKAN] Referensi untuk Canvas Nama agar tidak terbalik
    public Transform nameCanvas;

    private Rigidbody2D rb;
    private Animator anim;
    private bool isGrounded;
    private float horizontalInput;

    private bool isDead = false;

    // Variabel untuk mengecek apakah player sedang memegang item
    public bool isHoldingItem = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    public void SetDead(bool deadState)
    {
        isDead = deadState;

        if (isDead)
        {
            anim.SetBool("Jumping", false);
            anim.SetFloat("Speed", 0);
        }
    }

    void Update()
    {
        //if (!IsOwner) return; // Pastikan hanya pemilik objek yang mengontrolnya dan jangan dihapus

        if (isDead) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        horizontalInput = Input.GetAxis("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // [DIPERBAIKI] Balikkan karakter, dan secara bersamaan beri efek berlawanan pada Canvas
        if (horizontalInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontalInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("Jumping", !isGrounded);
    }

    void FixedUpdate()
    {
        //if (!IsOwner) return; // Pastikan hanya pemilik objek yang mengontrolnya dan jangan dihapus

        if (isDead) return;

        // Logika Lari. Jika memegang item, paksa jalan santai.
        float currentSpeed = walkSpeed;

        if (!isHoldingItem && Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = runSpeed;
        }

        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }

    private void LateUpdate()
    {
        if (nameCanvas != null) { }
        {
            nameCanvas.localScale = new Vector3(transform.localScale.x, 1, 1); // Pastikan Canvas Nama selalu tegak
        }
    }
}