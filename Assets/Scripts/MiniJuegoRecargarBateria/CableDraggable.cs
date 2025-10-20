using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CableDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image cableImage;
    [SerializeField] private RectTransform cableEndPoint;

    private int cableId;
    private Color cableColor;
    private CableMinigameManager manager;
    private Canvas canvas;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private CablePort currentPort;
    private bool isDragging = false;
    private Vector2 dragOffset;
    private int originalSiblingIndex; // Guardar el índice original en la jerarquía

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (cableImage == null)
            cableImage = GetComponent<Image>();

        if (cableImage == null)
        {
            Debug.LogError("CableDraggable: No se encontró el componente Image en " + gameObject.name);
        }
    }

    public void Initialize(int id, Color color, CableMinigameManager mgr)
    {
        cableId = id;
        cableColor = color;
        manager = mgr;

        if (cableImage == null)
            cableImage = GetComponent<Image>();

        if (cableImage != null)
        {
            cableImage.color = cableColor;
        }
        else
        {
            Debug.LogError("CableDraggable.Initialize: cableImage sigue siendo null en " + gameObject.name);
        }

        // Guardar el índice de hermano original
        StartCoroutine(SaveOriginalPositionDelayed());
    }

    private System.Collections.IEnumerator SaveOriginalPositionDelayed()
    {
        yield return new WaitForEndOfFrame();
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
            originalSiblingIndex = transform.GetSiblingIndex();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;

        // Calcular el offset entre el mouse y el centro del cable
        Vector2 localPointerPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPosition
        );

        dragOffset = rectTransform.anchoredPosition - localPointerPosition;

        // Desconectar del puerto actual si estaba conectado
        if (currentPort != null)
        {
            manager.OnCableDisconnected(currentPort);
            currentPort = null;
        }

        // Traer al frente
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // Mover el cable con el mouse, aplicando el offset
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        rectTransform.anchoredPosition = localPoint + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        // Verificar si se soltó sobre un puerto
        CablePort targetPort = manager.GetPortAtPosition(eventData.position);

        if (targetPort != null && !targetPort.IsConnected())
        {
            // Verificar si el color coincide ANTES de conectar
            if (targetPort.GetPortId() == cableId)
            {
                // Conexión correcta
                currentPort = targetPort;
                SnapToPortPermanent(targetPort);
                manager.OnCableConnected(cableId, targetPort);
            }
            else
            {
                // Color incorrecto - Mostrar feedback y REINICIAR
                StartCoroutine(ShowIncorrectConnectionFeedback(targetPort));
            }
        }
        else
        {
            // No hay puerto o está ocupado - volver a posición original
            ReturnToOriginalPosition();
        }
    }

    private System.Collections.IEnumerator ShowIncorrectConnectionFeedback(CablePort wrongPort)
    {
        // Mostrar feedback rojo en el puerto
        wrongPort.ShowIncorrectFeedback();

        // Devolver el cable al panel inmediatamente
        ReturnToOriginalPosition();

        // Esperar un momento para que el jugador vea el feedback
        yield return new WaitForSeconds(0.5f);

        // Reiniciar el minijuego
        manager.OnIncorrectConnection();
    }

    private void SnapToPort(CablePort port)
    {
        // Ajustar el cable al puerto sin cambiar el padre aún
        RectTransform portRect = port.GetComponent<RectTransform>();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, portRect.position),
            canvas.worldCamera,
            out localPoint
        );

        rectTransform.anchoredPosition = localPoint;
    }

    private void SnapToPortPermanent(CablePort port)
    {
        // Cambiar el padre al puerto para que se quede ahí permanentemente
        RectTransform portRect = port.GetComponent<RectTransform>();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, portRect.position),
            canvas.worldCamera,
            out localPoint
        );

        rectTransform.anchoredPosition = localPoint;

        // Cambiar el padre al puerto
        transform.SetParent(port.transform);
        rectTransform.anchoredPosition = Vector2.zero; // Centrar en el puerto
    }

    private void ReturnToOriginalPosition()
    {
        currentPort = null;

        // Volver al contenedor original si no está ahí
        if (transform.parent != manager.GetCablesContainer())
        {
            transform.SetParent(manager.GetCablesContainer());
        }

        // Restaurar el índice de hermano original (posición en la jerarquía)
        transform.SetSiblingIndex(originalSiblingIndex);

        // Usar Coroutine para asegurar que el layout se actualice primero
        StartCoroutine(ReturnToPositionCoroutine());
    }

    private System.Collections.IEnumerator ReturnToPositionCoroutine()
    {
        // Resetear la posición para que el Layout Group la recalcule
        rectTransform.anchoredPosition = Vector2.zero;

        // Esperar un frame para que Unity procese el cambio de padre
        yield return null;

        // Forzar actualización del layout del contenedor
        RectTransform container = manager.GetCablesContainer() as RectTransform;
        if (container != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }

        // Esperar otro frame para asegurar que el layout esté completamente actualizado
        yield return null;
    }

    public void ResetCable()
    {
        ReturnToOriginalPosition();
    }

    public int GetCableId()
    {
        return cableId;
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    public bool IsConnected()
    {
        return currentPort != null;
    }
}