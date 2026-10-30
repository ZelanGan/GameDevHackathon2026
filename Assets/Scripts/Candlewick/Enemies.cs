using UnityEngine;

namespace Candlewick
{
    public abstract class Enemy
    {
        public Vector2 Pos;
        public float Radius;
        public float Hp;
        public Room Home;
        public bool Dead;

        protected readonly GameManager Game;
        protected readonly GameObject Root;
        protected readonly SpriteRenderer Sprite;
        protected Vector2 Knock;
        protected float Age;
        protected Color Tint;
        float flash, nextHitFx;

        public virtual bool Solid => true;
        public virtual bool Targetable => true;

        protected Enemy(GameManager game, Vector2 pos, Room home, string name, Sprite sprite, Color tint, float scale, float radius, float hp)
        {
            Game = game;
            Pos = pos;
            Home = home;
            Radius = radius;
            Hp = hp * (1f + 0.25f * (game.Depth - 1));
            Tint = tint;
            Root = new GameObject(name);
            Root.transform.SetParent(game.WorldRoot, false);
            Root.transform.localPosition = pos;
            Sprite = Art.Spr("Body", Root.transform, sprite, Art.Lit, tint, 10, scale).GetComponent<SpriteRenderer>();
            game.Particles.Burst(pos, 8, Art.Gloom, 2f);
        }

        protected void AddEyes(float spacing, float y, float size, Color color)
        {
            Art.Spr("Eye", Root.transform, Art.Disk, Art.Glow, color, 11, size).transform.localPosition = new Vector3(-spacing, y, 0f);
            Art.Spr("Eye", Root.transform, Art.Disk, Art.Glow, color, 11, size).transform.localPosition = new Vector3(spacing, y, 0f);
        }

        public void Tick(float dt)
        {
            Age += dt;
            Act(dt);
            Knock = Vector2.Lerp(Knock, Vector2.zero, 1f - Mathf.Exp(-dt * 9f));
            Pos = Game.Map.Move(Pos, Knock * dt, Radius);

            var player = Game.Player;
            if (Vector2.Distance(Pos, player.Pos) < Radius + 0.36f && ContactDamage > 0f)
                player.Hurt(ContactDamage, (player.Pos - Pos).normalized * 0.4f);

            flash -= dt;
            Sprite.color = flash > 0f ? Color.white : Tint;
            Root.transform.localPosition = Pos;
        }

        protected abstract void Act(float dt);
        protected abstract float ContactDamage { get; }
        protected abstract int WaxMin { get; }
        protected abstract int WaxMax { get; }

        protected Vector2 ChaseDir()
        {
            var target = Game.Player.Pos;
            return Game.Map.LineOfSight(Pos, target) ? (target - Pos).normalized : Game.Map.FlowDir(Pos);
        }

        public void Push(Vector2 delta) => Pos = Game.Map.Move(Pos, delta, Radius);

        public void Damage(float amount, Vector2 knock, bool sparkHit)
        {
            if (Dead) return;
            Hp -= amount;
            Knock += knock;
            if (sparkHit && Age >= nextHitFx)
            {
                // A stream lands dozens of hits a second; only some of them should flash and crackle.
                nextHitFx = Age + 0.12f;
                flash = 0.06f;
                Game.Sound.Hit();
                Game.Particles.Burst(Pos, 3, Art.Flame, 3f);
            }
            if (Hp <= 0f && !Dissipate()) Die(true);
        }

        // Lets an enemy survive reaching zero health.
        protected virtual bool Dissipate() => false;

        public void Die(bool dropWax)
        {
            if (Dead) return;
            Dead = true;
            if (dropWax)
            {
                Game.Kills++;
                Game.Sound.Kill();
                Game.Particles.Burst(Pos, 14, Art.Gloom, 4f);
                Game.Particles.Burst(Pos, 6, Art.Ember, 3f);
                if (Random.value < Game.Buffs.DropChance)
                    Game.SpawnDrop(Pos, Random.Range(WaxMin, WaxMax + 1) + Game.Buffs.DropBonus);
            }
            Object.Destroy(Root);
        }
    }

    /// <summary>Fast and fragile; rushes straight at the flame.</summary>
    public class ShadowMoth : Enemy
    {
        readonly float phase = Random.value * 10f;
        readonly Vector3 baseScale = Vector3.one * 0.85f;

        public ShadowMoth(GameManager game, Vector2 pos, Room home)
            : base(game, pos, home, "Shadow Moth", Art.Moth, new Color(0.85f, 0.75f, 1f), 0.85f, 0.28f, 14f)
        {
            AddEyes(0.07f, 0f, 0.09f, new Color(1f, 0.2f, 0.12f));
        }

        protected override float ContactDamage => 6f;
        protected override int WaxMin => 2;
        protected override int WaxMax => 3;

        protected override void Act(float dt)
        {
            Vector2 dir = ChaseDir();
            Vector2 wobble = new Vector2(-dir.y, dir.x) * Mathf.Sin(Age * 7f + phase) * 0.5f;
            Pos = Game.Map.Move(Pos, (dir + wobble) * 5.8f * dt, Radius);
            Sprite.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            Sprite.transform.localScale = new Vector3(baseScale.x, baseScale.y * (0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Age * 20f + phase))), 1f);
        }
    }

    /// <summary>Keeps its distance and fires bolts that smother the player's flame.</summary>
    public class GloomSnuffer : Enemy
    {
        float cooldown = Random.Range(1f, 2.2f);

        public GloomSnuffer(GameManager game, Vector2 pos, Room home)
            : base(game, pos, home, "Gloom Snuffer", Art.Snuffer, Color.white, 1.25f, 0.5f, 34f)
        {
            AddEyes(0.11f, 0.03f, 0.1f, new Color(0.75f, 0.45f, 1f));
        }

        protected override float ContactDamage => 4f;
        protected override int WaxMin => 3;
        protected override int WaxMax => 4;

        protected override void Act(float dt)
        {
            var target = Game.Player.Pos;
            float d = Vector2.Distance(Pos, target);
            bool sees = Game.Map.LineOfSight(Pos, target);
            Vector2 dir;
            if (!sees || d > 8.5f) dir = ChaseDir();
            else if (d < 5.5f) dir = (Pos - target).normalized;
            else dir = Vector2.Perpendicular((target - Pos).normalized) * Mathf.Sin(Age * 0.8f);
            Pos = Game.Map.Move(Pos, dir * 2.6f * dt, Radius);

            cooldown -= dt;
            if (cooldown > 0f || !sees || d > 13f) return;
            cooldown = Mathf.Max(1.3f, 2.3f - 0.15f * Game.Depth);
            Vector2 aim = (target - Pos).normalized;
            int shots = Game.Depth >= 3 ? 3 : 1;
            for (int i = 0; i < shots; i++)
            {
                float spread = (i - (shots - 1) / 2f) * 14f;
                Game.SpawnBolt(Pos + aim * 0.6f, (Vector2)(Quaternion.Euler(0f, 0f, spread) * aim) * 7f);
            }
        }
    }

    /// <summary>Hunts the player through walls and cannot be killed: fire only scatters it for a while. Lit candles keep it out.</summary>
    public class Ghost : Enemy
    {
        const float ScatterSeconds = 7f, TouchSeconds = 4f, TouchDamage = 14f;
        readonly float fullHp, speed;
        float hiddenFor, slowedFor, lastHp;

        public Ghost(GameManager game, Vector2 pos)
            : base(game, pos, null, "Ghost", Art.Ghost, new Color(0.75f, 0.86f, 1f, 0.8f), 1.5f, 0.5f, 60f)
        {
            Sprite.sharedMaterial = Art.Glow;
            Sprite.sortingOrder = 13;
            Art.Spr("Aura", Root.transform, Art.Soft, Art.Glow, new Color(0.5f, 0.72f, 1f, 0.3f), 12, 3.2f);
            fullHp = lastHp = Hp;
            speed = Mathf.Min(3.4f + 0.3f * game.Depth, 4.6f);
            game.Sound.Moan();
        }

        public override bool Solid => false;
        public override bool Targetable => hiddenFor <= 0f;
        protected override float ContactDamage => 0f;
        protected override int WaxMin => 0;
        protected override int WaxMax => 0;

        protected override void Act(float dt)
        {
            if (hiddenFor > 0f)
            {
                hiddenFor -= dt;
                if (hiddenFor <= 0f) Reappear();
                return;
            }

            // Burning it slows it down, so holding the flame on it buys time.
            if (Hp < lastHp) slowedFor = 0.35f;
            lastHp = Hp;
            slowedFor -= dt;

            var player = Game.Player;
            Vector2 dir = (player.Pos - Pos).normalized;
            Vector2 next = Pos + dir * speed * (slowedFor > 0f ? 0.35f : 1f) * dt;
            if (Game.IsInSanctuary(Pos) || !Game.IsInSanctuary(next)) Pos = next;

            Sprite.transform.localPosition = new Vector3(0f, Mathf.Sin(Age * 3f) * 0.12f, 0f);
            Tint.a = 0.62f + 0.22f * Mathf.Sin(Age * 5f);

            if (Vector2.Distance(Pos, player.Pos) >= Radius + 0.3f) return;
            player.Hurt(TouchDamage, dir * 0.4f);
            Vanish(TouchSeconds);
        }

        protected override bool Dissipate()
        {
            Game.Sound.Kill();
            Vanish(ScatterSeconds);
            return true;
        }

        void Vanish(float seconds)
        {
            Game.Particles.Burst(Pos, 16, new Color(0.7f, 0.85f, 1f), 4f);
            hiddenFor = seconds;
            Hp = lastHp = fullHp;
            Root.SetActive(false);
        }

        void Reappear()
        {
            Pos = Game.GhostSpawnPoint();
            Root.transform.localPosition = Pos;
            Root.SetActive(true);
            Game.Sound.Moan();
        }
    }

    /// <summary>Slow, heavy, and leaves a trail of darkness that eats wax.</summary>
    public class NightCreeper : Enemy
    {
        float trailTimer;

        public NightCreeper(GameManager game, Vector2 pos, Room home)
            : base(game, pos, home, "Night Creeper", Art.Creeper, Color.white, 1.9f, 0.72f, 120f)
        {
            AddEyes(0.2f, 0.15f, 0.14f, new Color(1f, 0.32f, 0.1f));
        }

        protected override float ContactDamage => 13f;
        protected override int WaxMin => 4;
        protected override int WaxMax => 5;

        protected override void Act(float dt)
        {
            Pos = Game.Map.Move(Pos, ChaseDir() * 1.55f * dt, Radius);
            float pulse = 1.9f + Mathf.Sin(Age * 2.4f) * 0.08f;
            Sprite.transform.localScale = new Vector3(pulse, 3.8f - pulse, 1f);
            trailTimer -= dt;
            if (trailTimer > 0f) return;
            trailTimer = 0.45f;
            Game.SpawnTrail(Pos);
        }
    }
}
