// ============= SanityManager.cs ============= 
// Script para manejar la cordura del jugador con efectos visuales mejorados
using UnityEngine;
using UnityEngine.UI;

public class SanityManager : MonoBehaviour
{
    public static SanityManager Instance { get; private set; }

    [Header("Configuración de Cordura")]
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float currentSanity;

    [Header("Regeneración de Cordura")]
    [SerializeField] private float sanityRegenRate = 2f; // Cordura por segundo
    [SerializeField] private float regenDelayAfterDamage = 5f; // Segundos antes de empezar a regenerar

    [Header("UI")]
    [SerializeField] private Image sanityBarFill;
    [SerializeField] private CanvasGroup sanityBarCanvas;

    [Header("Efectos Visuales")]
    [SerializeField] private CanvasGroup vignetteDarkness;

    [Header("Configuración de Transición")]
    [SerializeField] private float alphaTransitionSpeed = 2f; // Velocidad de transición suave

    [Header("Configuración de Latido")]
    [SerializeField] private float heartbeatThreshold = 15f; // Umbral para activar latido (%)
    [SerializeField] private float heartbeatSpeed = 1.2f; // Velocidad del latido (latidos por segundo)
    [SerializeField][Range(0.1f, 0.3f)] private float heartbeatIntensity = 0.15f; // Diferencia entre estados de alpha

    // Variables privadas para el sistema
    private float targetAlpha = 0f;
    private float currentAlpha = 0f;
    private bool isHeartbeatActive = false;
    private float heartbeatTimer = 0f;
    private float timeSinceLastDamage = 0f; // Timer para controlar la regeneración

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentSanity = maxSanity;

        if (vignetteDarkness != null)
        {
            currentAlpha = 0f;
            vignetteDarkness.alpha = 0f;
        }
    }

    private void Update()
    {
        UpdateSanityUI();
        UpdateVignetteEffect();
        UpdateSanityRegeneration();
    }

    public void ReduceSanity(float amount)
    {
        currentSanity -= amount;
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);

        // Reiniciar el timer de daño
        timeSinceLastDamage = 0f;

        if (currentSanity <= 0f)
        {
            OnSanityDepleted();
        }
    }

    public void RestoreSanity(float amount)
    {
        currentSanity += amount;
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);
    }

    private void UpdateSanityRegeneration()
    {
        // Incrementar el tiempo desde el último daño
        timeSinceLastDamage += Time.deltaTime;

        // Si ha pasado suficiente tiempo y la cordura no está al máximo, regenerar
        if (timeSinceLastDamage >= regenDelayAfterDamage && currentSanity < maxSanity)
        {
            currentSanity += sanityRegenRate * Time.deltaTime;
            currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);
        }
    }

    private void UpdateSanityUI()
    {
        if (sanityBarFill != null)
        {
            sanityBarFill.fillAmount = currentSanity / maxSanity;
        }
    }

    private void UpdateVignetteEffect()
    {
        if (vignetteDarkness == null) return;

        float sanityPercentage = (currentSanity / maxSanity) * 100f;

        // Calcular el alpha objetivo basado en la cordura
        targetAlpha = 1f - (currentSanity / maxSanity);

        // Activar latido cuando la cordura está muy baja
        isHeartbeatActive = sanityPercentage <= heartbeatThreshold;

        if (isHeartbeatActive)
        {
            ApplyHeartbeatEffect();
        }
        else
        {
            // Transición suave normal
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * alphaTransitionSpeed);
            vignetteDarkness.alpha = currentAlpha;
        }
    }

    private void ApplyHeartbeatEffect()
    {
        // Incrementar el timer del latido
        heartbeatTimer += Time.deltaTime * heartbeatSpeed;

        // Calcular los dos estados de alpha para el latido
        float sanityPercentage = (currentSanity / maxSanity) * 100f;
        float normalizedSanity = sanityPercentage / heartbeatThreshold; // 0 a 1 dentro del rango de latido

        // Estado de alpha casi máximo (cuando está muy bajo)
        float maxHeartbeatAlpha = Mathf.Lerp(0.7f, 0.95f, 1f - normalizedSanity);

        // Estado de alpha penúltimo (un poco menos oscuro)
        float minHeartbeatAlpha = maxHeartbeatAlpha - heartbeatIntensity;

        // Crear el patrón de latido: lub-dub (dos picos)
        float beat = 0f;
        float t = heartbeatTimer % 1f;

        // Primer latido (LUB) - más fuerte y rápido
        if (t < 0.15f)
        {
            // Usar una curva de seno para un pulso suave
            beat = Mathf.Sin(t / 0.15f * Mathf.PI);
        }
        // Pequeña pausa
        else if (t >= 0.15f && t < 0.25f)
        {
            beat = 0f;
        }
        // Segundo latido (DUB) - más suave y corto
        else if (t >= 0.25f && t < 0.4f)
        {
            // Segundo pulso más débil (80% del primero)
            beat = Mathf.Sin((t - 0.25f) / 0.15f * Mathf.PI) * 0.8f;
        }
        // Período de relajación (diástole)
        else
        {
            beat = 0f;
        }

        // Interpolar entre los dos estados de alpha con el patrón de latido
        float heartbeatAlpha = Mathf.Lerp(minHeartbeatAlpha, maxHeartbeatAlpha, beat);

        // Aplicar transición suave al alpha objetivo (más rápida para el latido)
        currentAlpha = Mathf.Lerp(currentAlpha, heartbeatAlpha, Time.deltaTime * alphaTransitionSpeed * 4f);
        vignetteDarkness.alpha = currentAlpha;
    }

    private void OnSanityDepleted()
    {
        Debug.Log("¡Cordura agotada! Game Over o efecto especial");
        // Aquí puedes agregar lógica de game over o efectos especiales
    }

    // Métodos públicos de utilidad
    public float GetCurrentSanity() => currentSanity;
    public float GetSanityPercentage() => (currentSanity / maxSanity) * 100f;
    public float GetMaxSanity() => maxSanity;
    public bool IsHeartbeatActive() => isHeartbeatActive;
    public float GetTimeSinceLastDamage() => timeSinceLastDamage; // Útil para debugging
}