using System.Collections.Generic;

namespace NightTrolleybus.Route
{
    /// <summary>Вузол мапи маршруту.</summary>
    public class RouteNode
    {
        public int Id { get; }
        public int Layer { get; }
        public int Index { get; }
        public int LayerSize { get; }
        public NodeType Type { get; }
        public List<RouteNode> Next { get; } = new List<RouteNode>();

        public RouteNode(int id, int layer, int index, int layerSize, NodeType type)
        {
            Id = id;
            Layer = layer;
            Index = index;
            LayerSize = layerSize;
            Type = type;
        }
    }
}
