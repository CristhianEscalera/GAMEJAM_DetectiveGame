using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Velocidades")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float climbSpeed = 3f;

    [Header("Suavizado de movimiento")]
    [Range(0, 0.3f)]
    [SerializeField] private float movementSmoothing = 0.05f;

    [Header("Flip automático")]
    [SerializeField] private bool flipSprite = true;

    [Header("Estamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRecoveryRate = 15f;
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private GameObject staminaBarUI;  // Cambiado a GameObject para el canvas hijo

    private float currentStamina;
    private bool isStaminaVisible = false;
    private Transform staminaBarTransform; // Para mantener la escala correcta

    private Rigidbody2D rb;
    private Vector2 velocity = Vector2.zero;
    private bool facingRight = true;

    // --- Escaleras ---
    private bool isOnLadder = false;
    private bool isClimbing = false;
    private float horizontalInput;
    private float verticalInput;
    private float defaultGravity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        defaultGravity = rb.gravityScale;
        currentStamina = maxStamina;

        // Guardar referencia del transform de la barra
        if (staminaBarUI != null)
        {
            staminaBarTransform = staminaBarUI.transform;
            staminaBarUI.SetActive(false);
        }
    }

    private void Update()
    {
        // No permitir movimiento si hay un diálogo activo
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        // Leer entrada cada frame
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Control de escaleras
        if (isOnLadder && Mathf.Abs(verticalInput) > 0f)
            isClimbing = true;

        if (!isOnLadder)
        {
            isClimbing = false;
            rb.gravityScale = defaultGravity;
        }

        HandleStamina();
        UpdateStaminaUI();
        HandleStaminaVisibility();
    }

    private void FixedUpdate()
    {
        if (isClimbing)
            HandleClimbMovement();
        else
            HandleGroundMovement();
    }

    // --- MOVIMIENTO EN SUELO ---
    private void HandleGroundMovement()
    {
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool canRun = currentStamina > 0.1f;
        bool isRunning = wantsToRun && canRun && Mathf.Abs(horizontalInput) > 0f;

        float targetSpeed = (isRunning ? runSpeed : walkSpeed) * horizontalInput;
        float smoothedX = Mathf.SmoothDamp(rb.velocity.x, targetSpeed, ref velocity.x, movementSmoothing);

        rb.velocity = new Vector2(smoothedX, rb.velocity.y);

        // Flip visual
        if (flipSprite)
        {
            if (horizontalInput > 0 && !facingRight)
                Flip();
            else if (horizontalInput < 0 && facingRight)
                Flip();
        }
    }

    // --- MOVIMIENTO EN ESCALERAS ---
    private void HandleClimbMovement()
    {
        rb.gravityScale = 0f;

        float climbVelocity = verticalInput * climbSpeed;
        float climbHorizontal = horizontalInput * (walkSpeed * 0.5f);

        rb.velocity = new Vector2(climbHorizontal, climbVelocity);

        // Si no se mueve, queda quieto
        if (Mathf.Abs(verticalInput) < 0.1f && Mathf.Abs(horizontalInput) < 0.1f)
            rb.velocity = Vector2.zero;
    }

    // --- CONTROL DE ESTAMINA ---
    private void HandleStamina()
    {
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f && !isClimbing;

        if (wantsToRun && isMoving && currentStamina > 0f)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else
        {
            currentStamina += staminaRecoveryRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    // --- ACTUALIZAR BARRA DE ESTAMINA ---
    private void UpdateStaminaUI()
    {
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = currentStamina / maxStamina;
        }
    }

    // --- MOSTRAR / OCULTAR BARRA DE ESTAMINA ---
    private void HandleStaminaVisibility()
    {
        if (staminaBarUI == null) return;

        bool shouldBeVisible = currentStamina < maxStamina;

        if (shouldBeVisible && !isStaminaVisible)
        {
            isStaminaVisible = true;
            staminaBarUI.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeCanvasWorldSpace(1f, 0.25f));
        }
        else if (!shouldBeVisible && isStaminaVisible)
        {
            isStaminaVisible = false;
            StopAllCoroutines();
            StartCoroutine(FadeCanvasWorldSpace(0f, 0.5f, () =>
            {
                staminaBarUI.SetActive(false);
            }));
        }
    }

    private System.Collections.IEnumerator FadeCanvasWorldSpace(float targetAlpha, float duration, System.Action onComplete = null)
    {
        if (staminaBarFill == null) yield break;

        Color startColor = staminaBarFill.color;
        float startAlpha = startColor.a;
        float time = 0f;

        // También fade de otros elementos UI en el canvas si existen
        Image[] allImages = staminaBarUI.GetComponentsInChildren<Image>();

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);

            foreach (Image img in allImages)
            {
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }

            yield return null;
        }

        // Asegurar alpha final
        foreach (Image img in allImages)
        {
            Color c = img.color;
            c.a = targetAlpha;
            img.color = c;
        }

        onComplete?.Invoke();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        // Compensar el flip en la barra de estamina para que mantenga su orientación
        if (staminaBarTransform != null)
        {
            Vector3 barScale = staminaBarTransform.localScale;
            barScale.x *= -1;
            staminaBarTransform.localScale = barScale;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Escalera"))
            isOnLadder = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Escalera"))
        {
            isOnLadder = false;
            isClimbing = false;
            rb.gravityScale = defaultGravity;
        }
    }
}