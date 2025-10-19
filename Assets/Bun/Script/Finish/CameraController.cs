using UnityEngine;

/// <summary>
/// Third-person camera system with Lock-on support (Elden Ring style)
/// ระบบกล้องมุมมองบุคคลที่สามพร้อม Lock-on แบบ Souls-like
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerController playerController;

    [Header("Normal Camera Settings")]
    [SerializeField] private float normalDistance = 5f;
    [SerializeField] private float normalHeight = 2f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Lock-On Camera Settings")]
    [SerializeField] private float lockOnDistance = 4f;
    [SerializeField] private float lockOnHeight = 1.5f;
    [SerializeField] private float lockOnSmoothSpeed = 8f;
    [SerializeField] private float lockOnRotationSpeed = 5f;

    [Header("Rotation Limits")]
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float collisionOffset = 0.3f;
    [SerializeField] private float collisionSmoothSpeed = 15f;

    [Header("Lock-On Behavior")]
    [SerializeField] private float lockOnSideOffset = 0.5f; // ขยับกล้องไปข้างเล็กน้อย
    [SerializeField] private bool autoFollowTarget = true; // ติดตามเป้าหมายอัตโนมัติ

    // State
    private float currentX;
    private float currentY;
    private float currentDistance;
    private Vector3 currentVelocity;

    // Lock-on state
    private Transform currentTarget;
    private bool isLockedOn;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("CameraController: Player reference not set!");
            enabled = false;
            return;
        }

        if (playerController == null)
            playerController = player.GetComponent<PlayerController>();

        // Initialize rotation
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;

        currentDistance = normalDistance;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Get lock-on target from player
        if (playerController != null)
        {
            currentTarget = playerController.GetCurrentTarget();
            isLockedOn = currentTarget != null;
        }

        // Update camera based on mode
        if (isLockedOn && currentTarget != null)
        {
            HandleLockOnCamera();
        }
        else
        {
            HandleNormalCamera();
        }
    }

    #region Normal Camera (Free Look)

    void HandleNormalCamera()
    {
        // Mouse input for rotation
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        currentX += mouseX;
        currentY -= mouseY;
        currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);

        // Calculate desired camera position
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 direction = rotation * Vector3.back;

        Vector3 targetPosition = player.position + Vector3.up * normalHeight;
        Vector3 desiredPosition = targetPosition + direction * normalDistance;

        // Handle collision
        desiredPosition = HandleCameraCollision(targetPosition, desiredPosition, normalDistance);

        // Smooth camera movement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / smoothSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, smoothSpeed * Time.deltaTime);
    }

    #endregion

    #region Lock-On Camera

    void HandleLockOnCamera()
    {
        // Calculate positions
        Vector3 playerPos = player.position + Vector3.up * lockOnHeight;
        Vector3 targetPos = currentTarget.position + Vector3.up * lockOnHeight;

        // Calculate direction from player to target
        Vector3 playerToTarget = targetPos - playerPos;
        float distanceToTarget = playerToTarget.magnitude;

        // Calculate midpoint for camera to look at
        Vector3 midPoint = Vector3.Lerp(playerPos, targetPos, 0.5f);

        // Calculate camera position behind player
        Vector3 directionToCamera = (playerPos - targetPos).normalized;
        directionToCamera.y = 0; // Keep on horizontal plane
        directionToCamera.Normalize();

        // Add slight side offset for better view
        Vector3 sideOffset = Vector3.Cross(directionToCamera, Vector3.up) * lockOnSideOffset;

        // Position camera behind and to the side of player
        Vector3 targetPosition = playerPos + directionToCamera * lockOnDistance + Vector3.up * 0.5f + sideOffset;

        // Handle collision
        targetPosition = HandleCameraCollision(playerPos, targetPosition, lockOnDistance);

        // Calculate look direction
        Vector3 lookDirection = midPoint - targetPosition;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        // Smooth camera movement and rotation
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, 1f / lockOnSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lockOnRotationSpeed * Time.deltaTime);

        // Update camera angles for smooth transition out of lock-on
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
        if (currentY > 180) currentY -= 360;

        // Allow manual camera adjustment while locked on
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 0.5f;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 0.5f;

            currentX += mouseX;
            currentY -= mouseY;
            currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
        }
    }

    #endregion

    #region Collision Detection

    Vector3 HandleCameraCollision(Vector3 origin, Vector3 desiredPosition, float targetDistance)
    {
        Vector3 direction = desiredPosition - origin;
        float distance = direction.magnitude;

        RaycastHit hit;
        if (Physics.Raycast(origin, direction.normalized, out hit, distance, collisionLayers))
        {
            // Camera hit something, move it closer
            float adjustedDistance = hit.distance - collisionOffset;
            currentDistance = Mathf.Lerp(currentDistance, adjustedDistance, collisionSmoothSpeed * Time.deltaTime);
            currentDistance = Mathf.Max(currentDistance, 0.5f); // Minimum distance

            return origin + direction.normalized * currentDistance;
        }
        else
        {
            // No collision, return to target distance
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, collisionSmoothSpeed * Time.deltaTime);
            return origin + direction.normalized * currentDistance;
        }
    }

    #endregion

    #region Debug

    void OnDrawGizmos()
    {
        if (player == null) return;

        // Draw camera position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Draw line to player
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, player.position);

        // Draw lock-on info
        if (isLockedOn && currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(player.position, currentTarget.position);

            // Draw camera look target
            Vector3 midPoint = (player.position + currentTarget.position) * 0.5f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(midPoint, 0.3f);
        }
    }

    #endregion
}