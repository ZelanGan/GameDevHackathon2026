using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Candlewick
{
    /// <summary>One puff of the flamethrower stream: it slows and swells, burns everything it touches once, and stops at walls.</summary>
    public class FlamePuff
    {
        const float EndSpeed = 0.35f, Knock = 0.22f;
        public bool Dead;
        readonly GameManager game;
        readonly GameObject go;
        readonly SpriteRenderer sprite;
        readonly float damage, maxLife, startSize, endSize;
        readonly Vector2 startVel;
        readonly HashSet<Enemy> struck = new HashSet<Enemy>();
        Vector2 pos;
        float life;

        public FlamePuff(GameManager game, Vector2 pos, Vector2 vel, float damage, float startSize, float endSize, float life)
        {
            this.game = game;
            this.pos = pos;
            startVel = vel;
            this.damage = damage;
            this.startSize = startSize;
            this.endSize = endSize;
            this.life = maxLife = life;
            go = Art.Spr("Flame", game.WorldRoot, Art.Soft, Art.Glow, Art.Core, 15, startSize);
            go.transform.localPosition = pos;
            sprite = go.GetComponent<SpriteRenderer>();
        }

        public void Tick(float dt)
        {
            life -= dt;
            if (life <= 0f) { Kill(); return; }
            float age = 1f - life / maxLife;
            pos += startVel * Mathf.Lerp(1f, EndSpeed, age) * dt;
            if (!game.Map.IsOpen(pos))
            {
                if (Random.value < 0.25f) game.Particles.Burst(pos - startVel.normalized * 0.1f, 2, Art.Ember, 2f);
                Kill();
                return;
            }

            float size = Mathf.Lerp(startSize, endSize, age);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(size, size, 1f);
            Color color = age < 0.35f ? Color.Lerp(Art.Core, Art.Flame, age / 0.35f) : Color.Lerp(Art.Flame, Art.Blood, (age - 0.35f) / 0.65f);
            color.a = Mathf.Lerp(1f, 0.15f, age * age);
            sprite.color = color;

            float reach = size * 0.2f;
            foreach (var enemy in game.Enemies)
            {
                if (enemy.Dead || !enemy.Targetable || struck.Contains(enemy)) continue;
                if (Vector2.Distance(pos, enemy.Pos) > enemy.Radius + reach) continue;
                enemy.Damage(damage, startVel.normalized * Knock, true);
                struck.Add(enemy);
            }
        }

        public void Kill() { Dead = true; Object.Destroy(go); }
    }

    /// <summary>Gloom Snuffer projectile: costs wax and forces the flame down to dim.</summary>
    public class DarkBolt
    {
        public bool Dead;
        readonly GameManager game;
        readonly GameObject go;
        Vector2 pos, vel;
        float life = 3.5f;

        public DarkBolt(GameManager game, Vector2 pos, Vector2 vel)
        {
            this.game = game;
            this.pos = pos;
            this.vel = vel;
            go = Art.Spr("Bolt", game.WorldRoot, Art.Disk, Art.Glow, new Color(0.03f, 0.01f, 0.06f), 14, 0.38f);
            Art.Spr("Rim", go.transform, Art.Soft, Art.Glow, new Color(0.62f, 0.25f, 1f, 0.8f), 13, 2.4f);
        }

        public void Tick(float dt)
        {
            life -= dt;
            pos += vel * dt;
            go.transform.localPosition = pos;
            if (life <= 0f || !game.Map.IsOpen(pos)) { Kill(); return; }
            if (Vector2.Distance(pos, game.Player.Pos) > 0.5f) return;
            game.Player.Hurt(6f, vel.normalized * 0.2f);
            game.Player.Snuff(2.5f);
            game.Particles.Burst(pos, 10, Art.Gloom, 3f);
            Kill();
        }

        public void Kill() { Dead = true; Object.Destroy(go); }
    }

    public class WaxDrop
    {
        const float MagnetRange = 3.2f;
        public bool Dead;
        readonly GameManager game;
        readonly GameObject go;
        readonly int amount;
        Vector2 pos;
        float age;

        public WaxDrop(GameManager game, Vector2 pos, int amount)
        {
            this.game = game;
            this.pos = pos;
            this.amount = amount;
            go = Art.Spr("Wax", game.WorldRoot, Art.Disk, Art.Glow, Art.Wax, 6, 0.18f + amount * 0.025f);
            Art.Spr("Glow", go.transform, Art.Soft, Art.Glow, new Color(1f, 0.7f, 0.2f, 0.6f), 5, 3f);
        }

        public void Tick(float dt)
        {
            age += dt;
            var player = game.Player;
            float d = Vector2.Distance(pos, player.Pos);
            if (d < MagnetRange) pos = Vector2.MoveTowards(pos, player.Pos, dt * (12f - d * 2.5f));
            go.transform.localPosition = pos + Vector2.up * Mathf.Sin(age * 4f) * 0.06f;
            if (d < 0.55f)
            {
                player.ChangeWax(amount);
                game.Sound.Pickup();
                game.Particles.Burst(pos, 6, Art.Flame, 2f);
                Kill();
            }
            else if (age > 22f) Kill();
        }

        public void Kill() { Dead = true; Object.Destroy(go); }
    }

    /// <summary>Night Creeper residue; drains wax while the player stands in it.</summary>
    public class GloomTrail
    {
        public const float DrainPerSecond = 7f;
        const float Lifetime = 6f, Reach = 0.72f;
        public bool Dead;
        readonly GameManager game;
        readonly GameObject go;
        readonly SpriteRenderer sprite, rim;
        readonly Vector2 pos;
        float life = Lifetime;

        public GloomTrail(GameManager game, Vector2 pos)
        {
            this.game = game;
            this.pos = pos;
            // The solid disk matches the drain radius exactly; the rim makes it readable on dark floor.
            go = Art.Spr("Trail", game.WorldRoot, Art.Disk, Art.Glow, Color.black, 2, Reach * 2f);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(Reach * 2f, Reach * 0.7f, 1f); // A puddle lying on the ground.
            sprite = go.GetComponent<SpriteRenderer>();
            rim =Art.Spr("Rim", go.transform, Art.Soft, Art.Glow, Color.clear, 1, 1.7f).GetComponent<SpriteRenderer>();
        }

        public void Tick(float dt)
        {
            life -= dt;
            float fade = Mathf.Clamp01(life);
            sprite.color = new Color(0.03f, 0f, 0.06f, 0.88f * fade);
            rim.color = new Color(0.5f, 0.15f, 0.8f, 0.4f * fade);
            if (life <= 0f) Kill();
        }

        public bool Touches(Vector2 p) => Vector2.Distance(pos, p) < Reach;
        public bool IsNear(Vector2 p, float range) => Vector2.Distance(pos, p) < range;

        public void Kill() { Dead = true; Object.Destroy(go); }
    }

    /// <summary>An unlit candle. Once lit it is a light source, a melt-free sanctuary and a checkpoint.</summary>
    public class Altar
    {
        public const float InteractRange = 1.9f, SanctuaryRange = 3.5f;
        public readonly Vector2 Pos;
        public readonly Room Room;
        public bool IsLit;
        public bool HasRekindle;

        readonly GameObject root;
        readonly SpriteRenderer flame, body, hint;
        readonly Light2D light;
        float pulse;

        public Altar(GameManager game, Room room, Vector2 pos)
        {
            Room = room;
            Pos = pos;
            room.Altar = this;
            root = new GameObject("Altar");
            root.transform.SetParent(game.WorldRoot, false);
            root.transform.localPosition = pos;
            var plinth = Art.Spr("Plinth", root.transform, Art.Square, Art.Lit, new Color(0.45f, 0.42f, 0.55f), 4);
            plinth.transform.localScale = new Vector3(1.3f, 0.3f, 1f);
            plinth.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            var candle = Art.Spr("Candle", root.transform, Art.Square, Art.Glow, Art.WaxDark, 5);
            candle.transform.localScale = new Vector3(0.5f, 1f, 1f);
            candle.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            body = candle.GetComponent<SpriteRenderer>();
            var wick = Art.Spr("Wick", root.transform, Art.Square, Art.Glow, Color.black, 6);
            wick.transform.localScale = new Vector3(0.05f, 0.14f, 1f);
            wick.transform.localPosition = new Vector3(0f, 0.66f, 0f);
            hint = Art.Spr("Hint", root.transform, Art.Soft, Art.Glow, new Color(0.65f, 0.55f, 0.9f, 0.3f), 3, 2.6f).GetComponent<SpriteRenderer>();
            flame = Art.Spr("Flame", root.transform, Art.Soft, Art.Glow, Art.Core, 7).GetComponent<SpriteRenderer>();
            flame.transform.localPosition = new Vector3(0f, 0.93f, 0f);
            flame.transform.localScale = new Vector3(0.45f, 0.75f, 1f);
            flame.enabled = false;
            light = Art.AddLight(root, new Color(1f, 0.74f, 0.4f), 8f, 1f);
            light.enabled = false;
        }

        public void Tick(GameManager game, float dt)
        {
            pulse += dt;
            if (!IsLit)
            {
                hint.color = new Color(0.65f, 0.55f, 0.9f, 0.18f + 0.14f * Mathf.Sin(pulse * 2.5f));
                return;
            }
            float flicker = Mathf.PerlinNoise(pulse * 7f, Pos.x) * 0.2f;
            light.intensity = 0.95f + flicker;
            if (Random.value < dt * 8f)
                game.Particles.Emit(Pos + new Vector2(0f, 1f), new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.7f, 1.5f)), Art.Flame, 0.09f, 0.6f);
        }

        public void Light()
        {
            IsLit = true;
            HasRekindle = true;
            flame.enabled = true;
            light.enabled = true;
            body.color = Art.Wax;
            hint.color = new Color(1f, 0.6f, 0.2f, 0.5f);
        }
    }

    /// <summary>The way out of the current floor. Sealed until enough candles burn.</summary>
    public class Gate
    {
        public const float InteractRange = 2.2f;
        public readonly Vector2 Pos;
        public bool IsOpen { get; private set; }

        readonly SpriteRenderer portal, halo;
        readonly Light2D light;
        float pulse;

        public Gate(GameManager game, Vector2 pos)
        {
            Pos = pos;
            var root = new GameObject("Gate");
            root.transform.SetParent(game.WorldRoot, false);
            root.transform.localPosition = pos;
            var frame = Art.Spr("Frame", root.transform, Art.Square, Art.Lit, new Color(0.62f, 0.55f, 0.68f), 4);
            frame.transform.localScale = new Vector3(2.6f, 3f, 1f);
            portal = Art.Spr("Portal", root.transform, Art.Square, Art.Glow, new Color(0.03f, 0.02f, 0.06f), 5).GetComponent<SpriteRenderer>();
            portal.transform.localScale = new Vector3(1.9f, 2.5f, 1f);
            portal.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            halo = Art.Spr("Halo", root.transform, Art.Soft, Art.Glow, new Color(0.65f, 0.55f, 0.9f, 0.25f), 3, 5f).GetComponent<SpriteRenderer>();
            light = Art.AddLight(root, new Color(0.7f, 0.82f, 1f), 7f, 1.2f);
            light.enabled = false;
        }

        public void Tick(float dt)
        {
            pulse += dt;
            float wave = 0.5f + 0.5f * Mathf.Sin(pulse * 3f);
            if (!IsOpen)
            {
                halo.color = new Color(0.65f, 0.55f, 0.9f, 0.12f + 0.12f * wave);
                return;
            }
            halo.color = new Color(0.75f, 0.87f, 1f, 0.45f + 0.25f * wave);
            light.intensity = 1.1f + 0.3f * wave;
        }

        public void Open()
        {
            IsOpen = true;
            portal.color = new Color(0.85f, 0.93f, 1f);
            light.enabled = true;
        }
    }

    /// <summary>Pooled sprite particles for embers, bursts and smoke.</summary>
    public class Particles
    {
        class P { public SpriteRenderer Sr; public Vector2 Pos, Vel; public float Life, MaxLife, Size; public Color Color; }

        const int Capacity = 400;
        readonly List<P> pool = new List<P>(Capacity);
        int next;

        public Particles(Transform parent)
        {
            var root = new GameObject("Particles").transform;
            root.SetParent(parent, false);
            for (int i = 0; i < Capacity; i++)
            {
                var sr = Art.Spr("p", root, Art.Soft, Art.Glow, Color.white, 20).GetComponent<SpriteRenderer>();
                sr.enabled = false;
                pool.Add(new P { Sr = sr });
            }
        }

        public void Emit(Vector2 pos, Vector2 vel, Color color, float size, float life)
        {
            var p = pool[next];
            next = (next + 1) % Capacity;
            p.Pos = pos;
            p.Vel = vel;
            p.Color = color;
            p.Size = size;
            p.Life = p.MaxLife = life;
            p.Sr.enabled = true;
        }

        public void Burst(Vector2 pos, int count, Color color, float speed)
        {
            for (int i = 0; i < count; i++)
                Emit(pos, Random.insideUnitCircle * speed, color, Random.Range(0.1f, 0.26f), Random.Range(0.25f, 0.6f));
        }

        public void Tick(float dt)
        {
            foreach (var p in pool)
            {
                if (p.Life <= 0f) continue;
                p.Life -= dt;
                if (p.Life <= 0f) { p.Sr.enabled = false; continue; }
                p.Pos += p.Vel * dt;
                p.Vel *= 1f - 2f * dt;
                float t = p.Life / p.MaxLife;
                p.Sr.transform.localPosition = p.Pos;
                p.Sr.transform.localScale = Vector3.one * p.Size * (0.5f + t * 0.8f);
                p.Sr.color = new Color(p.Color.r, p.Color.g, p.Color.b, t);
            }
        }
    }
}
