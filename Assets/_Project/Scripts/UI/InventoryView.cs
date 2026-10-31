using System.Text;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Items;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Рядок інвентаря: вибраний предмет у дужках.</summary>
    public class InventoryView : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private VoidEventChannel inventoryChanged;
        [SerializeField] private Text label;

        private void OnEnable() => inventoryChanged.Raised += Refresh;

        private void OnDisable() => inventoryChanged.Raised -= Refresh;

        private void Refresh()
        {
            var text = new StringBuilder($"Інвентар {inventory.Items.Count}/{inventory.Capacity}: ");
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                string itemName = inventory.Items[i].DisplayName;
                text.Append(i == inventory.SelectedIndex ? $"[{itemName}] " : $"{itemName} ");
            }

            label.text = text.ToString();
        }
    }
}
