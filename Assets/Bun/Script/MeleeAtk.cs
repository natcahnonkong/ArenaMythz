using UnityEngine;

public class MeleeAtk : MonoBehaviour
{
    [Header("Attack Settings")]
    public int damage = 25;
    public float range = 1.5f;
    public float attackAngle = 90f;
    public LayerMask hittableLayers;


    [Header("Timing")]
    public float attackCooldown = 0.6f;
    public float hitDelay = 0.15f;


    [Header("Feedback")]
    public Animator animator;
    public string attackTriggerName = "Attack";
    public AudioClip swingSfx;
    public AudioClip hitSfx;
    public Transform hitPoint;


    private float lastAttackTime = -999f;
    private AudioSource audioSource;


    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (hitPoint == null) hitPoint = transform;
    }


    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            TryAttack();
        }
    }


    void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;


        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
            animator.SetTrigger(attackTriggerName);


        if (swingSfx != null) audioSource.PlayOneShot(swingSfx);


        if (hitDelay <= 0f) DoHit(); else Invoke(nameof(DoHit), hitDelay);
    }


    void DoHit()
    {
        Collider[] hits = Physics.OverlapSphere(hitPoint.position, range, hittableLayers);
        bool anyHit = false;


        foreach (var col in hits)
        {
            if (col.transform == transform) continue;


            Vector3 dirToTarget = (col.transform.position - transform.position);
            dirToTarget.y = 0;
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            if (angle > attackAngle * 0.5f) continue;


            var health = col.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage);
                anyHit = true;
            }


            var rb = col.attachedRigidbody;
            if (rb != null)
            {
                Vector3 push = (col.transform.position - transform.position).normalized;
                push.y = 0.25f;
                rb.AddForce(push * 5f, ForceMode.Impulse);
            }
        }


        if (anyHit && hitSfx != null) audioSource.PlayOneShot(hitSfx);
    }


    void OnDrawGizmosSelected()
    {
        if (hitPoint == null) hitPoint = transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitPoint.position, range);


        Vector3 forward = transform.forward;
        float half = attackAngle * 0.5f;
        Quaternion leftRot = Quaternion.AngleAxis(-half, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(half, Vector3.up);
        Vector3 leftDir = leftRot * forward * range;
        Vector3 rightDir = rightRot * forward * range;
        Gizmos.DrawLine(hitPoint.position, hitPoint.position + leftDir);
        Gizmos.DrawLine(hitPoint.position, hitPoint.position + rightDir);
    }
}