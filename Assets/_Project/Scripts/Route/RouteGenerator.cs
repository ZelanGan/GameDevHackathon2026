using System;
using System.Collections.Generic;

namespace NightTrolleybus.Route
{
    /// <summary>Будує розгалужену мапу із seed. Той самий seed дає ту саму мапу, тому в збереження йде лише він.</summary>
    public static class RouteGenerator
    {
        private const int MiddleLayers = 5;
        private const int MinWidth = 2;
        private const int MaxWidth = 3;

        private static readonly NodeType[] RandomTypes =
        {
            NodeType.Depot, NodeType.Depot,
            NodeType.Supermarket, NodeType.Supermarket,
            NodeType.GunShop,
            NodeType.Event, NodeType.Event,
            NodeType.EmptyRoad
        };

        public static List<RouteNode> Generate(int seed)
        {
            var random = new Random(seed);
            var layers = new List<List<RouteNode>>();
            var nodes = new List<RouteNode>();

            for (int layer = 0; layer < MiddleLayers; layer++)
            {
                int width = random.Next(MinWidth, MaxWidth + 1);
                var current = new List<RouteNode>();
                for (int index = 0; index < width; index++)
                {
                    NodeType type = RandomTypes[random.Next(RandomTypes.Length)];
                    var node = new RouteNode(nodes.Count, layer, index, width, type);
                    current.Add(node);
                    nodes.Add(node);
                }

                layers.Add(current);
            }

            var final = new RouteNode(nodes.Count, MiddleLayers, 0, 1, NodeType.Final);
            nodes.Add(final);
            layers.Add(new List<RouteNode> { final });

            for (int layer = 0; layer < layers.Count - 1; layer++)
                Connect(layers[layer], layers[layer + 1], random);

            return nodes;
        }

        private static void Connect(List<RouteNode> from, List<RouteNode> to, Random random)
        {
            // Кожен вузол веде до найближчого за положенням у наступному шарі, інколи ще до сусіднього.
            foreach (RouteNode node in from)
            {
                int target = Project(node.Index, from.Count, to.Count);
                node.Next.Add(to[target]);

                int neighbour = target + (random.Next(2) == 0 ? -1 : 1);
                if (random.Next(2) == 0 && neighbour >= 0 && neighbour < to.Count)
                    node.Next.Add(to[neighbour]);
            }

            // Жоден вузол не має лишитися без входу.
            foreach (RouteNode node in to)
            {
                if (from.Exists(other => other.Next.Contains(node)))
                    continue;

                from[Project(node.Index, to.Count, from.Count)].Next.Add(node);
            }
        }

        private static int Project(int index, int fromCount, int toCount)
        {
            if (fromCount <= 1)
                return (toCount - 1) / 2;

            return (int)Math.Round(index * (toCount - 1) / (double)(fromCount - 1));
        }
    }
}
