using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace InventorySystem.UI
{
    /// <summary>
    /// จัดการ UI ของ Hotbar (อยู่ล่างซ้ายของหน้าจอ)
    /// แสดง slots และ highlight slot ที่เลือก
    /// </summary>
    public class HotbarUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("HotbarSystem ที่จะแสดงผล")]
        [SerializeField] private Hotbar.HotbarSystem hotbarSystem;

        [Tooltip("InventorySystem สำหรับดึงข้อมูลไอเทม")]
        [SerializeField] private Inventory.InventorySystem inventorySystem;

        [Tooltip("Parent สำหรับวาง Hotbar slots")]
        [SerializeField] private Transform hotbarSlotsParent;

        [Tooltip("Prefab ของ Hotbar Slot UI")]
        [SerializeField] private GameObject hotbarSlotPrefab;

        [Header("Visual Settings")]
        [Tooltip("สีของ slot ที่ถูกเลือก")]
        [SerializeField] private Color selectedSlotColor = Color.yellow;

        [Tooltip("สีของ slot ปกติ")]
        [SerializeField] private Color normalSlotColor = Color.white;

        private List<HotbarSlotUI> hotbarSlotUIList = new List<HotbarSlotUI>();

        private void Start()
        {
            // หา references ถ้ายังไม่ได้กำหนด
            if (hotbarSystem == null)
            {
                hotbarSystem = FindObjectOfType<Hotbar.HotbarSystem>();
            }

            if (inventorySystem == null)
            {
                inventorySystem = FindObjectOfType<Inventory.InventorySystem>();
            }

            if (hotbarSystem == null || inventorySystem == null)
            {
                Debug.LogError("Missing required systems!");
                enabled = false;
                return;
            }

            InitializeHotbarUI();

            // Subscribe to events
            hotbarSystem.OnHotbarSlotSelected += OnSlotSelected;
            hotbarSystem.OnHotbarSlotChanged += UpdateHotbarSlot;
        }

        private void OnDestroy()
        {
            if (hotbarSystem != null)
            {
                hotbarSystem.OnHotbarSlotSelected -= OnSlotSelected;
                hotbarSystem.OnHotbarSlotChanged -= UpdateHotbarSlot;
            }
        }

        /// <summary>
        /// สร้าง Hotbar UI slots
        /// </summary>
        private void InitializeHotbarUI()
        {
            if (hotbarSlotsParent == null || hotbarSlotPrefab == null)
            {
                Debug.LogError("Missing Hotbar UI references!");
                return;
            }

            // Clear existing
            foreach (Transform child in hotbarSlotsParent)
            {
                Destroy(child.gameObject);
            }
            hotbarSlotUIList.Clear();

            // Create hotbar slots
            for (int i = 0; i < hotbarSystem.HotbarSize; i++)
            {
                GameObject slotObj = Instantiate(hotbarSlotPrefab, hotbarSlotsParent);
                HotbarSlotUI slotUI = slotObj.GetComponent<HotbarSlotUI>();

                if (slotUI != null)
                {
                    slotUI.Initialize(i, normalSlotColor);
                    hotbarSlotUIList.Add(slotUI);

                    // อัพเดทข้อมูลเริ่มต้น
                    var slot = inventorySystem.GetSlot(i);
                    if (slot != null)
                    {
                        slotUI.UpdateSlot(slot);
                    }
                }
            }

            // Highlight slot แรก
            OnSlotSelected(0);
        }

        /// <summary>
        /// Callback เมื่อเลือก slot ใหม่
        /// </summary>
        private void OnSlotSelected(int slotIndex)
        {
            // ปิด highlight ทุก slot
            foreach (var slotUI in hotbarSlotUIList)
            {
                slotUI.SetSelected(false, normalSlotColor);
            }

            // Highlight slot ที่เลือก
            if (slotIndex >= 0 && slotIndex < hotbarSlotUIList.Count)
            {
                hotbarSlotUIList[slotIndex].SetSelected(true, selectedSlotColor);
            }
        }

        /// <summary>
        /// Callback เมื่อ Hotbar slot เปลี่ยนแปลง
        /// </summary>
        private void UpdateHotbarSlot(int slotIndex, Inventory.InventorySlot slot)
        {
            if (slotIndex >= 0 && slotIndex < hotbarSlotUIList.Count)
            {
                hotbarSlotUIList[slotIndex].UpdateSlot(slot);
            }
        }
    }

    /// <summary>
    /// Component สำหรับ Hotbar Slot UI แต่ละอัน
    /// </summary>
    public class HotbarSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI keyNumberText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image selectionFrame;

        private int slotIndex;
        private Color normalColor;

        public void Initialize(int index, Color normalSlotColor)
        {
            slotIndex = index;
            normalColor = normalSlotColor;

            // แสดงเลขคีย์ (1-9)
            if (keyNumberText != null)
            {
                keyNumberText.text = (index + 1).ToString();
            }

            // ตั้งค่าเริ่มต้น
            SetSelected(false, normalColor);
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

        /// <summary>
        /// ตั้งค่าสถานะการเลือก
        /// </summary>
        public void SetSelected(bool selected, Color highlightColor)
        {
            if (selectionFrame != null)
            {
                selectionFrame.enabled = selected;
                selectionFrame.color = highlightColor;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? highlightColor : normalColor;
            }
        }
    }
}