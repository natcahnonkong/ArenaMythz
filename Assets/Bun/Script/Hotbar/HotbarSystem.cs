using InventorySystem.Items;
using System;
using UnityEngine;

namespace InventorySystem.Hotbar
{
    /// <summary>
    /// ระบบจัดการ Hotbar (Quick access bar)
    /// ทำงานร่วมกับ Inventory และอนุญาตให้เลือก slot ด้วยคีย์ 1-9
    /// </summary>
    public class HotbarSystem : MonoBehaviour
    {
        [Header("Hotbar Settings")]
        [Tooltip("จำนวน slot ใน Hotbar")]
        [SerializeField] private int hotbarSize = 5;

        [Tooltip("Inventory ที่เชื่อมต่อ")]
        [SerializeField] private Inventory.InventorySystem inventorySystem;

        private int currentSelectedSlot = 0;

        // Events
        public event Action<int> OnHotbarSlotSelected;
        public event Action<int, Inventory.InventorySlot> OnHotbarSlotChanged;

        public int HotbarSize => hotbarSize;
        public int CurrentSelectedSlot => currentSelectedSlot;

        private void Start()
        {
            if (inventorySystem == null)
            {
                inventorySystem = GetComponent<Inventory.InventorySystem>();
                if (inventorySystem == null)
                {
                    Debug.LogError("InventorySystem not found! Please assign it in the inspector.");
                    enabled = false;
                    return;
                }
            }

            // Subscribe to inventory changes
            inventorySystem.OnInventoryChanged += OnInventorySlotChanged;

            // Select first slot by default
            SelectSlot(0);
        }

        private void OnDestroy()
        {
            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged -= OnInventorySlotChanged;
            }
        }

        private void Update()
        {
            HandleHotbarInput();
        }

        /// <summary>
        /// จัดการ Input สำหรับเลือก Hotbar slot (คีย์ 1-9)
        /// </summary>
        private void HandleHotbarInput()
        {
            // ตรวจสอบคีย์ 1-9
            for (int i = 0; i < hotbarSize && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectSlot(i);
                }
            }

            // Mouse Scroll Wheel สำหรับเปลี่ยน slot
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                SelectSlot((currentSelectedSlot - 1 + hotbarSize) % hotbarSize);
            }
            else if (scroll < 0f)
            {
                SelectSlot((currentSelectedSlot + 1) % hotbarSize);
            }
        }

        /// <summary>
        /// เลือก Hotbar slot
        /// </summary>
        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= hotbarSize)
                return;

            currentSelectedSlot = slotIndex;
            OnHotbarSlotSelected?.Invoke(currentSelectedSlot);

            Debug.Log($"Selected Hotbar Slot: {currentSelectedSlot + 1}");
        }

        /// <summary>
        /// ดึงไอเทมจาก Hotbar slot ที่เลือก
        /// </summary>
        public ItemData GetSelectedItem()
        {
            if (inventorySystem == null)
                return null;

            return inventorySystem.GetItem(currentSelectedSlot);
        }

        /// <summary>
        /// ดึง slot ที่เลือก
        /// </summary>
        public Inventory.InventorySlot GetSelectedSlot()
        {
            if (inventorySystem == null)
                return null;

            return inventorySystem.GetSlot(currentSelectedSlot);
        }

        /// <summary>
        /// ใช้ไอเทมใน slot ที่เลือก
        /// </summary>
        public void UseSelectedItem(GameObject user)
        {
            ItemData item = GetSelectedItem();
            if (item != null)
            {
                item.UseItem(user);
            }
        }

        /// <summary>
        /// Callback เมื่อ Inventory เปลี่ยนแปลง
        /// </summary>
        private void OnInventorySlotChanged(int slotIndex, Inventory.InventorySlot slot)
        {
            // อัพเดทเฉพาะ Hotbar slots
            if (slotIndex < hotbarSize)
            {
                OnHotbarSlotChanged?.Invoke(slotIndex, slot);
            }
        }

        /// <summary>
        /// ตรวจสอบว่า slot ใน Hotbar ว่างหรือไม่
        /// </summary>
        public bool IsSlotEmpty(int slotIndex)
        {
            if (inventorySystem == null || slotIndex < 0 || slotIndex >= hotbarSize)
                return true;

            var slot = inventorySystem.GetSlot(slotIndex);
            return slot == null || slot.IsEmpty;
        }

        /// <summary>
        /// ย้ายไอเทมระหว่าง Hotbar slots
        /// </summary>
        public void MoveHotbarItem(int fromIndex, int toIndex)
        {
            if (inventorySystem == null)
                return;

            if (fromIndex >= 0 && fromIndex < hotbarSize &&
                toIndex >= 0 && toIndex < hotbarSize)
            {
                inventorySystem.MoveItem(fromIndex, toIndex);
            }
        }
    }
}