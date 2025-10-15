using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHP = 100f;

    [Header("Death")]
    [Tooltip("If true, Destroy the GameObject on death; otherwise just disable components.")]
    [SerializeField] private bool destroyOnDeath = true;

    [Tooltip("Optional: Animator parameter to set on death (e.g., 'Dead' bool).")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deathBoolParam = "Dead";

    private float _hp;
    private bool _dead;

    private void Awake()
    {
        _hp = maxHP;
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(float amount)
    {
        if (_dead) return;

        _hp = Mathf.Max(0f, _hp - Mathf.Max(0f, amount));
        if (_hp <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_dead) return;
        _dead = true;

        if (animator && !string.IsNullOrEmpty(deathBoolParam))
        {
            animator.SetBool(deathBoolParam, true);
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject, 0.1f);
        }
        else
        {
            // Simple soft-death: disable colliders & AI here if needed
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            var rb = GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;
            enabled = false;
        }
    }
}
