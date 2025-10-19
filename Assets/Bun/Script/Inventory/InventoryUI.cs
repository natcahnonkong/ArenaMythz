using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace InventorySystem.UI
{
    /// <summary>
    /// จัดการ UI ของ Inventory
    /// แสดงผล slots, ไอเทม, และจัดการการ drag/drop
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("InventorySystem ที่จะแสดงผล")]
        [SerializeField] private Inventory.InventorySystem inventorySystem;

        [Tooltip("Grid Layout สำหรับวาง Inventory slots")]
        [SerializeField] private Transform inventoryGridParent;

        [Tooltip("Prefab ของ Inventory Slot UI")]
        [SerializeField] private GameObject slotUIPrefab;

        [Header("UI Settings")]
        [Tooltip("ปุ่มสำหรับเปิด/ปิด Inventory")]
        [SerializeField] private KeyCode toggleInventoryKey = KeyCode.Tab;

        [Tooltip("Canvas Group สำหรับควบคุมการแสดงผล")]
        [SerializeField] private CanvasGroup inventoryCanvasGroup;

        private List<InventorySlotUI> slotUIList = new List<InventorySlotUI>();
        private bool isInventoryOpen = false;

        private void Start()
        {
            if (inventorySystem == null)
            {
                inventorySystem = FindObjectOfType<Inventory.InventorySystem>();
                if (inventorySystem == null)
                {
                    Debug.LogError("InventorySystem not found!");
                    enabled = false;
                    return;
                }
            }

            InitializeUI();
            CloseInventory(); // เริ่มต้นให้ปิด

            // Subscribe to events
            inventorySystem.OnInventoryChanged += UpdateSlotUI;
        }

        private void OnDestroy()
        {
            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged -= UpdateSlotUI;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleInventoryKey))
            {
                ToggleInventory();
            }
        }

        /// <summary>
        /// สร้าง UI slots ทั้งหมด
        /// </summary>
        private void InitializeUI()
        {
            if (inventoryGridParent == null || slotUIPrefab == null)
            {
                Debug.LogError("Missing UI references!");
                return;
            }

            // Clear existing slots
            foreach (Transform child in inventoryGridParent)
            {
                Destroy(child.gameObject);
            }
            slotUIList.Clear();

            // Create new slots
            for (int i = 0; i < inventorySystem.InventorySize; i++)
            {
                GameObject slotObj = Instantiate(slotUIPrefab, inventoryGridParent);
                InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

                if (slotUI != null)
                {
                    slotUI.Initialize(i, inventorySystem);
                    slotUIList.Add(slotUI);
                    UpdateSlotUI(i, inventorySystem.GetSlot(i));
                }
            }
        }

        /// <summary>
        /// อัพเดท UI ของ slot
        /// </summary>
        private void UpdateSlotUI(int slotIndex, Inventory.InventorySlot slot)
        {
            if (slotIndex >= 0 && slotIndex < slotUIList.Count)
            {
                slotUIList[slotIndex].UpdateSlot(slot);
            }
        }

        /// <summary>
        /// เปิด/ปิด Inventory
        /// </summary>
        public void ToggleInventory()
        {
            if (isInventoryOpen)
                CloseInventory();
            else
                OpenInventory();
        }

        /// <summary>
        /// เปิด Inventory
        /// </summary>
        public void OpenInventory()
        {
            isInventoryOpen = true;
            SetInventoryVisibility(true);
        }

        /// <summary>
        /// ปิด Inventory
        /// </summary>
        public void CloseInventory()
        {
            isInventoryOpen = false;
            SetInventoryVisibility(false);
        }

        /// <summary>
        /// ตั้งค่าการมองเห็น Inventory
        /// </summary>
        private void SetInventoryVisibility(bool visible)
        {
            if (inventoryCanvasGroup != null)
            {
                inventoryCanvasGroup.alpha = visible ? 1f : 0f;
                inventoryCanvasGroup.blocksRaycasts = visible;
                inventoryCanvasGroup.interactable = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }

    /// <summary>
    /// Component สำหรับ Inventory Slot UI แต่ละอัน
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image backgroundImage;

        private int slotIndex;
        private Inventory.InventorySystem inventorySystem;

        public void Initialize(int index, Inventory.InventorySystem invSystem)
        {
            slotIndex = index;
            inventorySystem = invSystem;
        }

        /// <summary>
        /// อัพเดทการแสดงผลของ slot
        /// </summary>
        public void UpdateSlot(Inventory.InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty)
            {
                // Slot ว่าง
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }
                if (quantityText != null)
                {
                    quantityText.text = "";
                }
            }
            else
            {
                // Slot มีไอเทม
                if (iconImage != null)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = slot.Item.icon;
                }

                if (quantityText != null)
                {
                    quantityText.text = slot.Quantity > 1 ? slot.Quantity.ToString() : "";
                }
            }
        }
    }
}