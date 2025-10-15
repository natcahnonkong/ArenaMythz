using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WeaponHitbox : MonoBehaviour
{
    [Header("Collider Hitbox")]
    [Tooltip("Assign a child collider (isTrigger = true). This will be toggled during active frames.")]
    [SerializeField] private Collider hitboxTrigger;

    [Header("Raycast (Optional)")]
    [SerializeField] private bool alsoUseRaycast = false;
    [Tooltip("Raycast origin (usually weapon root or handle).")]
    [SerializeField] private Transform rayOrigin;
    [Tooltip("Raycast direction relative to origin forward.")]
    [SerializeField] private float rayLength = 1.5f;
    [SerializeField] private float rayRadius = 0.25f;
    [SerializeField] private LayerMask raycastMask = ~0;

    [Header("General")]
    [Tooltip("Enemies must have this tag to receive damage via EnemyHealth.")]
    [SerializeField] private string enemyTag = "Enemy";

    private bool _active;
    private float _damage;
    private readonly HashSet<int> _alreadyHit = new HashSet<int>();
    private int _attackId; // increments per BeginAttack() to clear per-swing memory

    private void Reset()
    {
        // Try to find a trigger collider automatically on this object
        if (!hitboxTrigger) hitboxTrigger = GetComponent<Collider>();
        if (hitboxTrigger) hitboxTrigger.isTrigger = true;
    }

    private void Awake()
    {
        if (hitboxTrigger != null) hitboxTrigger.enabled = false;
    }

    /// <summary>
    /// Called by PlayerCombat at start of active frames.
    /// </summary>
    public void BeginAttack(float damage)
    {
        _damage = damage;
        _active = true;
        _attackId++;
        _alreadyHit.Clear();

        if (hitboxTrigger) hitboxTrigger.enabled = true;
    }

    /// <summary>
    /// Called by PlayerCombat at end of active frames.
    /// </summary>
    public void EndAttack()
    {
        _active = false;
        if (hitboxTrigger) hitboxTrigger.enabled = false;
    }

    private void FixedUpdate()
    {
        if (_active && alsoUseRaycast && rayOrigin)
        {
            // SphereCast in the forward direction of rayOrigin to catch very fast swings
            if (Physics.SphereCast(rayOrigin.position, rayRadius, rayOrigin.forward, out RaycastHit hit, rayLength, raycastMask, QueryTriggerInteraction.Ignore))
            {
                TryDamage(hit.collider);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active) return;
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        if (other == null) return;
        if (!other.CompareTag(enemyTag)) return;

        int id = other.GetInstanceID();
        if (_alreadyHit.Contains(id)) return; // Only once per target per attack window
        _alreadyHit.Add(id);

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(_damage);
        }
    }
}
