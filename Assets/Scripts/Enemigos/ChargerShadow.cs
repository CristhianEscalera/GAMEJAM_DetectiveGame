// Sombra que se lanza en línea recta como flecha hacia el jugador
using UnityEngine;

public class ChargerShadow : BaseShadow
{
    [Header("Configuración de Carga")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float chargeTime = 1.5f; // Tiempo de recarga mirando al jugador
    [SerializeField] private float chargeSpeed = 12f; // Velocidad de embestida (rápida)
    [SerializeField] private float chargeDistance = 8f; // Distancia que recorre
    [SerializeField] private float chargeCooldown = 2f; // Tiempo de espera después de fallar
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Efectos Visuales de Carga")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color chargeColor = Color.red; // Color al cargar
    private Color originalColor;

    private Vector3 patrolCenter;
    private Vector3 patrolTarget;
    private bool isCharging = false; // Está en proceso de cargar (mirando)
    private bool isDashing = false; // Está en embestida
    private bool canCharge = true;

    private float chargeTimer = 0f;
    private float cooldownTimer = 0f;
    private Vector3 dashDirection;
    private Vector3 dashStartPosition;
    private float dashTravelDistance = 0f;

    protected override void Start()
    {
        base.Start();
        patrolCenter = transform.position;
        SetNewPatrolTarget();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    protected override void CustomUpdate()
    {
        if (player == null) return;

        if (isDashing)
        {
            PerformDash();
        }
        else if (isCharging)
        {
            ChargeAttack();
        }
        else if (!canCharge)
        {
            WaitCooldown();
        }
        else
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange)
            {
                StartCharging();
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

    private void StartCharging()
    {
        isCharging = true;
        chargeTimer = 0f;

        // Cambiar color para indicar que está cargando
        if (spriteRenderer != null)
        {
            spriteRenderer.color = chargeColor;
        }

        // Reproducir partículas de carga
        if (spawnParticles != null)
        {
            spawnParticles.Play();
        }

        // Calcular dirección hacia donde está el jugador AHORA
        dashDirection = (player.position - transform.position).normalized;

        // Mirar al jugador
        FlipSprite(dashDirection.x);
    }

    private void ChargeAttack()
    {
        chargeTimer += Time.deltaTime;

        // Parpadeo visual durante la carga
        if (spriteRenderer != null)
        {
            float alpha = Mathf.PingPong(Time.time * 8f, 1f);
            Color currentColor = chargeColor;
            currentColor.a = alpha;
            spriteRenderer.color = currentColor;
        }

        // Cuando termina el tiempo de carga, lanzarse
        if (chargeTimer >= chargeTime)
        {
            LaunchDash();
        }
    }

    private void LaunchDash()
    {
        isCharging = false;
        isDashing = true;
        dashStartPosition = transform.position;
        dashTravelDistance = 0f;

        // Restaurar color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // Sonido de embestida
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // Partículas de lanzamiento
        if (damageParticles != null)
        {
            damageParticles.Play();
        }
    }

    private void PerformDash()
    {
        // Moverse en línea recta
        float moveAmount = chargeSpeed * Time.deltaTime;
        transform.position += dashDirection * moveAmount;
        dashTravelDistance += moveAmount;

        // Si recorrió la distancia completa, terminar embestida
        if (dashTravelDistance >= chargeDistance)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        isDashing = false;
        canCharge = false;
        cooldownTimer = 0f;

        // Partículas al terminar
        if (damageParticles != null)
        {
            damageParticles.Play();
        }
    }

    private void WaitCooldown()
    {
        cooldownTimer += Time.deltaTime;

        if (cooldownTimer >= chargeCooldown)
        {
            canCharge = true;
        }
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

    // Override para que no interrumpa la embestida
    protected override System.Collections.IEnumerator KnockbackRoutine(bool fromLight)
    {
        // Si está en medio de una embestida, terminarla primero
        if (isDashing)
        {
            EndDash();
        }

        // Si está cargando, cancelar la carga
        if (isCharging)
        {
            isCharging = false;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }

        return base.KnockbackRoutine(fromLight);
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

        // Visualizar distancia de embestida si está cargando
        if (isCharging && Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + dashDirection * chargeDistance);
        }
    }
}