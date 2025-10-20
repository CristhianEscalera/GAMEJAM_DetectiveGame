// ============= FlashlightController.cs =============
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Rendering.Universal;

public class FlashlightController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject flashlightVisual; // Sprite/Light2D de la linterna
    [SerializeField] private Image batteryBarFill;
    [SerializeField] private CanvasGroup batteryBarCanvas;

    [Header("Referencias de Destello")]
    [SerializeField] private Light2D flashLight2D; // Light2D principal
    [SerializeField] private PolygonCollider2D flashCollider; // Collider que se activa con el destello
    [SerializeField] private ParticleSystem flashParticles; // Partículas del destello
    [SerializeField] private AudioSource flashAudioSource; // Sonido del destello
    [SerializeField] private AudioClip flashSound; // Clip de sonido

    [Header("Configuración de Batería")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float batteryDrainRate = 10f;    // Por segundo
    [SerializeField] private float batteryRecoveryRate = 5f;  // Por segundo cuando está apagada

    [Header("Configuración de Destello")]
    [SerializeField] private float flashDuration = 2f; // Duración del destello en segundos
    [SerializeField] private float flashIntensityMultiplier = 3f; // Multiplicador de intensidad
    [SerializeField] private float flashBatteryCost = 20f; // Batería que consume el destello
    [SerializeField] private float flashCooldown = 1f; // Tiempo entre destellos

    [Header("Configuración de Movimiento")]
    [SerializeField] private float orbitRadius = 1.5f;        // Distancia del player
    [SerializeField] private float rotationSpeed = 10f;       // Velocidad de seguimiento del mouse

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    private float currentBattery;
    private bool isFlashlightOn = false;
    private bool isBatteryVisible = false;
    private Vector3 targetDirection;

    // Variables para el destello
    private bool isFlashing = false;
    private float flashTimer = 0f;
    private float cooldownTimer = 0f;
    private float normalIntensity = 1f;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (player == null)
            player = transform.parent;

        currentBattery = maxBattery;

        // Guardar intensidad normal del Light2D
        if (flashLight2D != null)
            normalIntensity = flashLight2D.intensity;

        // Desactivar collider al inicio
        if (flashCollider != null)
            flashCollider.enabled = false;

        // Asegurar que la linterna esté apagada al inicio
        if (flashlightVisual != null)
            flashlightVisual.SetActive(false);
    }

    private void Update()
    {
        HandleFlashlightInput();
        HandleFlashInput();
        HandleBattery();
        UpdateBatteryUI();
        UpdateFlash();
        UpdateCooldown();

        if (isFlashlightOn)
        {
            FollowMouse();
        }
    }

    // --- CONTROL DE INPUT ---
    private void HandleFlashlightInput()
    {
        // Solo permitir usar linterna si no hay diálogo activo
        bool canUseFlashlight = (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive())
                            && (CableMinigameManager.Instance == null || !CableMinigameManager.Instance.IsMinigameActive());

        if (!canUseFlashlight)
        {
            if (isFlashlightOn)
                TurnOffFlashlight();
            return;
        }

        // Click izquierdo mantenido y hay batería
        if (Input.GetMouseButton(0) && currentBattery > 0f)
        {
            if (!isFlashlightOn)
                TurnOnFlashlight();
        }
        else
        {
            if (isFlashlightOn)
                TurnOffFlashlight();
        }
    }

    // --- CONTROL DE DESTELLO ---
    private void HandleFlashInput()
    {
        // Solo se puede activar el destello si:
        // 1. La linterna está encendida (clic izquierdo mantenido)
        // 2. No está en cooldown
        // 3. Hay suficiente batería
        // 4. No hay diálogo activo
        bool canUseFlash = isFlashlightOn
                        && !isFlashing
                        && cooldownTimer <= 0f
                        && currentBattery >= flashBatteryCost
                        && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive())
                        && (CableMinigameManager.Instance == null || !CableMinigameManager.Instance.IsMinigameActive());

        if (canUseFlash && Input.GetMouseButtonDown(1))
        {
            ActivateFlash();
        }
    }

    // --- ACTIVAR DESTELLO ---
    private void ActivateFlash()
    {
        isFlashing = true;
        flashTimer = flashDuration;
        cooldownTimer = flashCooldown;

        // Consumir batería
        currentBattery -= flashBatteryCost;
        currentBattery = Mathf.Max(0f, currentBattery);

        // Aumentar intensidad de la luz
        if (flashLight2D != null)
        {
            flashLight2D.intensity = normalIntensity * flashIntensityMultiplier;
        }

        // Activar collider
        if (flashCollider != null)
        {
            flashCollider.enabled = true;
        }

        // Reproducir partículas
        if (flashParticles != null)
        {
            flashParticles.Play();
        }

        // Reproducir sonido
        if (flashAudioSource != null && flashSound != null)
        {
            flashAudioSource.PlayOneShot(flashSound);
        }
    }

    // --- ACTUALIZAR ESTADO DEL DESTELLO ---
    private void UpdateFlash()
    {
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;

            if (flashTimer <= 0f)
            {
                DeactivateFlash();
            }
        }
    }

    // --- DESACTIVAR DESTELLO ---
    private void DeactivateFlash()
    {
        isFlashing = false;

        // Restaurar intensidad normal
        if (flashLight2D != null)
        {
            flashLight2D.intensity = normalIntensity;
        }

        // Desactivar collider
        if (flashCollider != null)
        {
            flashCollider.enabled = false;
        }
    }

    // --- ACTUALIZAR COOLDOWN ---
    private void UpdateCooldown()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    // --- ENCENDER LINTERNA ---
    private void TurnOnFlashlight()
    {
        isFlashlightOn = true;
        if (flashlightVisual != null)
            flashlightVisual.SetActive(true);
    }

    // --- APAGAR LINTERNA ---
    private void TurnOffFlashlight()
    {
        isFlashlightOn = false;
        if (flashlightVisual != null)
            flashlightVisual.SetActive(false);

        // Si está en destello, desactivarlo también
        if (isFlashing)
        {
            DeactivateFlash();
        }
    }

    // --- SEGUIR EL MOUSE ---
    private void FollowMouse()
    {
        if (mainCamera == null || player == null) return;

        // Obtener posición del mouse en el mundo
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // Calcular dirección desde el player hacia el mouse
        targetDirection = (mouseWorldPos - player.position).normalized;

        // Calcular ángulo de rotación
        float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;

        // Suavizar la rotación
        float currentAngle = Mathf.Atan2(transform.position.y - player.position.y,
                                         transform.position.x - player.position.x) * Mathf.Rad2Deg;

        float smoothAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);

        // Posicionar la linterna en órbita alrededor del player
        float radians = smoothAngle * Mathf.Deg2Rad;
        Vector3 orbitPosition = player.position + new Vector3(
            Mathf.Cos(radians) * orbitRadius,
            Mathf.Sin(radians) * orbitRadius,
            0f
        );

        transform.position = orbitPosition;

        // Rotar la linterna para que apunte en la dirección correcta
        transform.rotation = Quaternion.Euler(0f, 0f, smoothAngle);
    }

    // --- MANEJO DE BATERÍA ---
    private void HandleBattery()
    {
        if (isFlashlightOn && currentBattery > 0f)
        {
            currentBattery -= batteryDrainRate * Time.deltaTime;

            // Si se acaba la batería, apagar linterna
            if (currentBattery <= 0f)
            {
                currentBattery = 0f;
                TurnOffFlashlight();
            }
        }
        else if (!isFlashlightOn && currentBattery < maxBattery)
        {
            currentBattery += batteryRecoveryRate * Time.deltaTime;
        }

        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
    }

    // --- ACTUALIZAR UI DE BATERÍA ---
    private void UpdateBatteryUI()
    {
        if (batteryBarFill != null)
        {
            batteryBarFill.fillAmount = currentBattery / maxBattery;
        }
    }

    // --- MOSTRAR / OCULTAR BARRA DE BATERÍA ---
    private void HandleBatteryVisibility()
    {
        if (batteryBarCanvas == null) return;

        bool shouldBeVisible = currentBattery < maxBattery;

        if (shouldBeVisible && !isBatteryVisible)
        {
            isBatteryVisible = true;
            batteryBarCanvas.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeCanvas(batteryBarCanvas, 1f, 0.25f));
        }
        else if (!shouldBeVisible && isBatteryVisible)
        {
            isBatteryVisible = false;
            StopAllCoroutines();
            StartCoroutine(FadeCanvas(batteryBarCanvas, 0f, 0.5f, () =>
            {
                batteryBarCanvas.gameObject.SetActive(false);
            }));
        }
    }

    private System.Collections.IEnumerator FadeCanvas(CanvasGroup canvas, float targetAlpha, float duration, System.Action onComplete = null)
    {
        float startAlpha = canvas.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        canvas.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    // --- GETTERS PÚBLICOS ---
    public bool IsFlashlightOn()
    {
        return isFlashlightOn;
    }

    public bool IsFlashing()
    {
        return isFlashing;
    }

    public float GetCurrentBattery()
    {
        return currentBattery;
    }

    public float GetBatteryPercentage()
    {
        return (currentBattery / maxBattery) * 100f;
    }

    public float GetFlashCooldownPercentage()
    {
        return Mathf.Clamp01(1f - (cooldownTimer / flashCooldown));
    }

    // --- OPCIONAL: Recargar batería completamente ---
    public void RechargeBattery()
    {
        currentBattery = maxBattery;
    }
}