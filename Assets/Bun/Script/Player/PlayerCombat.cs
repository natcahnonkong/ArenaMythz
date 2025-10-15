using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Animator with attack animations that fire animation events.")]
    [SerializeField] private Animator animator;

    [Tooltip("Weapon hitbox component on a child (or the weapon) that will be enabled/disabled by animation events.")]
    [SerializeField] private WeaponHitbox weaponHitbox;

    [Tooltip("Optional: PlayerInput on the player (or pass InputActionReferences directly).")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Input Actions (New Input System)")]
    [Tooltip("Action name must be 'LightAttack' in your Input Actions asset, or wire an InputActionReference here.")]
    [SerializeField] private InputActionReference lightAttackAction;

    [Tooltip("Action name must be 'HeavyAttack' in your Input Actions asset, or wire an InputActionReference here.")]
    [SerializeField] private InputActionReference heavyAttackAction;

    [Header("Animator Parameters (Triggers)")]
    [SerializeField] private string lightTrigger = "LightAttack";
    [SerializeField] private string heavyTrigger = "HeavyAttack";

    [Header("Stamina")]
    [SerializeField, Min(1f)] private float maxStamina = 100f;
    [SerializeField, Min(0f)] private float staminaRegenPerSecond = 15f;
    [SerializeField, Min(0f)] private float lightStaminaCost = 15f;
    [SerializeField, Min(0f)] private float heavyStaminaCost = 35f;

    [Header("Damage")]
    [SerializeField, Min(0f)] private float lightDamage = 20f;
    [SerializeField, Min(0f)] private float heavyDamage = 45f;

    [Header("Timing")]
    [Tooltip("Global cooldown after an attack chain finishes before a new chain can begin.")]
    [SerializeField, Min(0f)] private float chainCooldown = 0.15f;

    [Tooltip("If true, you can buffer the next input and it will fire as soon as combo window opens.")]
    [SerializeField] private bool inputBuffering = true;

    [Tooltip("Optional hard block to prevent starting a new chain while recovering.")]
    [SerializeField] private bool blockDuringRecovery = true;

    // Internal state
    private float _stamina;
    private bool _isAttacking;          // currently in any attack animation
    private bool _canCombo;             // currently in a combo window
    private bool _inRecovery;           // between attacks in the same chain (animation-controlled)
    private float _nextChainTime;       // time when new chain can begin

    private AttackType? _bufferedAttack;  // next attack requested during combo window (or buffered if inputBuffering)
    private AttackType _currentAttack;    // currently playing

    // Holds the active attack's damage & stamina for the hitbox to read
    private float _currentAttackDamage = 0f;

    private enum AttackType { Light, Heavy }

    private void Awake()
    {
        _stamina = maxStamina;

        // Fallback: if not wired in the inspector, try to fetch actions by name from PlayerInput
        if (playerInput && (lightAttackAction == null || heavyAttackAction == null))
        {
            var la = playerInput.actions?["LightAttack"];
            var ha = playerInput.actions?["HeavyAttack"];

            if (lightAttackAction == null && la != null) lightAttackAction = InputActionReference.Create(la);
            if (heavyAttackAction == null && ha != null) heavyAttackAction = InputActionReference.Create(ha);
        }
    }

    private void OnEnable()
    {
        if (lightAttackAction != null) lightAttackAction.action.performed += OnLightAttack;
        if (heavyAttackAction != null) heavyAttackAction.action.performed += OnHeavyAttack;
    }

    private void OnDisable()
    {
        if (lightAttackAction != null) lightAttackAction.action.performed -= OnLightAttack;
        if (heavyAttackAction != null) heavyAttackAction.action.performed -= OnHeavyAttack;
    }

    private void Update()
    {
        RegenerateStamina();

        // Optional block to ensure you can't start new chain while recovering
        if (blockDuringRecovery && _inRecovery) return;
    }

    private void RegenerateStamina()
    {
        if (!_isAttacking)
        {
            _stamina = Mathf.Min(maxStamina, _stamina + staminaRegenPerSecond * Time.deltaTime);
        }
    }

    private void OnLightAttack(InputAction.CallbackContext ctx)
    {
        HandleAttackInput(AttackType.Light);
    }

    private void OnHeavyAttack(InputAction.CallbackContext ctx)
    {
        HandleAttackInput(AttackType.Heavy);
    }

    private void HandleAttackInput(AttackType type)
    {
        // If currently in an attack and a combo window is open or buffering allowed, store the input
        if (_isAttacking)
        {
            if (_canCombo || inputBuffering)
            {
                _bufferedAttack = type;
            }
            return;
        }

        // If not attacking, check chain cooldown
        if (Time.time < _nextChainTime) return;

        // Start a new chain immediately if stamina allows
        TryStartAttack(type);
    }

    private bool HasStaminaFor(AttackType type)
    {
        float cost = (type == AttackType.Light) ? lightStaminaCost : heavyStaminaCost;
        return _stamina >= cost;
    }

    private void SpendStaminaFor(AttackType type)
    {
        float cost = (type == AttackType.Light) ? lightStaminaCost : heavyStaminaCost;
        _stamina = Mathf.Max(0f, _stamina - cost);
    }

    private float DamageFor(AttackType type)
    {
        return (type == AttackType.Light) ? lightDamage : heavyDamage;
    }

    private void TriggerAnimatorFor(AttackType type)
    {
        if (type == AttackType.Light) animator.SetTrigger(lightTrigger);
        else animator.SetTrigger(heavyTrigger);
    }

    private bool TryStartAttack(AttackType type)
    {
        if (!HasStaminaFor(type)) return false;

        _isAttacking = true;
        _inRecovery = false;
        _canCombo = false;
        _currentAttack = type;
        _currentAttackDamage = DamageFor(type);

        SpendStaminaFor(type);
        TriggerAnimatorFor(type);
        return true;
    }

    // ------------------------------------------------------------
    // Animation Events (call these from your attack animations)
    // ------------------------------------------------------------

    /// <summary>
    /// Called on the frame the attack becomes active (start of the 'active' window).
    /// Typically placed at the first active contact frame.
    /// </summary>
    public void AnimEvent_AttackStart()
    {
        // Set damage for the hitbox (in case clips vary damage after chaining)
        weaponHitbox?.BeginAttack(_currentAttackDamage);
    }

    /// <summary>
    /// Called when the combo input window opens.
    /// Place a little before the end of the current attack animation.
    /// </summary>
    public void AnimEvent_ComboWindowOpen()
    {
        _canCombo = true;

        // If player buffered an input, try to chain now
        if (_bufferedAttack.HasValue)
        {
            // We switch to recovery briefly so a second AnimEvent_AttackStart comes from the next clip
            ChainNextAttack(_bufferedAttack.Value);
            _bufferedAttack = null;
        }
    }

    /// <summary>
    /// Called when the combo input window closes.
    /// </summary>
    public void AnimEvent_ComboWindowClose()
    {
        _canCombo = false;
    }

    /// <summary>
    /// Called on the frame the attack is no longer active (end of the 'active' window).
    /// </summary>
    public void AnimEvent_AttackEnd()
    {
        weaponHitbox?.EndAttack();
        _inRecovery = true; // between attacks in the same chain (handled by clips/anim transitions)
    }

    /// <summary>
    /// Called at the end of the attack clip (or after the last chained attack resolves).
    /// </summary>
    public void AnimEvent_AttackFinished()
    {
        // If we still have a buffered input (no combo window caught it), try to resolve it if possible
        if (_bufferedAttack.HasValue && !_canCombo)
        {
            // If chain is finished (no combo window), start as a new chain (respect cooldown)
            if (Time.time >= _nextChainTime && !_isAttacking)
            {
                var next = _bufferedAttack.Value;
                _bufferedAttack = null;
                TryStartAttack(next);
                return;
            }
        }

        // End of chain
        _isAttacking = false;
        _inRecovery = false;
        _canCombo = false;
        _currentAttackDamage = 0f;
        _nextChainTime = Time.time + chainCooldown;
    }

    private void ChainNextAttack(AttackType next)
    {
        if (!HasStaminaFor(next)) return;

        // Spend stamina and set damage for the upcoming clip
        SpendStaminaFor(next);
        _currentAttack = next;
        _currentAttackDamage = DamageFor(next);

        // Fire the next clip immediately via trigger
        TriggerAnimatorFor(next);

        // Once we decided to chain, close the combo window until the next clip opens it again
        _canCombo = false;
        _inRecovery = false;
    }

    // ---------------------------
    // Optional utility accessors
    // ---------------------------
    public float CurrentStamina01 => maxStamina <= 0.01f ? 0f : _stamina / maxStamina;
    public bool IsAttacking => _isAttacking;
}
