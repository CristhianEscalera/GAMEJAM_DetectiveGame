// Sombra que salta en parábolas hacia el jugador
using UnityEngine;

public class JumpingShadow : BaseShadow
{
    [Header("Configuración de Salto")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float jumpDuration = 1f;
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float jumpCooldown = 2f;
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float patrolSpeed = 1.5f;

    private Vector3 patrolCenter;
    private Vector3 patrolTarget;
    private bool isJumping = false;
    private bool canJump = true;
    private Vector3 jumpStartPosition;
    private Vector3 jumpTargetPosition;
    private float jumpTimer = 0f;

    protected override void Start()
    {
        base.Start();
        patrolCenter = transform.position;
        SetNewPatrolTarget();
    }

    protected override void CustomUpdate()
    {
        if (player == null) return;

        if (isJumping)
        {
            PerformJump();
        }
        else
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange && canJump)
            {
                StartJump();
            }
            else if (!canJump)
            {
                // Esperar cooldown
            }
            else
            {
                Patrol();
            }
        }
    }

    private void Patrol()
    {
        Vector3 direction = (patrolTarget - transform.position).normalized;
        transform.position += direction * patrolSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, patrolTarget) < 0.5f)
        {
            SetNewPatrolTarget();
        }

        // Flip sprite según dirección
        FlipSprite(direction.x);
    }

    private void SetNewPatrolTarget()
    {
        Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
        patrolTarget = patrolCenter + new Vector3(randomPoint.x, randomPoint.y, 0);
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

    private void StartJump()
    {
        isJumping = true;
        canJump = false;
        jumpTimer = 0f;
        jumpStartPosition = transform.position;
        jumpTargetPosition = player.position; // Posición donde estaba el jugador al iniciar el salto

        // Activar partículas de salto
        if (spawnParticles != null)
        {
            spawnParticles.Play();
        }
    }

    private void PerformJump()
    {
        jumpTimer += Time.deltaTime;
        float progress = jumpTimer / jumpDuration;

        if (progress >= 1f)
        {
            // Terminar salto
            transform.position = jumpTargetPosition;
            isJumping = false;
            StartCoroutine(JumpCooldownRoutine());

            // Partículas de aterrizaje
            if (damageParticles != null)
            {
                damageParticles.Play();
            }
        }
        else
        {
            // Interpolación parabólica
            Vector3 currentPos = Vector3.Lerp(jumpStartPosition, jumpTargetPosition, progress);

            // Añadir altura parabólica
            float height = jumpHeight * Mathf.Sin(progress * Mathf.PI);
            currentPos.y += height;

            transform.position = currentPos;

            // Flip sprite según dirección del salto
            Vector3 direction = (jumpTargetPosition - jumpStartPosition).normalized;
            FlipSprite(direction.x);
        }
    }

    private System.Collections.IEnumerator JumpCooldownRoutine()
    {
        yield return new WaitForSeconds(jumpCooldown);
        canJump = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? patrolCenter : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);
    }
}