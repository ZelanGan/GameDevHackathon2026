using System.Collections.Generic;
using UnityEngine;

namespace Candlewick
{
    public enum BuffType { MaxWax, FireResist, WaxDrop, BurningAura, QuickWick, LongFlame }

    public class BuffInfo
    {
        public BuffType Type;
        public string Name, Description, Glyph;
        public Color Color;
    }

    /// <summary>Passive buffs granted by rescued candles. Every buff stacks.</summary>
    public class BuffSet
    {
        public static readonly BuffInfo[] All =
        {
            new BuffInfo { Type = BuffType.MaxWax, Name = "Tallow Heart", Description = "+25 max wax, and refills that much", Glyph = "W", Color = new Color(0.96f, 0.88f, 0.72f) },
            new BuffInfo { Type = BuffType.FireResist, Name = "Fire Resistance", Description = "Melt and take 20% less", Glyph = "R", Color = new Color(0.45f, 0.65f, 1f) },
            new BuffInfo { Type = BuffType.WaxDrop, Name = "Wax Harvest", Description = "More drops, each worth +1 wax", Glyph = "D", Color = new Color(1f, 0.82f, 0.3f) },
            new BuffInfo { Type = BuffType.BurningAura, Name = "Burning Aura", Description = "Scorch enemies that come close", Glyph = "A", Color = new Color(1f, 0.35f, 0.1f) },
            new BuffInfo { Type = BuffType.QuickWick, Name = "Quick Wick", Description = "Your flame burns 20% hotter", Glyph = "Q", Color = new Color(1f, 0.6f, 0.2f) },
            new BuffInfo { Type = BuffType.LongFlame, Name = "Long Flame", Description = "Your flame reaches 20% farther", Glyph = "L", Color = new Color(1f, 0.95f, 0.65f) },
        };

        readonly Dictionary<BuffType, int> stacks = new Dictionary<BuffType, int>();
        public IReadOnlyDictionary<BuffType, int> Stacks => stacks;

        public int Level(BuffType type) => stacks.TryGetValue(type, out int n) ? n : 0;
        public void Add(BuffType type) => stacks[type] = Level(type) + 1;

        public float DamageTakenMultiplier => Mathf.Pow(0.8f, Level(BuffType.FireResist));
        public float DropChance => Mathf.Min(0.65f + 0.2f * Level(BuffType.WaxDrop), 1f);
        public int DropBonus => Level(BuffType.WaxDrop);
        public float AuraRadius => Level(BuffType.BurningAura) == 0 ? 0f : 2.2f + 0.5f * Level(BuffType.BurningAura);
        public float AuraDps => 14f * Level(BuffType.BurningAura);
        public float FireRateMultiplier => Mathf.Pow(1.2f, Level(BuffType.QuickWick));
        public float RangeMultiplier => 1f + 0.2f * Level(BuffType.LongFlame);

        public static List<BuffInfo> RollChoices(int count)
        {
            var pool = new List<BuffInfo>(All);
            var picks = new List<BuffInfo>();
            for (int i = 0; i < count; i++)
            {
                int j = Random.Range(0, pool.Count);
                picks.Add(pool[j]);
                pool.RemoveAt(j);
            }
            return picks;
        }

        public static BuffInfo Info(BuffType type)
        {
            foreach (var b in All) if (b.Type == type) return b;
            return null;
        }
    }
}
