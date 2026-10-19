using UnityEngine;
using Unity.Netcode;

public class MovementController : NetworkBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 7f;

    [Header("Pengaturan Lompat & Coyote Time")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    [Tooltip("Berapa lama (dalam detik) pemain masih bisa melompat setelah jatuh dari ujung tebing?")]
    public float coyoteTime = 0.2f; // Waktu toleransi standar (0.2 detik sangat nyaman)

    // Referensi untuk Canvas Nama agar tidak ikut terbalik saat karakter menoleh
    public Transform nameCanvas;

    private Rigidbody2D rb;
    private Animator anim;
    private bool isGrounded;
    private float horizontalInput;

    // Variabel internal penghitung Coyote Time
    private float coyoteTimeCounter;

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

        if (isDead && anim != null)
        {
            anim.SetBool("Jumping", false);
            anim.SetFloat("Speed", 0);
        }
    }

    void Update()
    {
        // WAJIB AKTIF: Hanya pemilik asli karakter ini yang boleh membaca input & mengatur animasinya!
        if (!IsOwner) return;

        if (isDead) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // --- LOGIKA COYOTE TIME ---
        if (isGrounded)
        {
            // Jika menginjak tanah, isi penuh waktu toleransinya
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            // Jika sedang di udara/jatuh, kurangi waktunya perlahan
            coyoteTimeCounter -= Time.deltaTime;
        }

        horizontalInput = Input.GetAxis("Horizontal");

        // [PERBAIKAN LOMPATAN]: Ganti 'isGrounded' dengan 'coyoteTimeCounter > 0f'
        if (Input.GetButtonDown("Jump") && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // PENGAMAN PENTING: Kuras habis waktunya agar pemain tidak bisa melakukan "Double Jump" di udara
            coyoteTimeCounter = 0f;
        }

        // Balikkan arah hadap karakter berdasarkan input pemiliknya
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
        anim.SetBool("IsHolding", isHoldingItem);
    }

    void FixedUpdate()
    {
        // WAJIB AKTIF: Jangan biarkan komputer kita menggerakkan fisik karakter milik pemain lain!
        if (!IsOwner) return;

        if (isDead) return;

        float currentSpeed = walkSpeed;

        if (!isHoldingItem && Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = runSpeed;
        }

        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }

    private void LateUpdate()
    {
        if (nameCanvas != null)
        {
            nameCanvas.localScale = new Vector3(transform.localScale.x, 1, 1);
        }
    }
}