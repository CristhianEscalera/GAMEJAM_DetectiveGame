using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Rendering.Universal;

public class FlashlightController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject flashlightVisual;
    [SerializeField] private Image batteryBarFill;
    [SerializeField] private CanvasGroup batteryBarCanvas;

    // NUEVO: Referencia al script de movimiento del player
    [SerializeField] private PlayerMovement2D playerMovement;

    [Header("Referencias de Destello")]
    [SerializeField] private Light2D flashLight2D;
    [SerializeField] private PolygonCollider2D flashCollider;
    [SerializeField] private ParticleSystem flashParticles;
    [SerializeField] private AudioSource flashAudioSource;
    [SerializeField] private AudioClip flashSound;

    [Header("Configuración de Batería")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float batteryDrainRate = 10f;
    [SerializeField] private float batteryRecoveryRate = 5f;

    [Header("Configuración de Destello")]
    [SerializeField] private float flashDuration = 2f;
    [SerializeField] private float flashIntensityMultiplier = 3f;
    [SerializeField] private float flashBatteryCost = 20f;
    [SerializeField] private float flashCooldown = 1f;

    [Header("Configuración de Movimiento con Punto de Anclaje")]
    [SerializeField] private Transform anchorPointRight;
    [SerializeField] private Transform anchorPointLeft;
    [SerializeField] private Vector2 anchorOffsetRight = new Vector2(-0.3f, 0.2f);
    [SerializeField] private Vector2 anchorOffsetLeft = new Vector2(0.3f, 0.2f);
    [SerializeField] private float flashlightLength = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    private float currentBattery;
    private bool isFlashlightOn = false;
    private bool isBatteryVisible = false;
    private Vector3 targetDirection;

    private bool isFlashing = false;
    private float flashTimer = 0f;
    private float cooldownTimer = 0f;
    private float normalIntensity = 1f;

    private Vector3 currentAnchorPosition;
    private float currentAngle = 0f;
    private bool isFacingRight = true;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (player == null)
            player = transform.parent;

        // NUEVO: Obtener referencia al PlayerMovement2D si no está asignada
        if (playerMovement == null && player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement2D>();
        }

        currentBattery = maxBattery;

        if (flashLight2D != null)
            normalIntensity = flashLight2D.intensity;

        if (flashCollider != null)
            flashCollider.enabled = false;

        if (flashlightVisual != null)
            flashlightVisual.SetActive(false);

        UpdateAnchorPoint();

        if (isFacingRight)
            currentAngle = 0f;
        else
            currentAngle = 180f;
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
            UpdateAnchorPoint();
            FollowMouseWithAnchor();
        }
    }

    private void UpdateAnchorPoint()
    {
        if (player == null) return;

        bool wasFacingRight = isFacingRight;

        if (player.localScale.x > 0)
        {
            isFacingRight = true;

            if (anchorPointRight != null)
            {
                currentAnchorPosition = anchorPointRight.position;
            }
            else
            {
                currentAnchorPosition = player.position + new Vector3(anchorOffsetRight.x, anchorOffsetRight.y, 0f);
            }
        }
        else
        {
            isFacingRight = false;

            if (anchorPointLeft != null)
            {
                currentAnchorPosition = anchorPointLeft.position;
            }
            else
            {
                currentAnchorPosition = player.position + new Vector3(anchorOffsetLeft.x, anchorOffsetLeft.y, 0f);
            }
        }

        if (wasFacingRight != isFacingRight)
        {
            if (isFacingRight)
            {
                currentAngle = 0f;
            }
            else
            {
                currentAngle = 180f;
            }
        }
    }

    private void FollowMouseWithAnchor()
    {
        if (mainCamera == null || player == null) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector3 anchorWorldPos = currentAnchorPosition;
        anchorWorldPos.z = 0f;

        Vector3 directionToMouse = (mouseWorldPos - anchorWorldPos).normalized;

        float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;

        if (targetAngle < 0f)
            targetAngle += 360f;

        float clampedAngle = targetAngle;

        if (isFacingRight)
        {
            if (targetAngle > 90f && targetAngle < 270f)
            {
                float distToMin = Mathf.Abs(Mathf.DeltaAngle(targetAngle, 90f));
                float distToMax = Mathf.Abs(Mathf.DeltaAngle(targetAngle, 270f));

                if (distToMin < distToMax)
                    clampedAngle = 90f;
                else
                    clampedAngle = 270f;
            }
        }
        else
        {
            if (targetAngle < 90f || targetAngle > 270f)
            {
                float distToMin = Mathf.Abs(Mathf.DeltaAngle(targetAngle, 90f));
                float distToMax = Mathf.Abs(Mathf.DeltaAngle(targetAngle, 270f));

                if (distToMin < distToMax)
                    clampedAngle = 90f;
                else
                    clampedAngle = 270f;
            }
        }

        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, clampedAngle));

        if (angleDifference > 0.1f)
        {
            currentAngle = Mathf.LerpAngle(currentAngle, clampedAngle, rotationSpeed * Time.deltaTime);
        }
        else
        {
            currentAngle = clampedAngle;
        }

        float radians = currentAngle * Mathf.Deg2Rad;
        Vector3 flashlightTipPosition = anchorWorldPos + new Vector3(
            Mathf.Cos(radians) * flashlightLength,
            Mathf.Sin(radians) * flashlightLength,
            0f
        );

        transform.position = flashlightTipPosition;
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    // --- CORREGIDO: Ahora verifica si el player está escalando ---
    private void HandleFlashlightInput()
    {
        // Verificar si puede usar la linterna (no diálogo, no minigame, NO ESCALANDO)
        bool canUseFlashlight = (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive())
                            && (CableMinigameManager.Instance == null || !CableMinigameManager.Instance.IsMinigameActive())
                            && (playerMovement == null || !playerMovement.IsClimbing()); // NUEVO

        if (!canUseFlashlight)
        {
            if (isFlashlightOn)
                TurnOffFlashlight();
            return;
        }

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

    // --- CORREGIDO: También verifica escalada para el destello ---
    private void HandleFlashInput()
    {
        bool canUseFlash = isFlashlightOn
                        && !isFlashing
                        && cooldownTimer <= 0f
                        && currentBattery >= flashBatteryCost
                        && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive())
                        && (CableMinigameManager.Instance == null || !CableMinigameManager.Instance.IsMinigameActive())
                        && (playerMovement == null || !playerMovement.IsClimbing()); // NUEVO

        if (canUseFlash && Input.GetMouseButtonDown(1))
        {
            ActivateFlash();
        }
    }

    private void ActivateFlash()
    {
        isFlashing = true;
        flashTimer = flashDuration;
        cooldownTimer = flashCooldown;

        currentBattery -= flashBatteryCost;
        currentBattery = Mathf.Max(0f, currentBattery);

        if (flashLight2D != null)
        {
            flashLight2D.intensity = normalIntensity * flashIntensityMultiplier;
        }

        if (flashCollider != null)
        {
            flashCollider.enabled = true;
        }

        if (flashParticles != null)
        {
            flashParticles.Play();
        }

        if (flashAudioSource != null && flashSound != null)
        {
            flashAudioSource.PlayOneShot(flashSound);
        }
    }

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

    private void DeactivateFlash()
    {
        isFlashing = false;

        if (flashLight2D != null)
        {
            flashLight2D.intensity = normalIntensity;
        }

        if (flashCollider != null)
        {
            flashCollider.enabled = false;
        }
    }

    private void UpdateCooldown()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    private void TurnOnFlashlight()
    {
        isFlashlightOn = true;
        if (flashlightVisual != null)
            flashlightVisual.SetActive(true);
    }

    private void TurnOffFlashlight()
    {
        isFlashlightOn = false;
        if (flashlightVisual != null)
            flashlightVisual.SetActive(false);

        if (isFlashing)
        {
            DeactivateFlash();
        }
    }

    private void HandleBattery()
    {
        if (isFlashlightOn && currentBattery > 0f)
        {
            currentBattery -= batteryDrainRate * Time.deltaTime;

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

    private void UpdateBatteryUI()
    {
        if (batteryBarFill != null)
        {
            batteryBarFill.fillAmount = currentBattery / maxBattery;
        }
    }

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

    public void RechargeBattery()
    {
        currentBattery = maxBattery;
    }
}