using UnityEngine;

public class TestMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 10f; 

    [Header("Pengaturan Tanah")]
    public Transform groundCheck; 
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer; 

    private Rigidbody2D rb;
    private Animator anim; // BARU: Menyimpan referensi Animator
    private float horizontalInput;
    private bool isGrounded;
    private bool jumpInput; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); // BARU: Mengambil komponen Animator di objek Player
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpInput = true;
        }

        if (horizontalInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < 0) transform.localScale = new Vector3(-1, 1, 1);

        // BARU: Mengirim nilai absolut dari kecepatan horizontal ke parameter "Speed" di Animator
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput)); 
    }

    void FixedUpdate()
    {
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);

        if (jumpInput)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpInput = false; 
        }
    }
}