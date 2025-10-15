using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    private PlayerController playerController;

    [Header("Camera Settings")]
    public float normalDistance = 5f;
    public float normalHeight = 2f;
    public float lockOnDistance = 4f;
    public float lockOnHeight = 1.5f;

    [Header("Rotation")]
    public float mouseSensitivity = 3f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    public float smoothSpeed = 10f;

    [Header("Lock-on Rotation")]
    public float lockOnSmoothSpeed = 8f;

    [Header("Collision")]
    public LayerMask collisionLayers;
    public float collisionOffset = 0.3f;

    private float currentX;
    private float currentY;
    private float targetDistance;
    private float targetHeight;
    private Transform lockOnTarget;

    void Start()
    {
        currentX = transform.eulerAngles.y;
        currentY = transform.eulerAngles.x;
        targetDistance = normalDistance;
        targetHeight = normalHeight;

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        if (playerController != null)
        {
            lockOnTarget = playerController.GetCurrentTarget();
        }

        if (lockOnTarget != null)
        {
            HandleLockOnCamera();
        }
        else
        {
            HandleNormalCamera();
        }
    }

    void HandleNormalCamera()
    {
        targetDistance = normalDistance;
        targetHeight = normalHeight;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        currentX += mouseX;
        currentY -= mouseY;
        currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);

        Vector3 direction = new Vector3(0, 0, -targetDistance);
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 targetPosition = player.position + Vector3.up * targetHeight + rotation * direction;

        RaycastHit hit;
        Vector3 startPos = player.position + Vector3.up * targetHeight;
        Vector3 dir = targetPosition - startPos;
        float dist = dir.magnitude;

        if (Physics.Raycast(startPos, dir.normalized, out hit, dist, collisionLayers))
        {
            targetPosition = hit.point + hit.normal * collisionOffset;
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, smoothSpeed * Time.deltaTime);
    }

    void HandleLockOnCamera()
    {
        targetDistance = lockOnDistance;
        targetHeight = lockOnHeight;

        Vector3 midPoint = (player.position + lockOnTarget.position) / 2f;
        midPoint.y = player.position.y + targetHeight;

        Vector3 offset = -player.forward * targetDistance * 0.5f + Vector3.up * targetHeight;
        Vector3 targetPosition = player.position + offset;

        RaycastHit hit;
        Vector3 startPos = player.position + Vector3.up * targetHeight;
        Vector3 dir = targetPosition - startPos;
        float dist = dir.magnitude;

        if (Physics.Raycast(startPos, dir.normalized, out hit, dist, collisionLayers))
        {
            targetPosition = hit.point + hit.normal * collisionOffset;
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, lockOnSmoothSpeed * Time.deltaTime);

        Vector3 lookTarget = midPoint;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, lockOnSmoothSpeed * Time.deltaTime);

        currentX = transform.eulerAngles.y;
        currentY = transform.eulerAngles.x;
        if (currentY > 180) currentY -= 360;
    }
}