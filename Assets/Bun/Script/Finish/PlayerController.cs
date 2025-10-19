using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
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
    public float rollStartDelay = 0.1f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 2f;
    private float gravity = -20f;
    private float verticalVelocity;

    [Header("Lock-on")]
    public float lockOnRange = 20f;
    public LayerMask enemyLayer;
    public Transform lockOnIndicator;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Animator animator;

    private Vector3 moveDir, rollDir;
    private bool isRolling, canRoll = true, rollStarted, isSprinting;
    private float rollTimer;

    private Transform target;
    private bool lockedOn;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        cameraTransform ??= Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleLockOn();
        ApplyGravity();
        HandleJump();

        if (!isRolling)
        {
            HandleMovement();
            HandleRoll();
        }

        UpdateAnimations();
    }

    void LateUpdate()
    {
        if (isRolling) UpdateRoll();
    }

    #region Movement
    void HandleMovement()
    {
        Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input.Normalize();

        isSprinting = Input.GetKey(KeyCode.LeftShift) && input.magnitude > 0;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = right.y = 0;
        forward.Normalize(); right.Normalize();

        Vector3 dir = forward * input.y + right * input.x;
        float speed = isSprinting ? sprintSpeed : walkSpeed;

        if (lockedOn && target)
        {
            Vector3 lookDir = (target.position - transform.position).normalized;
            lookDir.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), rotationSpeed * Time.deltaTime);
        }
        else if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
        }

        moveDir = dir * speed;
        controller.Move(moveDir * Time.deltaTime);
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded && !isRolling)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    void ApplyGravity()
    {
        verticalVelocity = controller.isGrounded && verticalVelocity < 0 ? -2f : verticalVelocity + gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
    #endregion

    #region Roll
    void HandleRoll()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && canRoll)
        {
            StartRoll();
            return;
        }

        if (!canRoll && (rollTimer += Time.deltaTime) >= rollCooldown)
        {
            canRoll = true;
            rollTimer = 0;
        }
    }

    void StartRoll()
    {
        isRolling = true;
        canRoll = false;
        rollTimer = 0;
        rollStarted = false;

        Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 forward = cameraTransform.forward; forward.y = 0;
        Vector3 right = cameraTransform.right; right.y = 0;
        rollDir = input.magnitude == 0 ? transform.forward : (forward * input.y + right * input.x).normalized;

        animator.Play("Roll", 0, 0f);
    }

    void UpdateRoll()
    {
        rollTimer += Time.deltaTime;
        if (!rollStarted && rollTimer >= rollStartDelay) rollStarted = true;
        if (rollStarted) controller.Move(rollDir * rollSpeed * Time.deltaTime);
        if (rollTimer >= rollDuration) { isRolling = false; rollTimer = 0; rollStarted = false; }
    }
    #endregion

    #region Lock-on
    void HandleLockOn()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (!lockedOn) LockOnNearestEnemy();
            else ReleaseLockOn();
        }

        if (!lockedOn || !target) return;

        if (Vector3.Distance(transform.position, target.position) > lockOnRange) ReleaseLockOn();
        else if (lockOnIndicator) lockOnIndicator.position = target.position + Vector3.up * 2f;
    }

    void LockOnNearestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, lockOnRange, enemyLayer);
        if (enemies.Length == 0) return;

        Transform nearest = null; float minDist = float.MaxValue;
        foreach (Collider e in enemies)
        {
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < minDist) { minDist = d; nearest = e.transform; }
        }

        if (!nearest) return;

        target = nearest;
        lockedOn = true;
        if (lockOnIndicator) lockOnIndicator.gameObject.SetActive(true);
    }

    void ReleaseLockOn()
    {
        lockedOn = false; target = null;
        if (lockOnIndicator) lockOnIndicator.gameObject.SetActive(false);
    }
    #endregion

    #region Animation
    void UpdateAnimations()
    {
        if (!animator) return;
        Vector3 localVel = transform.InverseTransformDirection(moveDir);
        animator.SetFloat("VelocityX", localVel.x, 0.1f, Time.deltaTime);
        animator.SetFloat("VelocityZ", localVel.z, 0.1f, Time.deltaTime);
        animator.SetBool("IsRolling", isRolling);
        animator.SetBool("IsSprinting", isSprinting);
        animator.SetBool("IsLockedOn", lockedOn);
        animator.SetBool("IsGrounded", controller.isGrounded);
        animator.SetFloat("VerticalVelocity", verticalVelocity);
    }
    #endregion

    public Transform GetCurrentTarget() => target;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);
        if (lockedOn && target)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
