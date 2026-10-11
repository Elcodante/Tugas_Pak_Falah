using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Pengaturan Combat")]
    public float attackCooldown = 0.5f;

    [Header("Statistik Serangan")]
    public float attackDamage = 10f;
    public float knockbackForce = 5f;

    [Header("Pengaturan Hitbox (Sensor Serangan)")]
    public Transform attackPoint;
    public float attackRange = 1f;
    public LayerMask enemyLayers;

    private float nextAttackTime = 0f;
    private Animator anim;
    private MovementController moveControl;

    void Start()
    {
        anim = GetComponent<Animator>();
        moveControl = GetComponent<MovementController>();
    }

    void Update()
    {
        // if (!IsOwner) return;

        if (moveControl != null && moveControl.isHoldingItem) return;
        if (Input.GetMouseButton(1)) return;
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            Attack();
        }
    }

    private void Attack()
    {
        anim.SetTrigger("Attack");
        nextAttackTime = Time.time + attackCooldown;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyPatrol enemyScript = enemy.GetComponent<EnemyPatrol>();
            if (enemyScript != null)
            {
                float hitDirection = (enemy.transform.position.x > transform.position.x) ? 1f : -1f;
                enemyScript.TakeDamage(attackDamage, hitDirection, knockbackForce);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}