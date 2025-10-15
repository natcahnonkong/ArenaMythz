using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float rollSpeed = 8f;
    public float rotationSpeed = 10f;

    [Header("Roll Settings")]
    public float rollDuration = 0.6f;
    public float rollCooldown = 0.5f;

    [Header("Jump Settings")]
    public float jumpHeight = 2f;

    [Header("Lock-on")]
    public float lockOnRange = 20f;
    public LayerMask enemyLayer;
    public Transform lockOnIndicator;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Animator animator;
    private Vector3 moveDirection;
    private bool isRolling;
    private bool canRoll = true;
    private float rollTimer;
    private bool isSprinting;

    private Transform currentTarget;
    private bool isLockedOn;

    private float verticalVelocity;
    private float gravity = -20f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleLockOn();
        HandleJump();

        if (!isRolling)
        {
            HandleMovement();
            HandleRoll();
        }
        else
        {
            UpdateRoll();
        }

        ApplyGravity();
        UpdateAnimations();
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(h, v).normalized;

        isSprinting = Input.GetKey(KeyCode.LeftShift) && input.magnitude > 0 && !isLockedOn;

        if (input.magnitude > 0)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 desiredMoveDir = forward * input.y + right * input.x;

            if (isLockedOn)
            {
                moveDirection = desiredMoveDir * walkSpeed;

                if (currentTarget != null)
                {
                    Vector3 lookDir = currentTarget.position - transform.position;
                    lookDir.y = 0;
                    if (lookDir != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    }
                }
            }
            else
            {
                float speed = isSprinting ? sprintSpeed : walkSpeed;
                moveDirection = desiredMoveDir * speed;

                if (desiredMoveDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        controller.Move(moveDirection * Time.deltaTime);
    }

    void HandleRoll()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && canRoll && !isSprinting)
        {
            StartRoll();
        }

        if (!canRoll)
        {
            rollTimer += Time.deltaTime;
            if (rollTimer >= rollCooldown)
            {
                canRoll = true;
                rollTimer = 0;
            }
        }
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void StartRoll()
    {
        isRolling = true;
        canRoll = false;
        rollTimer = 0;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h == 0 && v == 0)
        {
            moveDirection = transform.forward * rollSpeed;
        }
        else
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            moveDirection = (forward * v + right * h).normalized * rollSpeed;
        }
    }

    void UpdateRoll()
    {
        rollTimer += Time.deltaTime;

        controller.Move(moveDirection * Time.deltaTime);

        if (rollTimer >= rollDuration)
        {
            isRolling = false;
            rollTimer = 0;
        }
    }

    void HandleLockOn()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (!isLockedOn)
            {
                FindNearestEnemy();
            }
            else
            {
                ReleaseLockOn();
            }
        }

        if (isLockedOn && currentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.position);
            if (dist > lockOnRange)
            {
                ReleaseLockOn();
            }

            if (lockOnIndicator != null)
            {
                lockOnIndicator.position = currentTarget.position + Vector3.up * 2f;
            }
        }
    }

    void FindNearestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, lockOnRange, enemyLayer);

        if (enemies.Length == 0) return;

        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (Collider enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);

            Vector3 screenPoint = Camera.main.WorldToViewportPoint(enemy.transform.position);
            bool onScreen = screenPoint.z > 0 && screenPoint.x > 0.2f && screenPoint.x < 0.8f && screenPoint.y > 0.2f && screenPoint.y < 0.8f;

            if (dist < minDist && onScreen)
            {
                minDist = dist;
                nearest = enemy.transform;
            }
        }

        if (nearest != null)
        {
            currentTarget = nearest;
            isLockedOn = true;

            if (lockOnIndicator != null)
            {
                lockOnIndicator.gameObject.SetActive(true);
            }
        }
    }

    void ReleaseLockOn()
    {
        isLockedOn = false;
        currentTarget = null;

        if (lockOnIndicator != null)
        {
            lockOnIndicator.gameObject.SetActive(false);
        }
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        Vector3 localVelocity = transform.InverseTransformDirection(moveDirection);

        animator.SetFloat("VelocityX", localVelocity.x, 0.1f, Time.deltaTime);
        animator.SetFloat("VelocityZ", localVelocity.z, 0.1f, Time.deltaTime);
        animator.SetBool("IsRolling", isRolling);
        animator.SetBool("IsSprinting", isSprinting);
        animator.SetBool("IsLockedOn", isLockedOn);
        animator.SetBool("IsGrounded", controller.isGrounded);
        animator.SetFloat("VerticalVelocity", verticalVelocity);
    }

    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);

        if (isLockedOn && currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}