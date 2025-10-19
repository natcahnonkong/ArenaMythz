using UnityEngine;
using System.Collections;
using InventorySystem.Items;
using InventorySystem.Hotbar;

namespace CombatSystem
{
    /// <summary>
    /// ระบบการต่อสู้ Melee
    /// จัดการการโจมตี, การเช็คการโดน, และการแสดง weapon model
    /// </summary>
    [RequireComponent(typeof(HotbarSystem))]
    public class MeleeCombatSystem : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("ตำแหน่งที่จะวาง weapon model (มือขวา)")]
        [SerializeField] private Transform weaponHolder;

        [Tooltip("จุดเริ่มต้นของการยิง Raycast สำหรับเช็คการโจมตี")]
        [SerializeField] private Transform attackPoint;

        [Tooltip("Layer ของศัตรูที่สามารถโจมตีได้")]
        [SerializeField] private LayerMask enemyLayer;

        [Header("Combat Settings")]
        [Tooltip("ปุ่มสำหรับโจมตี")]
        [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        private HotbarSystem hotbarSystem;
        private Animator animator;
        private AudioSource audioSource;

        private MeleeWeaponData currentWeapon;
        private GameObject currentWeaponModel;

        private bool isAttacking = false;
        private float lastAttackTime = -999f;

        private void Awake()
        {
            hotbarSystem = GetComponent<HotbarSystem>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // ถ้าไม่ได้กำหนด attack point ให้สร้างอัตโนมัติ
            if (attackPoint == null)
            {
                GameObject attackPointObj = new GameObject("AttackPoint");
                attackPointObj.transform.SetParent(transform);
                attackPointObj.transform.localPosition = new Vector3(0, 1f, 1f);
                attackPoint = attackPointObj.transform;
            }
        }

        private void Start()
        {
            // Subscribe to hotbar events
            if (hotbarSystem != null)
            {
                hotbarSystem.OnHotbarSlotSelected += OnHotbarSlotChanged;

                // Equip weapon from initial slot
                UpdateCurrentWeapon();
            }
        }

        private void OnDestroy()
        {
            if (hotbarSystem != null)
            {
                hotbarSystem.OnHotbarSlotSelected -= OnHotbarSlotChanged;
            }
        }

        private void Update()
        {
            HandleCombatInput();
        }

        /// <summary>
        /// จัดการ Input สำหรับการโจมตี
        /// </summary>
        private void HandleCombatInput()
        {
            if (Input.GetKeyDown(attackKey) && CanAttack())
            {
                PerformAttack();
            }
        }

        /// <summary>
        /// ตรวจสอบว่าสามารถโจมตีได้หรือไม่
        /// </summary>
        private bool CanAttack()
        {
            if (isAttacking)
                return false;

            if (currentWeapon == null)
            {
                Debug.Log("No weapon equipped!");
                return false;
            }

            // ตรวจสอบ cooldown
            if (Time.time < lastAttackTime + currentWeapon.attackCooldown)
                return false;

            return true;
        }

        /// <summary>
        /// ทำการโจมตี
        /// </summary>
        private void PerformAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;

            // เล่น animation
            if (animator != null && !string.IsNullOrEmpty(currentWeapon.attackAnimationTrigger))
            {
                animator.SetTrigger(currentWeapon.attackAnimationTrigger);
            }

            // เล่นเสียง
            if (audioSource != null && currentWeapon.attackSound != null)
            {
                audioSource.PlayOneShot(currentWeapon.attackSound);
            }

            // เริ่ม Coroutine สำหรับเช็คการโดน
            StartCoroutine(AttackRoutine());
        }

        /// <summary>
        /// Coroutine สำหรับจัดการลำดับการโจมตี
        /// </summary>
        private IEnumerator AttackRoutine()
        {
            // รอจนกว่าจะถึงจุดที่ต้องเช็คการโดน
            yield return new WaitForSeconds(currentWeapon.hitDetectionDelay);

            // เช็คการโดน
            DetectHit();

            // แสดง effect
            if (currentWeapon.attackEffect != null)
            {
                SpawnAttackEffect();
            }

            // รอให้ animation จบ
            float remainingTime = currentWeapon.attackCooldown - currentWeapon.hitDetectionDelay;
            yield return new WaitForSeconds(Mathf.Max(0, remainingTime));

            isAttacking = false;
        }

        /// <summary>
        /// ตรวจจับศัตรูที่โดนโจมตี
        /// </summary>
        private void DetectHit()
        {
            // ใช้ SphereCast เพื่อหาศัตรูในระยะและมุมที่กำหนด
            Collider[] hitColliders = Physics.OverlapSphere(
                attackPoint.position,
                currentWeapon.attackRange,
                enemyLayer
            );

            foreach (Collider hitCollider in hitColliders)
            {
                // ตรวจสอบมุมการโจมตี
                Vector3 directionToTarget = (hitCollider.transform.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                if (angleToTarget <= currentWeapon.attackAngle / 2f)
                {
                    // ทำความเสียหาย
                    DealDamage(hitCollider.gameObject);

                    Debug.Log($"Hit {hitCollider.name} for {currentWeapon.damage} damage!");
                }
            }
        }

        /// <summary>
        /// สร้างความเสียหายให้เป้าหมาย
        /// </summary>
        private void DealDamage(GameObject target)
        {
            // ถ้าเป้าหมายมี Component ที่รับความเสียหายได้
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(currentWeapon.damage);
            }

            // หรือส่ง Message แบบ loose coupling
            target.SendMessage("TakeDamage", currentWeapon.damage, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// สร้าง Attack Effect
        /// </summary>
        private void SpawnAttackEffect()
        {
            if (currentWeapon.attackEffect != null)
            {
                GameObject effect = Instantiate(
                    currentWeapon.attackEffect,
                    attackPoint.position,
                    attackPoint.rotation
                );

                Destroy(effect, 2f); // ทำลาย effect หลัง 2 วินาที
            }
        }

        /// <summary>
        /// Callback เมื่อเปลี่ยน Hotbar slot
        /// </summary>
        private void OnHotbarSlotChanged(int slotIndex)
        {
            UpdateCurrentWeapon();
        }

        /// <summary>
        /// อัพเดทอาวุธปัจจุบัน
        /// </summary>
        private void UpdateCurrentWeapon()
        {
            // ลบ weapon model เก่า
            if (currentWeaponModel != null)
            {
                Destroy(currentWeaponModel);
                currentWeaponModel = null;
            }

            // ดึงไอเทมจาก Hotbar
            ItemData selectedItem = hotbarSystem.GetSelectedItem();

            // ตรวจสอบว่าเป็นอาวุธหรือไม่
            if (selectedItem is MeleeWeaponData weaponData)
            {
                currentWeapon = weaponData;
                EquipWeapon();
                Debug.Log($"Equipped: {currentWeapon.itemName}");
            }
            else
            {
                currentWeapon = null;
                Debug.Log("No weapon equipped");
            }
        }

        /// <summary>
        /// สวมใส่อาวุธ (แสดง weapon model)
        /// </summary>
        private void EquipWeapon()
        {
            if (currentWeapon == null || weaponHolder == null)
                return;

            if (currentWeapon.weaponModelPrefab != null)
            {
                currentWeaponModel = Instantiate(
                    currentWeapon.weaponModelPrefab,
                    weaponHolder
                );

                currentWeaponModel.transform.localPosition = Vector3.zero;
                currentWeaponModel.transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Force equip อาวุธใหม่
        /// </summary>
        public void EquipWeapon(MeleeWeaponData weapon)
        {
            if (weapon == null)
                return;

            currentWeapon = weapon;
            EquipWeapon();
        }

        /// <summary>
        /// ถอดอาวุธ
        /// </summary>
        public void UnequipWeapon()
        {
            if (currentWeaponModel != null)
            {
                Destroy(currentWeaponModel);
                currentWeaponModel = null;
            }

            currentWeapon = null;
        }

        /// <summary>
        /// ดึงข้อมูลอาวุธปัจจุบัน
        /// </summary>
        public MeleeWeaponData GetCurrentWeapon()
        {
            return currentWeapon;
        }

        /// <summary>
        /// วาด Gizmos สำหรับ Debug
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || attackPoint == null || currentWeapon == null)
                return;

            // วงกลมแสดงระยะโจมตี
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, currentWeapon.attackRange);

            // แสดงมุมการโจมตี
            Vector3 forward = transform.forward * currentWeapon.attackRange;
            float halfAngle = currentWeapon.attackAngle / 2f;

            Quaternion leftRotation = Quaternion.Euler(0, -halfAngle, 0);
            Quaternion rightRotation = Quaternion.Euler(0, halfAngle, 0);

            Vector3 leftBoundary = leftRotation * forward;
            Vector3 rightBoundary = rightRotation * forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(attackPoint.position, attackPoint.position + leftBoundary);
            Gizmos.DrawLine(attackPoint.position, attackPoint.position + rightBoundary);

            // แสดงจุดกึ่งกลาง
            Gizmos.color = Color.green;
            Gizmos.DrawLine(attackPoint.position, attackPoint.position + forward);
        }
    }

    /// <summary>
    /// Interface สำหรับ GameObject ที่สามารถรับความเสียหายได้
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage);
    }
}