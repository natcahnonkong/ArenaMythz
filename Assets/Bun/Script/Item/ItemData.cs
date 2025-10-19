using UnityEngine;

namespace InventorySystem.Items
{
    /// <summary>
    /// Base class สำหรับ Item ทุกประเภท
    /// สร้าง ScriptableObject เพื่อเก็บข้อมูล Item แบบ reusable
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("ชื่อของไอเทม")]
        public string itemName = "New Item";

        [Tooltip("รหัสไอเทมที่ไม่ซ้ำกัน")]
        public string itemID = "item_000";

        [Tooltip("คำอธิบายของไอเทม")]
        [TextArea(3, 5)]
        public string description = "Item description";

        [Tooltip("ไอคอนสำหรับแสดงใน UI")]
        public Sprite icon;

        [Header("Stack Settings")]
        [Tooltip("สามารถ stack ได้หรือไม่")]
        public bool isStackable = true;

        [Tooltip("จำนวนสูงสุดที่ stack ได้ (ถ้า isStackable = true)")]
        public int maxStackSize = 99;

        [Header("Prefab Reference")]
        [Tooltip("Prefab ของไอเทมในโลก 3D (ถ้ามี)")]
        public GameObject itemPrefab;

        /// <summary>
        /// ใช้ไอเทม - Override ใน derived classes
        /// </summary>
        public virtual void UseItem(GameObject user)
        {
            Debug.Log($"{itemName} was used by {user.name}");
        }

        /// <summary>
        /// ตรวจสอบว่าเป็นไอเทมประเภทเดียวกันหรือไม่
        /// </summary>
        public virtual bool IsSameItem(ItemData other)
        {
            return other != null && itemID == other.itemID;
        }
    }
}