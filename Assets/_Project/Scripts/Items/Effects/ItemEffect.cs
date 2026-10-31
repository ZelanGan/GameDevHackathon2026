using UnityEngine;

namespace NightTrolleybus.Items.Effects
{
    /// <summary>Один ефект предмета. Предмет може мати кілька ефектів, зокрема побічні.</summary>
    public abstract class ItemEffect : ScriptableObject
    {
        public abstract void Apply();
    }
}
