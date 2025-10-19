using UnityEngine;

namespace InventorySystem.Items
{
    /// <summary>
    /// ScriptableObject สำหรับอาวุธ Melee
    /// เก็บข้อมูลเฉพาะของอาวุธ เช่น ความเสียหาย, ระยะโจมตี
    /// </summary>
    [CreateAssetMenu(fileName = "New Melee Weapon", menuName = "Inventory/Melee Weapon")]
    public class MeleeWeaponData : ItemData
    {
        [Header("Weapon Stats")]
        [Tooltip("ความเสียหายของอาวุธ")]
        public float damage = 10f;

        [Tooltip("ระยะการโจมตี (เมตร)")]
        public float attackRange = 2f;

        [Tooltip("มุมการโจมตี (องศา)")]
        public float attackAngle = 60f;

        [Tooltip("เวลาระหว่างการโจมตีแต่ละครั้ง (วินาที)")]
        public float attackCooldown = 1f;

        [Header("Animation")]
        [Tooltip("ชื่อ Animation Trigger สำหรับการโจมตี")]
        public string attackAnimationTrigger = "Attack";

        [Tooltip("เวลาที่จะเช็คการโดน (หลังเริ่มโจมตี)")]
        public float hitDetectionDelay = 0.3f;

        [Header("Effects")]
        [Tooltip("Particle effect เมื่อโจมตี")]
        public GameObject attackEffect;

        [Tooltip("เสียงเมื่อโจมตี")]
        public AudioClip attackSound;

        [Tooltip("Prefab ของอาวุธที่จะถือในมือ")]
        public GameObject weaponModelPrefab;

        /// <summary>
        /// Override UseItem สำหรับอาวุธ (จะใช้ผ่าน Combat System)
        /// </summary>
        public override void UseItem(GameObject user)
        {
            // อาวุธจะถูกใช้ผ่าน MeleeCombatSystem
            Debug.Log($"Equipped {itemName}");
        }
    }
}