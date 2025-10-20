using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 0.15f;
    [SerializeField] private Vector3 offset;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);
    }
}
