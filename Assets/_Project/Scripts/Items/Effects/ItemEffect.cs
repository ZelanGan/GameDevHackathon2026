using UnityEngine;

namespace NightTrolleybus.Items.Effects
{
    /// <summary>Один ефект предмета. Предмет може мати кілька ефектів, зокрема побічні.</summary>
    public abstract class ItemEffect : ScriptableObject
    {
        /// <summary>Що робить ефект: цей текст гравець бачить в інвентарі.</summary>
        public abstract string Description { get; }

        public abstract void Apply();
    }
}
