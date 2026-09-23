using UnityEngine;

// Define all possible states for the player
public enum PlayerState
{
    Idle,
    Walk,
    Run,
    Jump,
    Attack,
    Pickaxe,
    Dead
}

public class TestMovement : MonoBehaviour
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
    private bool isRunning;

    public bool isDead = false;
    public bool isHoldingItem = false;

    // The current state of the player
    public PlayerState currentState = PlayerState.Idle;

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
            currentState = PlayerState.Dead;
            anim.SetBool("Jumping", false);
            anim.SetFloat("Speed", 0);
        }
    }

    void Update()
    {
        if (isDead) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        horizontalInput = Input.GetAxis("Horizontal");
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // --- ACTION INPUTS ---
        // Left Click = Attack
        if (Input.GetMouseButtonDown(0) && isGrounded)
        {
            PerformAttack();
        }
        // Right Click = Pickaxe
        else if (Input.GetMouseButtonDown(1) && isGrounded)
        {
            PerformPickaxe();
        }
        // Jump Input
        else if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Update the current state based on actions
        UpdatePlayerState();

        // Flip character sprite
        if (horizontalInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < 0) transform.localScale = new Vector3(-1, 1, 1);

        // Update standard Animator parameters
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("Jumping", !isGrounded);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // Stop moving if attacking or using pickaxe
        if (currentState == PlayerState.Attack || currentState == PlayerState.Pickaxe)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return; 
        }

        float currentSpeed = walkSpeed;

        if (!isHoldingItem && isRunning)
        {
            currentSpeed = runSpeed;
        }

        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }

    private void UpdatePlayerState()
    {
        // Don't interrupt attack or pickaxe states automatically in Update
        // (You should reset the state back to Idle using Animation Events when the animation finishes)
        if (currentState == PlayerState.Attack || currentState == PlayerState.Pickaxe) return;

        // Determine state based on movement/air
        if (!isGrounded)
        {
            currentState = PlayerState.Jump;
        }
        else if (horizontalInput != 0)
        {
            currentState = (!isHoldingItem && isRunning) ? PlayerState.Run : PlayerState.Walk;
        }
        else
        {
            currentState = PlayerState.Idle;
        }
    }

    private void PerformAttack()
    {
        currentState = PlayerState.Attack;
        
        // Tells the Animator to play the Attack animation (changes sprite)
        anim.SetTrigger("Attack"); 
    }

    private void PerformPickaxe()
    {
        currentState = PlayerState.Pickaxe;
        
        // Tells the Animator to play the Pickaxe animation (changes sprite)
        anim.SetTrigger("Pickaxe"); 
    }

    // Call this method via an Animation Event at the end of your Attack/Pickaxe animations!
    public void ResetStateToIdle()
    {
        currentState = PlayerState.Idle;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}