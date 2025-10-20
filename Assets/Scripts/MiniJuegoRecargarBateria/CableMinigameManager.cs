using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class CableMinigameManager : MonoBehaviour
{
    public static CableMinigameManager Instance { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private Transform cablesContainer;
    [SerializeField] private Transform portsContainer;
    [SerializeField] private Text timerText;
    [SerializeField] private Text instructionsText;

    [Header("Prefabs")]
    [SerializeField] private GameObject cablePrefab;
    [SerializeField] private GameObject portPrefab;

    [Header("Configuración")]
    [SerializeField] private float minigameTime = 30f;
    [SerializeField]
    private Color[] cableColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        new Color(0.5f, 0f, 0.5f) // Morado
    };

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField] private AudioClip connectSound;

    private List<CableDraggable> cables = new List<CableDraggable>();
    private List<CablePort> ports = new List<CablePort>();
    private float currentTime;
    private bool isMinigameActive = false;
    private int cablesConnected = 0;
    private AudioSource audioSource;

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (minigamePanel != null)
            minigamePanel.SetActive(false);
    }

    private void Update()
    {
        // Abrir minijuego con R
        if (Input.GetKeyDown(KeyCode.R) && !isMinigameActive)
        {
            bool canOpenMinigame = DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive();

            if (canOpenMinigame)
            {
                OpenMinigame();
            }
        }

        // Cerrar con ESC
        if (Input.GetKeyDown(KeyCode.Escape) && isMinigameActive)
        {
            CloseMinigame();
        }

        // Actualizar timer
        if (isMinigameActive)
        {
            UpdateTimer();
        }
    }

    public void OpenMinigame()
    {
        isMinigameActive = true;
        currentTime = minigameTime;
        cablesConnected = 0;

        if (minigamePanel != null)
            minigamePanel.SetActive(true);

        SetupMinigame();

        if (instructionsText != null)
        {
            instructionsText.text = "Conecta todos los cables a sus puertos correctos";
            instructionsText.color = Color.white;
        }
    }

    public void CloseMinigame()
    {
        isMinigameActive = false;

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        CleanupMinigame();
    }

    private void SetupMinigame()
    {
        CleanupMinigame();

        if (portPrefab == null)
        {
            Debug.LogError("CableMinigameManager: portPrefab no está asignado!");
            return;
        }

        if (cablePrefab == null)
        {
            Debug.LogError("CableMinigameManager: cablePrefab no está asignado!");
            return;
        }

        // Crear puertos en orden
        for (int i = 0; i < cableColors.Length; i++)
        {
            GameObject portObj = Instantiate(portPrefab, portsContainer);

            Image portImage = portObj.GetComponent<Image>();
            if (portImage == null)
            {
                portImage = portObj.AddComponent<Image>();
                Debug.LogWarning("Se agregó Image al puerto " + i);
            }

            CablePort port = portObj.GetComponent<CablePort>();
            if (port == null)
                port = portObj.AddComponent<CablePort>();

            port.Initialize(i, cableColors[i]);
            ports.Add(port);
        }

        // Crear cables en orden aleatorio
        List<int> shuffledIndices = Enumerable.Range(0, cableColors.Length).OrderBy(x => Random.value).ToList();

        for (int i = 0; i < shuffledIndices.Count; i++)
        {
            int colorIndex = shuffledIndices[i];
            GameObject cableObj = Instantiate(cablePrefab, cablesContainer);

            Image cableImage = cableObj.GetComponent<Image>();
            if (cableImage == null)
            {
                cableImage = cableObj.AddComponent<Image>();
                Debug.LogWarning("Se agregó Image al cable " + i);
            }

            CableDraggable cable = cableObj.GetComponent<CableDraggable>();
            if (cable == null)
                cable = cableObj.AddComponent<CableDraggable>();

            cable.Initialize(colorIndex, cableColors[colorIndex], this);
            cables.Add(cable);
        }

        // Forzar recalculo del layout después de crear los cables
        StartCoroutine(ForceLayoutRebuild());
    }

    private System.Collections.IEnumerator ForceLayoutRebuild()
    {
        yield return new WaitForEndOfFrame();

        // Forzar actualización del layout
        if (cablesContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(cablesContainer as RectTransform);
        }

        if (portsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(portsContainer as RectTransform);
        }
    }

    private void CleanupMinigame()
    {
        foreach (var cable in cables)
        {
            if (cable != null)
                Destroy(cable.gameObject);
        }
        cables.Clear();

        foreach (var port in ports)
        {
            if (port != null)
                Destroy(port.gameObject);
        }
        ports.Clear();
    }

    private void UpdateTimer()
    {
        currentTime -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = $"Tiempo: {Mathf.CeilToInt(currentTime)}s";

            if (currentTime <= 10f)
                timerText.color = Color.red;
            else
                timerText.color = Color.white;
        }

        if (currentTime <= 0f)
        {
            FailMinigame();
        }
    }

    public void OnCableConnected(int cableId, CablePort port)
    {
        // Ya verificamos que coincide en CableDraggable, solo confirmamos
        if (port.GetPortId() == cableId)
        {
            port.SetConnected(true);
            cablesConnected++;

            PlaySound(connectSound);

            // Pequeño delay para feedback visual
            if (cablesConnected >= cableColors.Length)
            {
                Invoke(nameof(CompleteMinigame), 0.3f);
            }
        }
        else
        {
            // Esto no debería pasar, pero por seguridad
            Debug.LogWarning("Intento de conexión incorrecta detectado");
        }
    }

    public void OnIncorrectConnection()
    {
        // Cable conectado al puerto incorrecto - REINICIAR
        PlaySound(failSound);

        if (instructionsText != null)
        {
            instructionsText.text = "¡Cable incorrecto! Reiniciando...";
            instructionsText.color = Color.red;
        }

        Invoke(nameof(RestartMinigame), 1f);
    }

    public void OnCableDisconnected(CablePort port)
    {
        if (port != null && port.IsConnected())
        {
            port.SetConnected(false);
            cablesConnected = Mathf.Max(0, cablesConnected - 1);
        }
    }

    public CablePort GetPortAtPosition(Vector2 position)
    {
        // Buscar el puerto más cercano dentro del radio
        CablePort closestPort = null;
        float closestDistance = float.MaxValue;

        foreach (var port in ports)
        {
            if (port.IsPositionInside(position, out float distance))
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPort = port;
                }
            }
        }

        return closestPort;
    }

    private void CompleteMinigame()
    {
        PlaySound(successSound);

        // Recargar batería de la linterna
        FlashlightController flashlight = FindObjectOfType<FlashlightController>();
        if (flashlight != null)
        {
            flashlight.RechargeBattery();
        }

        if (instructionsText != null)
        {
            instructionsText.text = "¡Batería recargada!";
            instructionsText.color = Color.green;
        }

        Invoke(nameof(CloseMinigame), 1.5f);
    }

    private void FailMinigame()
    {
        PlaySound(failSound);

        if (instructionsText != null)
        {
            instructionsText.text = "¡Se acabó el tiempo! Reiniciando...";
            instructionsText.color = Color.red;
        }

        Invoke(nameof(RestartMinigame), 1f);
    }

    private void RestartMinigame()
    {
        currentTime = minigameTime;
        cablesConnected = 0;

        if (instructionsText != null)
        {
            instructionsText.text = "Conecta todos los cables a sus puertos correctos";
            instructionsText.color = Color.white;
        }

        SetupMinigame();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public bool IsMinigameActive()
    {
        return isMinigameActive;
    }

    public Transform GetCablesContainer()
    {
        return cablesContainer;
    }
}