using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NightTrolleybus.UI
{
    /// <summary>Передає наведення й натискання на клітинку інвентаря.</summary>
    public class InventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public int Index { get; set; }
        public event Action<int> Hovered;
        public event Action<int> Unhovered;
        public event Action<int, int> Clicked;

        public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(Index);

        public void OnPointerExit(PointerEventData eventData) => Unhovered?.Invoke(Index);

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(Index, eventData.clickCount);
    }
}
