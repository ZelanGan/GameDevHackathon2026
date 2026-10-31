using NightTrolleybus.Core;

namespace NightTrolleybus.Route
{
    public enum NodeType
    {
        Depot,
        Supermarket,
        GunShop,
        Event,
        EmptyRoad,
        Final
    }

    public static class NodeTypeExtensions
    {
        public static string DisplayName(this NodeType type)
        {
            switch (type)
            {
                case NodeType.Depot: return Loc.T("Депо");
                case NodeType.Supermarket: return Loc.T("Супермаркет");
                case NodeType.GunShop: return Loc.T("Магазин зброї");
                case NodeType.Event: return Loc.T("Подія");
                case NodeType.EmptyRoad: return Loc.T("Порожня дорога");
                default: return Loc.T("Кінцева");
            }
        }

        /// <summary>Що гравець отримує на цьому вузлі: підказка на мапі.</summary>
        public static string Benefit(this NodeType type)
        {
            switch (type)
            {
                case NodeType.Depot: return Loc.T("+ енергія");
                case NodeType.Supermarket: return Loc.T("+ свічки");
                case NodeType.GunShop: return Loc.T("зброя за квитки");
                case NodeType.Event: return Loc.T("вибір і наслідки");
                case NodeType.EmptyRoad: return Loc.T("без зупинки");
                default: return Loc.T("фінал");
            }
        }

        /// <summary>Сцена зупинки або null, якщо на цьому вузлі пішки не виходять.</summary>
        public static string StopScene(this NodeType type)
        {
            switch (type)
            {
                case NodeType.Depot: return "Stop_Depot";
                case NodeType.Supermarket: return "Stop_Market";
                case NodeType.GunShop: return "Stop_GunShop";
                default: return null;
            }
        }
    }
}
