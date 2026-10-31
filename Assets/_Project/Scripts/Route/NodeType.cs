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
                case NodeType.Depot: return "Депо";
                case NodeType.Supermarket: return "Супермаркет";
                case NodeType.GunShop: return "Магазин зброї";
                case NodeType.Event: return "Подія";
                case NodeType.EmptyRoad: return "Порожня дорога";
                default: return "Кінцева";
            }
        }

        /// <summary>Що гравець отримує на цьому вузлі: підказка на мапі.</summary>
        public static string Benefit(this NodeType type)
        {
            switch (type)
            {
                case NodeType.Depot: return "+ енергія";
                case NodeType.Supermarket: return "+ свічки";
                case NodeType.GunShop: return "зброя за квитки";
                case NodeType.Event: return "вибір і наслідки";
                case NodeType.EmptyRoad: return "без зупинки";
                default: return "фінал";
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
