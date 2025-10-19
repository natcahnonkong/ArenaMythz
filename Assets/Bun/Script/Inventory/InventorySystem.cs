using InventorySystem.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Inventory
{
    /// <summary>
    /// ระบบจัดการ Inventory หลัก
    /// จัดการการเพิ่ม/ลบ/ย้ายไอเทม และ Event callbacks
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [Tooltip("จำนวน slot ใน Inventory")]
        [SerializeField] private int inventorySize = 20;

        private List<InventorySlot> slots = new List<InventorySlot>();

        // Events สำหรับอัพเดท UI
        public event Action<int, InventorySlot> OnInventoryChanged;
        public event Action OnInventoryFull;

        public int InventorySize => inventorySize;
        public List<InventorySlot> Slots => slots;

        private void Awake()
        {
            InitializeInventory();
        }

        /// <summary>
        /// สร้าง slots ทั้งหมด
        /// </summary>
        private void InitializeInventory()
        {
            slots.Clear();
            for (int i = 0; i < inventorySize; i++)
            {
                slots.Add(new InventorySlot());
            }
        }

        /// <summary>
        /// เพิ่มไอเทมเข้า Inventory
        /// </summary>
        /// <returns>true ถ้าเพิ่มสำเร็จ</returns>
        public bool AddItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;

            int remainingQuantity = quantity;

            // 1. พยายามเพิ่มใน slot ที่มีไอเทมเดียวกันอยู่แล้ว
            if (item.isStackable)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (!slots[i].IsEmpty && slots[i].Item.IsSameItem(item))
                    {
                        int added = slots[i].AddItem(item, remainingQuantity);
                        remainingQuantity -= added;
                        OnInventoryChanged?.Invoke(i, slots[i]);

                        if (remainingQuantity <= 0)
                            return true;
                    }
                }
            }

            // 2. หา slot ว่างเพื่อเพิ่มไอเทมที่เหลือ
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty)
                {
                    int added = slots[i].AddItem(item, remainingQuantity);
                    remainingQuantity -= added;
                    OnInventoryChanged?.Invoke(i, slots[i]);

                    if (remainingQuantity <= 0)
                        return true;
                }
            }

            // 3. ถ้าเพิ่มไม่หมด แสดงว่า Inventory เต็ม
            if (remainingQuantity > 0)
            {
                OnInventoryFull?.Invoke();
                return false;
            }

            return true;
        }

        /// <summary>
        /// ลบไอเทมออกจาก slot ที่กำหนด
        /// </summary>
        public bool RemoveItem(int slotIndex, int quantity = 1)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return false;

            if (slots[slotIndex].IsEmpty)
                return false;

            slots[slotIndex].RemoveItem(quantity);
            OnInventoryChanged?.Invoke(slotIndex, slots[slotIndex]);
            return true;
        }

        /// <summary>
        /// ลบไอเทมตาม ItemData
        /// </summary>
        public bool RemoveItemByType(ItemData item, int quantity = 1)
        {
            int remainingToRemove = quantity;

            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty && slots[i].Item.IsSameItem(item))
                {
                    int removeAmount = Mathf.Min(slots[i].Quantity, remainingToRemove);
                    slots[i].RemoveItem(removeAmount);
                    remainingToRemove -= removeAmount;
                    OnInventoryChanged?.Invoke(i, slots[i]);

                    if (remainingToRemove <= 0)
                        return true;
                }
            }

            return remainingToRemove <= 0;
        }

        /// <summary>
        /// ย้ายไอเทมระหว่าง slot
        /// </summary>
        public void MoveItem(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= slots.Count ||
                toIndex < 0 || toIndex >= slots.Count ||
                fromIndex == toIndex)
                return;

            InventorySlot fromSlot = slots[fromIndex];
            InventorySlot toSlot = slots[toIndex];

            // ถ้า slot ปลายทางว่าง - ย้ายทั้งหมด
            if (toSlot.IsEmpty)
            {
                toSlot.SetItem(fromSlot.Item, fromSlot.Quantity);
                fromSlot.Clear();
            }
            // ถ้าเป็นไอเทมเดียวกัน - stack กัน
            else if (toSlot.Item.IsSameItem(fromSlot.Item) && toSlot.Item.isStackable)
            {
                int spaceLeft = toSlot.Item.maxStackSize - toSlot.Quantity;
                int amountToMove = Mathf.Min(fromSlot.Quantity, spaceLeft);

                toSlot.AddItem(fromSlot.Item, amountToMove);
                fromSlot.RemoveItem(amountToMove);
            }
            // ถ้าเป็นคนละไอเทม - สลับกัน
            else
            {
                InventorySlot temp = fromSlot.Clone();
                fromSlot.SetItem(toSlot.Item, toSlot.Quantity);
                toSlot.SetItem(temp.Item, temp.Quantity);
            }

            OnInventoryChanged?.Invoke(fromIndex, slots[fromIndex]);
            OnInventoryChanged?.Invoke(toIndex, slots[toIndex]);
        }

        /// <summary>
        /// ดึงไอเทมจาก slot
        /// </summary>
        public ItemData GetItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return null;

            return slots[slotIndex].Item;
        }

        /// <summary>
        /// ดึง slot
        /// </summary>
        public InventorySlot GetSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return null;

            return slots[slotIndex];
        }

        /// <summary>
        /// ตรวจสอบว่ามีไอเทมหรือไม่
        /// </summary>
        public bool HasItem(ItemData item, int quantity = 1)
        {
            int totalCount = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.Item.IsSameItem(item))
                {
                    totalCount += slot.Quantity;
                    if (totalCount >= quantity)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// นับจำนวนไอเทมทั้งหมด
        /// </summary>
        public int GetItemCount(ItemData item)
        {
            int count = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.Item.IsSameItem(item))
                {
                    count += slot.Quantity;
                }
            }
            return count;
        }

        /// <summary>
        /// เคลียร์ Inventory ทั้งหมด
        /// </summary>
        public void ClearInventory()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].Clear();
                OnInventoryChanged?.Invoke(i, slots[i]);
            }
        }
    }
}