using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Candlewick
{
    public enum FlameMode { Dim, Normal, Flare }

    /// <summary>The player: wax is health, and the flame mode trades wax for light, speed and firepower.</summary>
    public class PlayerCandle
    {
        const float Radius = 0.34f, BodyHeight = 1.15f, ContactGrace = 0.6f;

        public Vector2 Pos;
        public float Wax, MaxWax = 100f;
        public FlameMode Mode = FlameMode.Normal;
        public float SnuffedFor;
        public Vector2 Aim = Vector2.right;
        public bool Alive = true;

        public FlameMode EffectiveMode => SnuffedFor > 0f ? FlameMode.Dim : Mode;

        readonly GameManager game;
        readonly GameObject root;
        readonly Transform body, wick, flame, halo, aura, arrow, crosshair;
        readonly SpriteRenderer bodySprite, flameSprite, haloSprite, auraSprite, arrowSprite;
        readonly Light2D glow, streamGlow;
        float fireCooldown, grace, hurtFlash, emberTimer, lightRadius = 8f;

        public PlayerCandle(GameManager game, Transform parent, Vector2 pos)
        {
            this.game = game;
            Pos = pos;
            Wax = MaxWax;
            root = new GameObject("Player");
            root.transform.SetParent(parent, false);
            Art.Spr("Pool", root.transform, Art.Disk, Art.Glow, new Color(0.72f, 0.62f, 0.48f), 11, 0.8f).transform.localPosition = new Vector3(0f, -0.42f, 0f);
            var pool = root.transform.GetChild(0);
            pool.localScale = new Vector3(0.85f, 0.3f, 1f);
            body = Art.Spr("Body", root.transform, Art.Square, Art.Glow, Art.Wax, 12).transform;
            bodySprite = body.GetComponent<SpriteRenderer>();
            wick = Art.Spr("Wick", root.transform, Art.Square, Art.Glow, new Color(0.1f, 0.07f, 0.06f), 13).transform;
            wick.localScale = new Vector3(0.05f, 0.15f, 1f);
            halo = Art.Spr("Halo", root.transform, Art.Soft, Art.Glow, new Color(1f, 0.55f, 0.15f, 0.55f), 14).transform;
            haloSprite = halo.GetComponent<SpriteRenderer>();
            flame = Art.Spr("Flame", root.transform, Art.Soft, Art.Glow, Art.Core, 15).transform;
            flameSprite = flame.GetComponent<SpriteRenderer>();
            aura = Art.Spr("Aura", root.transform, Art.Soft, Art.Glow, new Color(1f, 0.3f, 0.05f, 0f), 2).transform;
            auraSprite = aura.GetComponent<SpriteRenderer>();
            arrow = Art.Spr("AltarArrow", parent, Art.Arrow, Art.Glow, Art.Flame, 16, 0.3f).transform;
            arrowSprite = arrow.GetComponent<SpriteRenderer>();
            crosshair = Art.Spr("Crosshair", parent, Art.Disk, Art.Glow, new Color(1f, 0.85f, 0.5f, 0.9f), 30, 0.16f).transform;
            Art.Spr("Ring", crosshair, Art.Soft, Art.Glow, new Color(1f, 0.5f, 0.15f, 0.55f), 29, 3.2f);
            glow = Art.AddLight(root, new Color(1f, 0.72f, 0.42f), lightRadius, 1.15f);
            var streamLight = new GameObject("StreamLight");
            streamLight.transform.SetParent(parent, false);
            streamGlow = Art.AddLight(streamLight, new Color(1f, 0.55f, 0.2f), 4f, 0.75f);
            streamGlow.enabled = false;
        }

        public void Tick(float dt, Camera cam)
        {
            ReadInput(dt, cam);
            Melt(dt);
            Burn(dt);
            UpdateVisuals(dt);
        }

        void ReadInput(float dt, Camera cam)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            Vector2 move = Vector2.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed) move.x -= 1f;
                if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame) ToggleFlare();
                if (kb.qKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) ToggleDim();
                if (kb.eKey.wasPressedThisFrame) game.Interact();
            }
            if (move.sqrMagnitude > 1f) move.Normalize();
            float speed = EffectiveMode == FlameMode.Flare ? 7.5f : EffectiveMode == FlameMode.Dim ? 3.4f : 5f;
            Pos = game.Map.Move(Pos, move * speed * dt, Radius);

            bool firing = false;
            if (mouse != null)
            {
                Vector2 screen = mouse.position.ReadValue();
                Vector2 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                Vector2 aim = world - Pos;
                if (aim.sqrMagnitude > 0.01f) Aim = aim.normalized;
                crosshair.position = world;
                if (mouse.rightButton.wasPressedThisFrame) ToggleFlare();
                firing = mouse.leftButton.isPressed;
            }

            // Puffs can come faster than the frame rate, so catch up within the frame.
            fireCooldown -= dt;
            if (firing)
                while (fireCooldown <= 0f) Fire();
            else
                fireCooldown = Mathf.Max(fireCooldown, 0f);
            UpdateStream(firing, dt);
        }

        // Lights the area the stream covers and drives the roar of the flame.
        void UpdateStream(bool firing, float dt)
        {
            var mode = EffectiveMode;
            float range = FlameRange(mode) * game.Buffs.RangeMultiplier;
            streamGlow.enabled = firing;
            streamGlow.transform.position = Pos + Aim * range * 0.55f;
            streamGlow.pointLightOuterRadius = range * 0.8f;
            streamGlow.intensity = (mode == FlameMode.Flare ? 1.1f : mode == FlameMode.Dim ? 0.35f : 0.75f) + Random.Range(-0.08f, 0.08f);
            game.Sound.SetFlame(!firing ? 0f : mode == FlameMode.Flare ? 0.6f : mode == FlameMode.Dim ? 0.14f : 0.34f);
        }

        static float FlameRange(FlameMode mode) => mode == FlameMode.Flare ? 7.5f : mode == FlameMode.Dim ? 2.4f : 4.8f;

        void ToggleFlare()
        {
            Mode = Mode == FlameMode.Flare ? FlameMode.Normal : FlameMode.Flare;
            if (Mode == FlameMode.Flare) game.Sound.Flare(); else game.Sound.Dim();
        }

        void ToggleDim()
        {
            Mode = Mode == FlameMode.Dim ? FlameMode.Normal : FlameMode.Dim;
            game.Sound.Dim();
        }

        void Fire()
        {
            var mode = EffectiveMode;
            float rate = game.Buffs.FireRateMultiplier;
            float reach = game.Buffs.RangeMultiplier;
            Vector2 muzzle = Pos + Aim * 0.45f;
            switch (mode)
            {
                case FlameMode.Flare:
                    fireCooldown += 0.0125f / rate;
                    game.SpawnFlame(muzzle, Rotate(Aim, Random.Range(-14f, 14f)) * Random.Range(15f, 19f), 2.5f, 1f, 4.2f, 0.65f * reach);
                    break;
                case FlameMode.Dim:
                    fireCooldown += 0.06f / rate;
                    game.SpawnFlame(muzzle, Rotate(Aim, Random.Range(-6f, 6f)) * Random.Range(7f, 9f), 0.4f, 0.45f, 1.5f, 0.45f * reach);
                    break;
                default:
                    fireCooldown += 0.02f / rate;
                    game.SpawnFlame(muzzle, Rotate(Aim, Random.Range(-9f, 9f)) * Random.Range(11.5f, 14.5f), 1.1f, 0.7f, 3f, 0.55f * reach);
                    break;
            }
        }

        void Melt(float dt)
        {
            SnuffedFor = Mathf.Max(0f, SnuffedFor - dt);
            float rate = EffectiveMode == FlameMode.Flare ? 3f : EffectiveMode == FlameMode.Dim ? 0.1f : 0.5f;
            if (game.IsInSanctuary(Pos)) rate = 0f;
            ChangeWax(-rate * game.Buffs.DamageTakenMultiplier * dt);
            grace -= dt;
            hurtFlash -= dt;
        }

        void Burn(float dt)
        {
            float radius = game.Buffs.AuraRadius;
            if (radius <= 0f) return;
            foreach (var enemy in game.Enemies)
                if (enemy.Targetable && Vector2.Distance(enemy.Pos, Pos) < radius + enemy.Radius)
                    enemy.Damage(game.Buffs.AuraDps * dt, Vector2.zero, false);
        }

        void UpdateVisuals(float dt)
        {
            var mode = EffectiveMode;
            float targetRadius = mode == FlameMode.Flare ? 14f : mode == FlameMode.Dim ? 3.8f : 8f;
            lightRadius = Mathf.Lerp(lightRadius, targetRadius, 1f - Mathf.Exp(-dt * 6f));
            float flicker = Mathf.PerlinNoise(Time.time * 9f, 3.1f) * 0.14f + Mathf.PerlinNoise(Time.time * 23f, 8.3f) * 0.06f;
            glow.pointLightOuterRadius = lightRadius * (0.92f + flicker);
            glow.pointLightInnerRadius = lightRadius * 0.22f;
            glow.intensity = (mode == FlameMode.Flare ? 1.45f : mode == FlameMode.Dim ? 0.8f : 1.15f) + flicker;
            glow.color = mode == FlameMode.Flare ? new Color(1f, 0.58f, 0.25f) : new Color(1f, 0.74f, 0.45f);

            root.transform.localPosition = new Vector3(Pos.x, Pos.y, 0f);

            // The candle physically melts: its height is its health.
            float fraction = Mathf.Clamp01(Wax / MaxWax);
            float height = Mathf.Lerp(0.14f, BodyHeight, fraction);
            float width = 0.42f + (1f - fraction) * 0.1f;
            body.localScale = new Vector3(width, height, 1f);
            body.localPosition = new Vector3(0f, -0.42f + height / 2f, 0f);
            bodySprite.color = hurtFlash > 0f ? Art.Blood : Art.Wax;
            float top = -0.42f + height;
            wick.localPosition = new Vector3(0f, top + 0.06f, 0f);

            float size = mode == FlameMode.Flare ? 1.05f : mode == FlameMode.Dim ? 0.32f : 0.58f;
            size *= 0.9f + flicker * 1.2f;
            flame.localPosition = new Vector3(Aim.x * 0.04f, top + 0.13f + size * 0.34f, 0f);
            flame.localScale = new Vector3(size * 0.65f, size, 1f);
            flameSprite.color = SnuffedFor > 0f ? new Color(0.6f, 0.45f, 0.9f) : Art.Core;
            halo.localPosition = flame.localPosition;
            halo.localScale = Vector3.one * size * 2.8f;
            haloSprite.color = mode == FlameMode.Flare ? new Color(1f, 0.42f, 0.08f, 0.8f) : new Color(1f, 0.58f, 0.15f, 0.5f);

            emberTimer -= dt;
            if (emberTimer <= 0f)
            {
                emberTimer = mode == FlameMode.Flare ? 0.02f : mode == FlameMode.Dim ? 0.3f : 0.08f;
                Vector2 tip = Pos + (Vector2)flame.localPosition;
                var color = Color.Lerp(Art.Flame, Art.Blood, Random.value);
                game.Particles.Emit(tip + Random.insideUnitCircle * 0.08f, new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.8f, 2f)), color, Random.Range(0.05f, 0.14f), Random.Range(0.3f, 0.7f));
            }

            float auraRadius = game.Buffs.AuraRadius;
            aura.localScale = Vector3.one * auraRadius * 2.4f;
            auraSprite.color = new Color(1f, 0.3f, 0.05f, auraRadius > 0f ? 0.2f + flicker : 0f);

            Vector2 objective = game.Objective(Pos);
            arrowSprite.enabled = Vector2.Distance(objective, Pos) > 3f;
            Vector2 dir = (objective - Pos).normalized;
            arrow.position = Pos + dir * 1.2f;
            arrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            arrowSprite.color = game.CurrentGate.IsOpen ? new Color(0.75f, 0.88f, 1f, 0.5f + flicker) : new Color(1f, 0.78f, 0.28f, 0.35f + flicker);
        }

        public void ChangeWax(float amount)
        {
            Wax = Mathf.Min(Wax + amount, MaxWax);
        }

        /// <summary>Contact and projectile damage, with a short grace period between hits.</summary>
        public void Hurt(float amount, Vector2 knock)
        {
            if (grace > 0f || !Alive) return;
            grace = ContactGrace;
            hurtFlash = 0.12f;
            ChangeWax(-amount * game.Buffs.DamageTakenMultiplier);
            Pos = game.Map.Move(Pos, knock, Radius);
            game.Shake(0.28f);
            game.Sound.Hurt();
            game.Particles.Burst(Pos, 10, Art.Wax, 3.5f);
        }

        public void GoOut()
        {
            Alive = false;
            flameSprite.enabled = false;
            haloSprite.enabled = false;
            arrowSprite.enabled = false;
            crosshair.gameObject.SetActive(false);
            glow.enabled = false;
            streamGlow.enabled = false;
        }

        public void Snuff(float seconds)
        {
            if (SnuffedFor <= 0f) game.Sound.Dim();
            SnuffedFor = Mathf.Max(SnuffedFor, seconds);
        }

        public void Revive(Vector2 at, float wax)
        {
            Pos = at;
            Wax = Mathf.Max(wax, 1f);
            Mode = FlameMode.Normal;
            SnuffedFor = 0f;
            grace = 2f;
        }

        static Vector2 Rotate(Vector2 v, float degrees) => Quaternion.Euler(0f, 0f, degrees) * v;
    }
}
