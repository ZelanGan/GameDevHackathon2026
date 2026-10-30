using UnityEngine;

namespace Candlewick
{
    /// <summary>The colours of one day: wall paint, doors and posters, the night sky and the darkness itself.</summary>
    public class Palette
    {
        public string Name;
        public Color[] Walls;
        public Color Lobby, Door, Poster, NightSky, Ambient;

        public static readonly Palette[] All =
        {
            new Palette
            {
                Name = "Mint",
                Walls = new[] { new Color(0.62f, 0.9f, 0.76f), new Color(0.82f, 0.9f, 0.68f), new Color(0.6f, 0.8f, 0.95f) },
                Lobby = new Color(0.9f, 0.85f, 0.65f), Door = new Color(0.85f, 1f, 0.9f), Poster = new Color(0.95f, 0.9f, 0.6f),
                NightSky = new Color(0.08f, 0.16f, 0.26f), Ambient = new Color(0.3f, 0.5f, 0.6f),
            },
            new Palette
            {
                Name = "Peach",
                Walls = new[] { new Color(1f, 0.74f, 0.6f), new Color(1f, 0.86f, 0.62f), new Color(0.88f, 0.68f, 0.62f) },
                Lobby = new Color(1f, 0.8f, 0.72f), Door = new Color(1f, 0.82f, 0.72f), Poster = new Color(0.7f, 0.85f, 0.95f),
                NightSky = new Color(0.2f, 0.1f, 0.24f), Ambient = new Color(0.6f, 0.38f, 0.5f),
            },
            new Palette
            {
                Name = "Lavender",
                Walls = new[] { new Color(0.76f, 0.7f, 1f), new Color(0.92f, 0.72f, 0.96f), new Color(0.64f, 0.76f, 1f) },
                Lobby = new Color(0.85f, 0.8f, 1f), Door = new Color(0.86f, 0.78f, 1f), Poster = new Color(1f, 0.88f, 0.6f),
                NightSky = new Color(0.12f, 0.08f, 0.32f), Ambient = new Color(0.38f, 0.32f, 0.78f),
            },
            new Palette
            {
                Name = "Teal and ochre",
                Walls = new[] { new Color(0.5f, 0.86f, 0.86f), new Color(0.98f, 0.8f, 0.42f), new Color(0.56f, 0.72f, 0.76f) },
                Lobby = new Color(0.95f, 0.78f, 0.5f), Door = new Color(0.72f, 0.95f, 0.95f), Poster = new Color(1f, 0.75f, 0.55f),
                NightSky = new Color(0.04f, 0.18f, 0.22f), Ambient = new Color(0.22f, 0.55f, 0.55f),
            },
            new Palette
            {
                Name = "Rose",
                Walls = new[] { new Color(1f, 0.64f, 0.76f), new Color(0.98f, 0.8f, 0.86f), new Color(0.78f, 0.66f, 0.92f) },
                Lobby = new Color(1f, 0.74f, 0.78f), Door = new Color(1f, 0.78f, 0.84f), Poster = new Color(0.72f, 0.92f, 0.85f),
                NightSky = new Color(0.24f, 0.07f, 0.2f), Ambient = new Color(0.68f, 0.3f, 0.5f),
            },
        };
    }
}
