using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightTrolleybus.Route
{
    /// <summary>Мапа маршруту й рух по ній. Живе в Boot.</summary>
    public class RouteManager : MonoBehaviour
    {
        public event Action<RouteNode> NodeSelected;

        private readonly List<int> visited = new List<int>();

        public int Seed { get; private set; }
        public IReadOnlyList<RouteNode> Nodes { get; private set; }
        public IReadOnlyList<int> Visited => visited;
        public RouteNode Current { get; private set; }
        public RouteNode Target { get; private set; }

        public void Generate(int seed)
        {
            Seed = seed;
            Nodes = RouteGenerator.Generate(seed);
            visited.Clear();
            Current = null;
            Target = null;
        }

        /// <summary>Відновлює прогрес зі збереження.</summary>
        public void Restore(int seed, IEnumerable<int> visitedIds, int currentId)
        {
            Generate(seed);
            visited.AddRange(visitedIds);
            Current = currentId >= 0 ? Nodes[currentId] : null;
        }

        /// <summary>Скільки зупинок уже пройдено, разом із поточною. Події й порожня дорога не рахуються.</summary>
        public int StopsVisited
        {
            get
            {
                int count = 0;
                foreach (int id in visited)
                {
                    if (Nodes[id].Type.StopScene() != null)
                        count++;
                }

                return count;
            }
        }

        public bool IsAvailable(RouteNode node) =>
            Current != null ? Current.Next.Contains(node) : node.Layer == 0;

        public void Select(RouteNode node)
        {
            if (!IsAvailable(node))
                return;

            Target = node;
            NodeSelected?.Invoke(node);
        }

        public RouteNode CompleteTravel()
        {
            Current = Target;
            Target = null;
            visited.Add(Current.Id);
            return Current;
        }
    }
}
