using UnityEngine;

public class MovementController : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 7f;

    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    private Rigidbody2D rb;
    private Animator anim;
    private bool isGrounded;
    private float horizontalInput;

    private bool isDead = false;

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
        if (isDead) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        horizontalInput = Input.GetAxis("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (horizontalInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < 0) transform.localScale = new Vector3(-1, 1, 1);

        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("Jumping", !isGrounded);
    }

    void FixedUpdate()
    {
        // [TAMBAHKAN] Jika sudah mati, JANGAN jalankan kode fisika
        if (isDead) return;

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }
}