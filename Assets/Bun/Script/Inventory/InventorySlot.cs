using InventorySystem.Items;
using UnityEngine;

namespace InventorySystem.Inventory
{
    /// <summary>
    /// Class สำหรับเก็บข้อมูลของ slot หนึ่งใน Inventory
    /// เก็บข้อมูลว่า slot นี้มีไอเทมอะไร กี่ชิ้น
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        [SerializeField] private ItemData item;
        [SerializeField] private int quantity;

        public ItemData Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;

        /// <summary>
        /// Constructor - สร้าง slot ว่าง
        /// </summary>
        public InventorySlot()
        {
            item = null;
            quantity = 0;
        }

        /// <summary>
        /// Constructor - สร้าง slot พร้อมไอเทม
        /// </summary>
        public InventorySlot(ItemData newItem, int newQuantity)
        {
            item = newItem;
            quantity = newQuantity;
        }

        /// <summary>
        /// เพิ่มไอเทมใน slot
        /// </summary>
        /// <returns>จำนวนที่เพิ่มได้จริง</returns>
        public int AddItem(ItemData newItem, int amount)
        {
            // ถ้า slot ว่าง
            if (IsEmpty)
            {
                item = newItem;
                quantity = Mathf.Min(amount, newItem.maxStackSize);
                return quantity;
            }

            // ถ้าเป็นไอเทมเดียวกันและ stackable
            if (item.IsSameItem(newItem) && item.isStackable)
            {
                int spaceLeft = item.maxStackSize - quantity;
                int amountToAdd = Mathf.Min(amount, spaceLeft);
                quantity += amountToAdd;
                return amountToAdd;
            }

            return 0; // เพิ่มไม่ได้
        }

        /// <summary>
        /// ลดจำนวนไอเทมใน slot
        /// </summary>
        public void RemoveItem(int amount)
        {
            quantity -= amount;
            if (quantity <= 0)
            {
                Clear();
            }
        }

        /// <summary>
        /// ตั้งค่าไอเทมใหม่
        /// </summary>
        public void SetItem(ItemData newItem, int newQuantity)
        {
            item = newItem;
            quantity = newQuantity;
        }

        /// <summary>
        /// เคลียร์ slot
        /// </summary>
        public void Clear()
        {
            item = null;
            quantity = 0;
        }

        /// <summary>
        /// สามารถเพิ่มไอเทมได้หรือไม่
        /// </summary>
        public bool CanAddItem(ItemData newItem)
        {
            if (IsEmpty) return true;
            if (item.IsSameItem(newItem) && item.isStackable)
            {
                return quantity < item.maxStackSize;
            }
            return false;
        }

        /// <summary>
        /// คัดลอก slot
        /// </summary>
        public InventorySlot Clone()
        {
            return new InventorySlot(item, quantity);
        }
    }
}