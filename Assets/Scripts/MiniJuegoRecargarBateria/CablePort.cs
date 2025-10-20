using UnityEngine;
using UnityEngine.UI;

public class CablePort : MonoBehaviour
{
    [SerializeField] private Image portImage;
    [SerializeField] private Image portOutline;
    [SerializeField] private float detectionRadius = 80f; // Aumentado para mejor detección

    private int portId;
    private Color portColor;
    private bool isConnected = false;
    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (portImage == null)
            portImage = GetComponent<Image>();
    }

    public void Initialize(int id, Color color)
    {
        portId = id;
        portColor = color;

        if (portImage != null)
        {
            portImage.color = portColor;
        }

        if (portOutline != null)
        {
            portOutline.color = new Color(portColor.r, portColor.g, portColor.b, 0.5f);
        }

        isConnected = false;
    }

    public bool IsPositionInside(Vector2 screenPosition)
    {
        float distance;
        return IsPositionInside(screenPosition, out distance);
    }

    public bool IsPositionInside(Vector2 screenPosition, out float distance)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            screenPosition,
            canvas.worldCamera,
            out localPoint
        );

        distance = localPoint.magnitude;
        return distance <= detectionRadius;
    }

    public void SetConnected(bool connected)
    {
        isConnected = connected;

        // Feedback visual mejorado
        if (portOutline != null)
        {
            if (connected)
            {
                portOutline.color = Color.green;
                portOutline.transform.localScale = Vector3.one * 1.2f;
            }
            else
            {
                portOutline.color = new Color(portColor.r, portColor.g, portColor.b, 0.5f);
                portOutline.transform.localScale = Vector3.one;
            }
        }

        // Opcional: cambiar el color del puerto cuando está conectado
        if (portImage != null)
        {
            if (connected)
            {
                // Puerto más brillante cuando está conectado
                portImage.color = new Color(
                    Mathf.Min(portColor.r * 1.3f, 1f),
                    Mathf.Min(portColor.g * 1.3f, 1f),
                    Mathf.Min(portColor.b * 1.3f, 1f),
                    portColor.a
                );
            }
            else
            {
                portImage.color = portColor;
            }
        }
    }

    public void ShowIncorrectFeedback()
    {
        StartCoroutine(IncorrectFeedbackCoroutine());
    }

    private System.Collections.IEnumerator IncorrectFeedbackCoroutine()
    {
        // Guardar colores originales
        Color originalPortColor = portImage != null ? portImage.color : Color.white;
        Color originalOutlineColor = portOutline != null ? portOutline.color : Color.white;

        // Cambiar a rojo
        if (portImage != null)
            portImage.color = Color.red;

        if (portOutline != null)
        {
            portOutline.color = Color.red;
            portOutline.transform.localScale = Vector3.one * 1.3f;
        }

        yield return new WaitForSeconds(0.5f);

        // Restaurar colores originales
        if (portImage != null)
            portImage.color = originalPortColor;

        if (portOutline != null)
        {
            portOutline.color = originalOutlineColor;
            portOutline.transform.localScale = Vector3.one;
        }
    }

    public bool IsConnected()
    {
        return isConnected;
    }

    public int GetPortId()
    {
        return portId;
    }

    public Color GetPortColor()
    {
        return portColor;
    }
}