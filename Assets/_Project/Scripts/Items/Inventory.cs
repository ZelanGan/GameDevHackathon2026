using System.Collections.Generic;
using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Vehicle.Repair;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Items
{
    /// <summary>Обмежений інвентар гравця. Предмет вибирають колесом миші, Tab або цифрами, а Q використовує його.</summary>
    public class Inventory : MonoBehaviour, IInventory
    {
        [SerializeField] private int capacity = 6;
        [SerializeField] private VoidEventChannel inventoryChanged;
        [SerializeField] private StringEventChannel noticeRaised;

        private readonly List<ItemData> items = new List<ItemData>();

        public IReadOnlyList<ItemData> Items => items;
        public int Capacity => capacity;
        public int SelectedIndex { get; private set; }

        private void Awake() => GameContext.Register(this);

        private void OnDestroy() => GameContext.Unregister(this);

        private void Start() => inventoryChanged.Raise();

        public bool TryAdd(ItemData item)
        {
            if (items.Count >= capacity)
            {
                noticeRaised.Raise("Інвентар заповнений");
                return false;
            }

            items.Add(item);
            noticeRaised.Raise($"Отримано: {item.DisplayName}");
            inventoryChanged.Raise();
            return true;
        }

        public int Count(ItemData item)
        {
            int count = 0;
            foreach (ItemData other in items)
            {
                if (other == item)
                    count++;
            }

            return count;
        }

        public bool TryRemove(ItemData item, int count)
        {
            if (Count(item) < count)
                return false;

            for (int i = 0; i < count; i++)
                items.Remove(item);

            SelectedIndex = items.Count == 0 ? 0 : Mathf.Min(SelectedIndex, items.Count - 1);
            inventoryChanged.Raise();
            return true;
        }

        /// <summary>Замінює вміст інвентаря даними зі збереження.</summary>
        public void SetItems(IEnumerable<ItemData> saved)
        {
            items.Clear();
            items.AddRange(saved);
            SelectedIndex = 0;
            inventoryChanged.Raise();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || items.Count == 0)
                return;

            if (keyboard.tabKey.wasPressedThisFrame)
                Step(1);

            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (scroll != 0f)
                    Step(scroll > 0f ? -1 : 1);
            }

            // Цифри 1..3 зайняті в міні-грі ремонту, тому там вони предмет не перемикають.
            RepairHost repairHost = FindAnyObjectByType<RepairHost>();
            if (repairHost == null || !repairHost.IsOpen)
                SelectFromDigits(keyboard);

            if (keyboard.qKey.wasPressedThisFrame)
                UseSelected();
        }

        private void Step(int direction)
        {
            SelectedIndex = (SelectedIndex + direction + items.Count) % items.Count;
            inventoryChanged.Raise();
        }

        private void SelectFromDigits(Keyboard keyboard)
        {
            Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
            for (int i = 0; i < keys.Length; i++)
            {
                if (keyboard[keys[i]].wasPressedThisFrame)
                    Select(i);
            }
        }

        public void Select(int index)
        {
            if (index < 0 || index >= items.Count || index == SelectedIndex)
                return;

            SelectedIndex = index;
            inventoryChanged.Raise();
        }

        public void UseSelected()
        {
            if (items.Count == 0)
                return;

            ItemData item = items[SelectedIndex];
            if (!item.IsUsable)
            {
                noticeRaised.Raise($"{item.DisplayName}: це для обміну");
                return;
            }

            items.RemoveAt(SelectedIndex);
            SelectedIndex = items.Count == 0 ? 0 : Mathf.Min(SelectedIndex, items.Count - 1);
            item.Use();
            noticeRaised.Raise($"Використано: {item.DisplayName} ({item.Description})");
            inventoryChanged.Raise();
        }
    }
}
