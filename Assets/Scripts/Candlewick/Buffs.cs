using System.Collections.Generic;
using UnityEngine;

namespace Candlewick
{
    public enum BuffType { MaxWax, FireResist, WaxDrop, BurningAura, QuickWick, LongFlame, SteadyFlame }

    public class BuffInfo
    {
        public BuffType Type;
        public string NameEn, NameUk, DescriptionEn, DescriptionUk, Glyph;
        public Color Color;
        public string Name => Loc.T(NameEn, NameUk);
        public string Description => Loc.T(DescriptionEn, DescriptionUk);
    }

    /// <summary>Passive buffs granted by lit candles. Every buff stacks.</summary>
    public class BuffSet
    {
        public static readonly BuffInfo[] All =
        {
            new BuffInfo { Type = BuffType.MaxWax, NameEn = "Inner Sun", NameUk = "Внутрішнє сонце", DescriptionEn = "+25 max light, and refills that much", DescriptionUk = "+25 до макс. світла й стільки ж відновлює", Glyph = "W", Color = new Color(0.96f, 0.88f, 0.72f) },
            new BuffInfo { Type = BuffType.FireResist, NameEn = "Fire Resistance", NameUk = "Стійкість до вогню", DescriptionEn = "Fade and take 20% less", DescriptionUk = "Згасання та шкода менші на 20%", Glyph = "R", Color = new Color(0.45f, 0.65f, 1f) },
            new BuffInfo { Type = BuffType.WaxDrop, NameEn = "Light Harvest", NameUk = "Збір світла", DescriptionEn = "More drops, each worth +1 light", DescriptionUk = "Більше іскор, кожна дає +1 світла", Glyph = "D", Color = new Color(1f, 0.82f, 0.3f) },
            new BuffInfo { Type = BuffType.BurningAura, NameEn = "Burning Aura", NameUk = "Палюча аура", DescriptionEn = "Scorch enemies that come close", DescriptionUk = "Обпалює ворогів поруч", Glyph = "A", Color = new Color(1f, 0.35f, 0.1f) },
            new BuffInfo { Type = BuffType.QuickWick, NameEn = "Blazing Ray", NameUk = "Палкий промінь", DescriptionEn = "Your flame burns 20% hotter", DescriptionUk = "Полум'я пече на 20% сильніше", Glyph = "Q", Color = new Color(1f, 0.6f, 0.2f) },
            new BuffInfo { Type = BuffType.LongFlame, NameEn = "Long Flame", NameUk = "Довге полум'я", DescriptionEn = "Your flame reaches 20% farther", DescriptionUk = "Полум'я сягає на 20% далі", Glyph = "L", Color = new Color(1f, 0.95f, 0.65f) },
            new BuffInfo { Type = BuffType.SteadyFlame, NameEn = "Steady Flame", NameUk = "Рівне полум'я", DescriptionEn = "Attacks cost 30% less light", DescriptionUk = "Атаки витрачають на 30% менше світла", Glyph = "S", Color = new Color(0.65f, 1f, 0.75f) },
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
        public float AttackCostMultiplier => Mathf.Pow(0.7f, Level(BuffType.SteadyFlame));
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
