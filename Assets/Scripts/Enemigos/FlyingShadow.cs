// Sombra voladora con patrullaje y persecución
using UnityEngine;

public class FlyingShadow : BaseShadow
{
    [Header("Configuración de Vuelo")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float patrolSpeed = 2f;

    private Vector3 patrolCenter;
    private Vector3 patrolTarget;
    private bool isChasing = false;

    protected override void Start()
    {
        base.Start();
        patrolCenter = transform.position;
        SetNewPatrolTarget();
    }

    protected override void CustomUpdate()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Detectar si el jugador está en rango
        if (distanceToPlayer <= detectionRange)
        {
            isChasing = true;
            ChasePlayer();
        }
        else
        {
            isChasing = false;
            Patrol();
        }
    }

    private void ChasePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Flip sprite según dirección
        FlipSprite(direction.x);
    }

    private void Patrol()
    {
        Vector3 direction = (patrolTarget - transform.position).normalized;
        transform.position += direction * patrolSpeed * Time.deltaTime;

        // Si llegó al objetivo, establecer uno nuevo
        if (Vector3.Distance(transform.position, patrolTarget) < 0.5f)
        {
            SetNewPatrolTarget();
        }

        // Flip sprite según dirección de patrulla
        FlipSprite(direction.x);
    }

    private void FlipSprite(float directionX)
    {
        if (directionX > 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (directionX < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    private void SetNewPatrolTarget()
    {
        Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
        patrolTarget = patrolCenter + new Vector3(randomPoint.x, randomPoint.y, 0);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualizar rango de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Visualizar área de patrullaje
        Gizmos.color = Color.blue;
        Vector3 center = Application.isPlaying ? patrolCenter : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);
    }
}