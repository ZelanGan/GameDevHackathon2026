using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Candlewick
{
    public enum FlameMode { Dim, Normal, Flare }

    /// <summary>The last ray of the sun, a glowing child whose cape holds its light (wax), which is also its health: and the flame mode trades wax for light, speed and firepower.</summary>
    public class PlayerCandle
    {
        public const float Gravity = 28f, JumpSpeed = 14f, Radius = 0.34f, NormalSpeed = 5f;
        const float LightPerFollower = 0.12f;
        const float ContactGrace = 0.6f, SanctuaryHealPerSecond = 3f;
        const float CoyoteTime = 0.1f, JumpBuffer = 0.12f, MaxFallSpeed = 22f, DropThroughTime = 0.25f, PitWaxCost = 15f;

        // The child's cape stores its light, as in Sky: it shines at full and goes dark as the light runs out.
        const float FigureScale = 1.3f, ChildCenterY = 0.4f;
        static readonly Color CapeLit = new Color(1f, 0.82f, 0.4f), CapeSpent = new Color(0.1f, 0.1f, 0.16f);

        public Vector2 Pos;
        public float Wax, MaxWax = 100f;
        public FlameMode Mode = FlameMode.Normal;
        public float SnuffedFor;
        public Vector2 Aim = Vector2.right;
        public bool Alive = true;

        public FlameMode EffectiveMode => SnuffedFor > 0f ? FlameMode.Dim : Mode;

        // Wax trades power for speed: a full candle hits hard, a stub runs fast.
        const float MinDamage = 0.5f, MaxDamage = 1.5f, MaxSpeedBonus = 1.4f;
        float WaxFraction => Mathf.Clamp01(Wax / MaxWax);
        public float DamageMultiplier => Mathf.Lerp(MinDamage, MaxDamage, WaxFraction);
        public float SpeedMultiplier => Mathf.Lerp(MaxSpeedBonus, 1f, WaxFraction);

        readonly GameManager game;
        readonly GameObject root;
        readonly Transform figure, cape, flame, halo, aura, arrow, soulArrow, crosshair;
        readonly SpriteRenderer bodySprite, capeSprite, flameSprite, haloSprite, auraSprite, arrowSprite, soulArrowSprite;
        readonly Light2D glow, streamGlow;
        float fireCooldown, grace, hurtFlash, emberTimer, healMoteTimer, lightRadius = 8f;
        float verticalSpeed, sinceGrounded, jumpQueued, dropping;
        bool grounded, jumpHeld;
        Vector2 lastSafe, lastPos;
        float facing = 1f, stride;

        public PlayerCandle(GameManager game, Transform parent, Vector2 pos)
        {
            this.game = game;
            Pos = lastSafe = pos;
            Wax = MaxWax;
            root = new GameObject("Player");
            root.transform.SetParent(parent, false);
            // The child and cape share a parent so both can flip and bob together.
            figure = new GameObject("Figure").transform;
            figure.SetParent(root.transform, false);
            cape = Art.Spr("Cape", figure, Art.Cape, Art.Glow, CapeLit, 11).transform;
            capeSprite = cape.GetComponent<SpriteRenderer>();
            var child = Art.Spr("Child", figure, Art.Child, Art.Glow, Color.white, 12);
            bodySprite = child.GetComponent<SpriteRenderer>();
            child.transform.localPosition = new Vector3(0f, ChildCenterY, 0f);
            cape.localPosition = new Vector3(0f, ChildCenterY, 0f);
            halo = Art.Spr("Halo", root.transform, Art.Soft, Art.Glow, new Color(1f, 0.55f, 0.15f, 0.55f), 10).transform;
            haloSprite = halo.GetComponent<SpriteRenderer>();
            flame = Art.Spr("Heart", figure, Art.Soft, Art.Glow, Art.Core, 13).transform;
            flameSprite = flame.GetComponent<SpriteRenderer>();
            aura = Art.Spr("Aura", root.transform, Art.Soft, Art.Glow, new Color(1f, 0.3f, 0.05f, 0f), 2).transform;
            auraSprite = aura.GetComponent<SpriteRenderer>();
            arrow = Art.Spr("AltarArrow", parent, Art.Arrow, Art.Glow, Art.Flame, 16, 0.3f).transform;
            arrowSprite = arrow.GetComponent<SpriteRenderer>();
            soulArrow = Art.Spr("SoulArrow", parent, Art.Arrow, Art.Glow, Color.white, 16, 0.26f).transform;
            soulArrowSprite = soulArrow.GetComponent<SpriteRenderer>();
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
            float move = 0f;
            bool jumpDown = false;
            jumpHeld = false;
            if (kb != null)
            {
                if (kb.dKey.isPressed) move += 1f;
                if (kb.aKey.isPressed) move -= 1f;
                jumpDown = kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                jumpHeld = kb.wKey.isPressed || kb.spaceKey.isPressed;
                if (kb.sKey.wasPressedThisFrame) dropping = DropThroughTime;
                if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame) ToggleFlare();
                if (kb.qKey.wasPressedThisFrame) ToggleDim();
                if (kb.eKey.wasPressedThisFrame) game.Interact();
            }
            float speed = EffectiveMode == FlameMode.Flare ? 7.5f : EffectiveMode == FlameMode.Dim ? 3.4f : NormalSpeed;
            MoveBody(move * speed * SpeedMultiplier, jumpDown, dt);

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

        void MoveBody(float runSpeed, bool jumpDown, float dt)
        {
            sinceGrounded = grounded ? 0f : sinceGrounded + dt;
            jumpQueued = jumpDown ? JumpBuffer : jumpQueued - dt;
            dropping -= dt;

            // Coyote time and a jump buffer forgive presses that come slightly early or late.
            if (jumpQueued > 0f && sinceGrounded < CoyoteTime)
            {
                verticalSpeed = JumpSpeed;
                jumpQueued = 0f;
                sinceGrounded = CoyoteTime;
                game.Sound.Jump();
                game.Particles.Burst(Pos + Vector2.down * Radius, 5, Art.WaxDark, 1.5f);
            }
            // Letting go early cuts the jump short.
            if (!jumpHeld && verticalSpeed > 5f) verticalSpeed = 5f;

            verticalSpeed = Mathf.Max(verticalSpeed - Gravity * dt, -MaxFallSpeed);
            Pos = game.Map.MoveWalker(Pos, new Vector2(runSpeed * dt, verticalSpeed * dt), Radius, dropping > 0f, out grounded, out bool bumped);
            if (grounded || bumped) verticalSpeed = 0f;
            if (Pos.y < DungeonMap.KillY) FallInPit();
            else if (grounded) lastSafe = Pos;
        }

        void FallInPit()
        {
            ChangeWax(-PitWaxCost * game.Buffs.DamageTakenMultiplier);
            Pos = lastSafe;
            verticalSpeed = 0f;
            grace = ContactGrace * 2f;
            game.Shake(0.3f);
            game.Sound.Hurt();
            game.Particles.Burst(Pos, 12, Art.Gloom, 3f);
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
            float power = DamageMultiplier;
            float swell = 0.75f + 0.25f * power; // A fuller candle also breathes a visibly thicker flame.
            Vector2 muzzle = Pos + Aim * 0.45f;
            switch (mode)
            {
                case FlameMode.Flare:
                    fireCooldown += 0.0125f / rate;
                    game.SpawnFlame(muzzle, Rotate(Aim, Random.Range(-14f, 14f)) * Random.Range(15f, 19f), 2.5f * power, 1f * swell, 4.2f * swell, 0.65f * reach);
                    break;
                case FlameMode.Dim:
                    fireCooldown += 0.06f / rate;
                    game.SpawnFlame(muzzle, Rotate(Aim, Random.Range(-6f, 6f)) * Random.Range(7f, 9f), 0.4f * power, 0.45f * swell, 1.5f * swell, 0.45f * reach);
                    break;
                default:
                    fireCooldown += 0.02f / rate;
                    game.SpawnFlame(muzzle, Rotate(Aim, Random.Range(-9f, 9f)) * Random.Range(11.5f, 14.5f), 1.1f * power, 0.7f * swell, 3f * swell, 0.55f * reach);
                    break;
            }
        }

        void Melt(float dt)
        {
            SnuffedFor = Mathf.Max(0f, SnuffedFor - dt);
            float rate = EffectiveMode == FlameMode.Flare ? 3f : EffectiveMode == FlameMode.Dim ? 0.1f : 0.5f;
            if (game.IsInSanctuary(Pos)) Heal(dt);
            else ChangeWax(-rate * game.Buffs.DamageTakenMultiplier * dt);
            grace -= dt;
            hurtFlash -= dt;
        }

        // Lit candles pour wax back into the player, with motes rising while it still does anything.
        void Heal(float dt)
        {
            if (Wax >= MaxWax) return;
            ChangeWax(SanctuaryHealPerSecond * dt);
            healMoteTimer -= dt;
            if (healMoteTimer > 0f) return;
            healMoteTimer = 0.12f;
            game.Particles.Emit(Pos + new Vector2(Random.Range(-0.45f, 0.45f), -0.4f), new Vector2(0f, Random.Range(1f, 1.8f)), Art.Wax, Random.Range(0.1f, 0.18f), 0.8f);
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
            targetRadius *= 1f + LightPerFollower * game.FollowerCount;
            lightRadius = Mathf.Lerp(lightRadius, targetRadius, 1f - Mathf.Exp(-dt * 6f));
            float flicker = Mathf.PerlinNoise(Time.time * 9f, 3.1f) * 0.14f + Mathf.PerlinNoise(Time.time * 23f, 8.3f) * 0.06f;
            glow.pointLightOuterRadius = lightRadius * (0.92f + flicker);
            glow.pointLightInnerRadius = lightRadius * 0.22f;
            glow.intensity = (mode == FlameMode.Flare ? 1.45f : mode == FlameMode.Dim ? 0.8f : 1.15f) + flicker;
            glow.color = mode == FlameMode.Flare ? new Color(1f, 0.58f, 0.25f) : new Color(1f, 0.74f, 0.45f);

            root.transform.localPosition = new Vector3(Pos.x, Pos.y, 0f);

            float fraction = Mathf.Clamp01(Wax / MaxWax);
            float speedNow = dt > 0f ? (Pos.x - lastPos.x) / dt : 0f;
            lastPos = Pos;
            if (Mathf.Abs(speedNow) > 0.5f) facing = Mathf.Sign(speedNow);
            else if (Mathf.Abs(Aim.x) > 0.2f) facing = Mathf.Sign(Aim.x);
            stride += Mathf.Abs(speedNow) * dt * 2.2f;

            figure.localScale = new Vector3(facing * FigureScale, FigureScale, 1f);
            figure.localPosition = new Vector3(0f, grounded ? Mathf.Abs(Mathf.Sin(stride)) * 0.05f : 0f, 0f);

            bodySprite.color = hurtFlash > 0f ? Art.Blood : Color.Lerp(new Color(0.55f, 0.52f, 0.55f), Color.white, 0.4f + fraction * 0.6f);
            capeSprite.color = Color.Lerp(CapeSpent, CapeLit, fraction * fraction);
            // The cape trails behind when running and billows up when falling.
            float sway = Mathf.Clamp(Mathf.Abs(speedNow) * 2.4f, 0f, 14f) + Mathf.Clamp(-verticalSpeed * 0.8f, -6f, 10f) + Mathf.Sin(Time.time * 3f) * 1.5f;
            cape.localRotation = Quaternion.Euler(0f, 0f, -sway);

            float size = mode == FlameMode.Flare ? 0.8f : mode == FlameMode.Dim ? 0.22f : 0.42f;
            size *= (0.9f + flicker * 1.2f) * (0.4f + 0.6f * fraction);
            flame.localPosition = new Vector3(0.04f, ChildCenterY + 0.1f, 0f);
            flame.localScale = Vector3.one * size;
            flameSprite.color = SnuffedFor > 0f ? new Color(0.6f, 0.45f, 0.9f) : Art.Core;
            halo.localPosition = new Vector3(0f, ChildCenterY * FigureScale, 0f);
            halo.localScale = Vector3.one * (size * 2.2f + 0.9f + fraction * 1.4f);
            float haloAlpha = (mode == FlameMode.Flare ? 0.7f : 0.42f) * (0.25f + 0.75f * fraction);
            haloSprite.color = new Color(1f, mode == FlameMode.Flare ? 0.5f : 0.68f, 0.28f, haloAlpha);

            emberTimer -= dt;
            if (emberTimer <= 0f)
            {
                emberTimer = (mode == FlameMode.Flare ? 0.03f : mode == FlameMode.Dim ? 0.4f : 0.12f) / Mathf.Max(0.15f, fraction);
                // Light sheds from the cape's hem, more thinly as it runs out.
                Vector2 tip = Pos + new Vector2(-facing * Random.Range(0.1f, 0.7f), Random.Range(-0.3f, 0.3f));
                var color = Color.Lerp(Art.Flame, Art.Core, Random.value);
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
            arrowSprite.color = new Color(1f, 0.78f, 0.28f, (game.FollowerCount > 0 ? 0.6f : 0.35f) + flicker);

            // A second, paler arrow points at the nearest soul still waiting to be found.
            var soul = game.NearestLost(Pos);
            soulArrowSprite.enabled = soul != null && Vector2.Distance(soul.Pos, Pos) > 3f;
            if (soul != null)
            {
                Vector2 toSoul = (soul.Pos - Pos).normalized;
                soulArrow.position = Pos + toSoul * 1.6f;
                soulArrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(toSoul.y, toSoul.x) * Mathf.Rad2Deg);
                soulArrowSprite.color = new Color(0.65f, 0.88f, 1f, 0.35f + flicker);
            }
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

        /// <summary>For the intro: 0 hides the child and its guides, 1 is fully there.</summary>
        public void SetPresence(float amount)
        {
            bool present = amount > 0f;
            root.transform.localPosition = new Vector3(Pos.x, Pos.y, 0f);
            root.transform.localScale = new Vector3(amount, amount, 1f);
            root.SetActive(present);
            bool guides = amount >= 1f;
            arrow.gameObject.SetActive(guides);
            soulArrow.gameObject.SetActive(guides);
            crosshair.gameObject.SetActive(guides);
        }

        public void GoOut()
        {
            Alive = false;
            flameSprite.enabled = false;
            haloSprite.enabled = false;
            capeSprite.color = CapeSpent;
            bodySprite.color = new Color(0.4f, 0.38f, 0.42f);
            arrowSprite.enabled = false;
            soulArrowSprite.enabled = false;
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
            Pos = lastSafe = at;
            verticalSpeed = 0f;
            Wax = Mathf.Max(wax, 1f);
            Mode = FlameMode.Normal;
            SnuffedFor = 0f;
            grace = 2f;
        }

        static Vector2 Rotate(Vector2 v, float degrees) => Quaternion.Euler(0f, 0f, degrees) * v;
    }
}
