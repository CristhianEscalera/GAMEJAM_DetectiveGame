// Clase base para todas las sombras
using UnityEngine;

public abstract class BaseShadow : MonoBehaviour
{
    [Header("Configuración General")]
    [SerializeField] protected int hitsToDestroy = 3;
    [SerializeField] protected float sanityDamage = 10f;
    [SerializeField] protected float knockbackDistance = 2f;

    [Header("Efectos")]
    [SerializeField] protected ParticleSystem spawnParticles;
    [SerializeField] protected ParticleSystem damageParticles;
    [SerializeField] protected ParticleSystem deathParticles;

    [Header("Sonidos")]
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected AudioClip aliveSound;
    [SerializeField] protected AudioClip deathSound;
    [SerializeField] protected AudioClip attackSound;

    protected int currentHits = 0;
    protected Transform player;
    protected bool isDead = false;
    protected bool isAttacking = false;
    protected Coroutine currentKnockback; // Para evitar múltiples knockbacks simultáneos

    protected virtual void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    protected virtual void Start()
    {
        // Spawn con partículas
        if (spawnParticles != null)
        {
            spawnParticles.Play();
        }

        // Sonido ambiente
        if (audioSource != null && aliveSound != null)
        {
            audioSource.clip = aliveSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    protected Collider2D lastLightCollider; // Guardar referencia del collider de luz

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // Detectar luz del jugador
        if (collision.CompareTag("LuzPlayer") && !isDead)
        {
            lastLightCollider = collision; // Guardar referencia
            TakeLightDamage();
        }

        // Detectar al jugador
        if (collision.CompareTag("Player") && !isDead && !isAttacking)
        {
            AttackPlayer();
        }
    }

    protected virtual void TakeLightDamage()
    {
        currentHits++;

        // Partículas de daño
        if (damageParticles != null)
        {
            damageParticles.Play();
        }

        if (currentHits >= hitsToDestroy)
        {
            Die();
        }
        else
        {
            // Detener knockback anterior si existe
            if (currentKnockback != null)
            {
                StopCoroutine(currentKnockback);
            }
            // Knockback al recibir luz
            currentKnockback = StartCoroutine(KnockbackRoutine(true));
        }
    }

    protected virtual void AttackPlayer()
    {
        if (SanityManager.Instance != null)
        {
            SanityManager.Instance.ReduceSanity(sanityDamage);
        }

        // Sonido de ataque
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // Partículas de ataque
        if (damageParticles != null)
        {
            damageParticles.Play();
        }

        // Detener knockback anterior si existe
        if (currentKnockback != null)
        {
            StopCoroutine(currentKnockback);
        }
        // Retroceso después de atacar
        currentKnockback = StartCoroutine(KnockbackRoutine(false));
    }

    protected virtual System.Collections.IEnumerator KnockbackRoutine(bool fromLight)
    {
        isAttacking = true;

        Vector3 knockbackDirection;
        
        if (fromLight)
        {
            // Retroceder desde la luz (calcular dirección desde el centro de la luz)
            if (lastLightCollider != null)
            {
                Vector3 lightCenter = lastLightCollider.bounds.center;
                knockbackDirection = (transform.position - lightCenter).normalized;
            }
            else
            {
                // Fallback: retroceder desde el jugador
                if (player != null)
                {
                    knockbackDirection = (transform.position - player.position).normalized;
                }
                else
                {
                    knockbackDirection = -transform.right;
                }
            }
        }
        else
        {
            // Retroceder desde el jugador
            if (player != null)
            {
                knockbackDirection = (transform.position - player.position).normalized;
            }
            else
            {
                knockbackDirection = -transform.right;
            }
        }

        // Calcular posición objetivo con la distancia completa
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + (knockbackDirection * knockbackDistance);
        
        // Teleport instantáneo con la distancia correcta
        transform.position = targetPosition;

        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        currentKnockback = null; // Limpiar referencia
    }

    protected virtual void Die()
    {
        isDead = true;

        // Detener sonido ambiente
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Sonido de muerte
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Partículas de muerte
        if (deathParticles != null)
        {
            ParticleSystem particles = Instantiate(deathParticles, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }

        Destroy(gameObject, 0.5f);
    }

    protected virtual void Update()
    {
        if (isDead) return;
        
        // Lógica específica de cada sombra
        CustomUpdate();
    }

    protected abstract void CustomUpdate();
}