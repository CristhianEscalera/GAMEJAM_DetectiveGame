// Sombra terrestre que persigue directamente al jugador
using UnityEngine;

public class GroundShadow : BaseShadow
{
    [Header("Configuración Terrestre")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private LayerMask groundLayer; // Para detectar el suelo
    [SerializeField] private float groundCheckDistance = 1f;
    [SerializeField] private float gravity = 20f; // Gravedad para caer al suelo
    [SerializeField] private float groundSnapDistance = 0.5f; // Distancia para "pegarse" al suelo

    private bool isGrounded = false;
    private float verticalVelocity = 0f;

    protected override void CustomUpdate()
    {
        if (player == null) return;

        ApplyGravityAndSnap();
        CheckGround();
        
        if (isGrounded)
        {
            ChasePlayer();
        }
    }

    private void ApplyGravityAndSnap()
    {
        // Raycast para detectar suelo debajo
        RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);

        if (groundHit.collider != null)
        {
            isGrounded = true;
            
            // Si está cerca del suelo, "pegarse" a él
            if (groundHit.distance > groundSnapDistance)
            {
                // Caer hacia el suelo
                verticalVelocity = -gravity * Time.deltaTime;
                Vector3 newPos = transform.position;
                newPos.y += verticalVelocity;
                
                // No pasar del suelo
                if (newPos.y < groundHit.point.y)
                {
                    newPos.y = groundHit.point.y;
                }
                
                transform.position = newPos;
            }
            else
            {
                // Ya está en el suelo, mantener posición Y
                Vector3 pos = transform.position;
                pos.y = groundHit.point.y;
                transform.position = pos;
                verticalVelocity = 0f;
            }
        }
        else
        {
            // No hay suelo, caer
            isGrounded = false;
            verticalVelocity -= gravity * Time.deltaTime;
            Vector3 newPos = transform.position;
            newPos.y += verticalVelocity * Time.deltaTime;
            transform.position = newPos;
        }
    }

    private void CheckGround()
    {
        // Raycast hacia abajo para detectar suelo
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
    }

    private void ChasePlayer()
    {
        // Solo moverse horizontalmente hacia el jugador
        Vector3 direction = player.position - transform.position;
        direction.y = 0; // Ignorar diferencia en Y
        direction.Normalize();

        // Mover solo en X
        Vector3 newPos = transform.position + new Vector3(direction.x * moveSpeed * Time.deltaTime, 0, 0);
        transform.position = newPos;

        // Flip sprite según dirección
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

    // Override del knockback para solo moverse en X y mantener en el suelo
    protected override System.Collections.IEnumerator KnockbackRoutine(bool fromLight)
    {
        isAttacking = true;

        float knockbackDirectionX = 0f;
        
        if (fromLight)
        {
            // Retroceder desde la luz (solo en X)
            if (lastLightCollider != null)
            {
                Vector3 lightCenter = lastLightCollider.bounds.center;
                float dirX = transform.position.x - lightCenter.x;
                knockbackDirectionX = dirX > 0 ? 1f : -1f; // Solo dirección X
            }
            else if (player != null)
            {
                float dirX = transform.position.x - player.position.x;
                knockbackDirectionX = dirX > 0 ? 1f : -1f;
            }
            else
            {
                knockbackDirectionX = -1f;
            }
        }
        else
        {
            // Retroceder desde el jugador (solo en X)
            if (player != null)
            {
                float dirX = transform.position.x - player.position.x;
                knockbackDirectionX = dirX > 0 ? 1f : -1f;
            }
            else
            {
                knockbackDirectionX = -1f;
            }
        }

        // Aplicar knockback solo en X, mantener Y actual
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = new Vector3(
            startPosition.x + (knockbackDirectionX * knockbackDistance),
            startPosition.y, // Y se mantendrá con la gravedad
            startPosition.z
        );
        
        transform.position = targetPosition;

        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        currentKnockback = null;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualizar detección de suelo
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}