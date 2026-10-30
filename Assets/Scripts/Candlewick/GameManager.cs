using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Candlewick
{
    public enum GameState { Title, Playing, Choosing, FloorCleared, Escaped, Dying, GameOver }

    /// <summary>Owns the run: game state, the dungeon, every live entity, and the per-frame update order.</summary>
    public class GameManager : MonoBehaviour
    {
        public const int FloorCount = 3;
        public const int CandlesToOpen = 3;
        const int CandlesPerFloor = 5, MaxGhosts = 5;
        const float LightCost = 15f, ReviveFraction = 0.45f, FirstGhostDelay = 5f, SecondsPerExtraGhost = 45f;

        public GameState State { get; private set; } = GameState.Title;
        public DungeonMap Map { get; private set; }
        public PlayerCandle Player { get; private set; }
        public BuffSet Buffs { get; private set; } = new BuffSet();
        public Particles Particles { get; private set; }
        public Sfx Sound { get; private set; }
        public Transform WorldRoot { get; private set; }
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Altar> Altars = new List<Altar>();

        public int Depth { get; private set; } = 1;
        public int Kills;
        public int LitThisNight { get; private set; }
        public int TotalSaved { get; private set; }
        public float SurvivalTime { get; private set; }
        public Gate CurrentGate { get; private set; }

        readonly List<FlamePuff> flames = new List<FlamePuff>();
        readonly List<DarkBolt> bolts = new List<DarkBolt>();
        readonly List<WaxDrop> drops = new List<WaxDrop>();
        readonly List<GloomTrail> trails = new List<GloomTrail>();
        Camera cam;
        GameHud hud;
        Vector3 camPos;
        float shake, ambientTimer, dyingTimer, ghostClock;

        void Awake()
        {
            Art.Init();
            Sound = gameObject.AddComponent<Sfx>();
            hud = gameObject.AddComponent<GameHud>();
            hud.Build(this);
            SetUpCamera();
            SetUpDarkness();
            BuildNight();
            Player.Wax = Player.MaxWax;
            hud.ShowTitle();
        }

        void SetUpCamera()
        {
            cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 8.5f;
            cam.backgroundColor = Art.Background;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.75f);
            bloom.intensity.Override(1.1f);
            bloom.scatter.Override(0.72f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.6f);
            vignette.color.Override(Color.black);
            var volume = new GameObject("Atmosphere").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.profile = profile;
        }

        static void SetUpDarkness()
        {
            foreach (var light in FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
            {
                if (light.lightType != Light2D.LightType.Global) continue;
                light.color = new Color(0.35f, 0.38f, 0.72f);
                light.intensity = 0.05f;
            }
        }

        void BuildNight()
        {
            ClearWorld();
            WorldRoot = new GameObject("World").transform;
            Particles = new Particles(WorldRoot);
            Map = new DungeonMap();
            Map.BuildTilemap(WorldRoot);
            LitThisNight = 0;

            var order = new List<Room>(Map.Rooms);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            var start = order[0];
            start.IsStart = true;
            start.Entered = true;

            // The gate takes the room farthest from the start, and the altars the next farthest.
            order.RemoveAt(0);
            order.Sort((a, b) => Vector2.Distance(b.Center, start.Center).CompareTo(Vector2.Distance(a.Center, start.Center)));
            foreach (var room in order) room.WavesLeft = 1 + Mathf.Min(1 + Depth / 2, 3);
            var gateRoom = order[0];
            CurrentGate = new Gate(this, new Vector2(gateRoom.Center.x, gateRoom.Rect.yMax - 1.5f));
            for (int i = 1; i <= CandlesPerFloor; i++)
            {
                Altars.Add(new Altar(this, order[i], order[i].Center + Vector2.up * 0.5f));
                order[i].WavesLeft += 1;
            }

            float previousWax = Player != null ? Player.Wax : 100f;
            float previousMax = Player != null ? Player.MaxWax : 100f;
            Player = new PlayerCandle(this, WorldRoot, start.Center) { MaxWax = previousMax };
            Player.Wax = previousWax;
            Map.UpdateFlow(Player.Pos);
            ambientTimer = 14f;
            ghostClock = 0f;
            camPos = new Vector3(Player.Pos.x, Player.Pos.y, -10f);
            cam.transform.position = camPos;
        }

        void ClearWorld()
        {
            Enemies.Clear();
            Altars.Clear();
            flames.Clear();
            bolts.Clear();
            drops.Clear();
            trails.Clear();
            if (WorldRoot != null) Destroy(WorldRoot.gameObject);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            HandleMenuKeys();

            switch (State)
            {
                case GameState.Playing:
                    TickPlaying(dt);
                    break;
                case GameState.Dying:
                    Particles.Tick(dt);
                    dyingTimer -= dt;
                    if (dyingTimer <= 0f) ShowGameOver();
                    break;
                default:
                    Particles.Tick(dt);
                    break;
            }
            if (State != GameState.Playing) Sound.SetFlame(0f);
            if (State != GameState.Title) UpdateCamera(dt);
            hud.Refresh();
        }

        void HandleMenuKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            bool confirm = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
            if (!confirm) return;
            if (State == GameState.Title || State == GameState.GameOver || State == GameState.Escaped) StartRun();
            else if (State == GameState.FloorCleared) NextFloor();
        }

        void TickPlaying(float dt)
        {
            SurvivalTime += dt;
            Map.UpdateFlow(Player.Pos);
            Player.Tick(dt, cam);
            if (State != GameState.Playing) return;

            UpdateRooms(dt);
            UpdateAmbient(dt);
            UpdateGhosts(dt);

            CurrentGate.Tick(dt);
            foreach (var altar in Altars) altar.Tick(this, dt);
            for (int i = 0; i < Enemies.Count; i++) Enemies[i].Tick(dt);
            SeparateEnemies();
            TickList(flames, f => f.Tick(dt), f => f.Dead);
            TickList(bolts, b => b.Tick(dt), b => b.Dead);
            TickList(drops, d => d.Tick(dt), d => d.Dead);
            TickList(trails, t => t.Tick(dt), t => t.Dead);
            Enemies.RemoveAll(e => e.Dead);

            // Overlapping trail pieces drain as one, so the rate stays predictable.
            foreach (var trail in trails)
            {
                if (!trail.Touches(Player.Pos)) continue;
                Player.ChangeWax(-GloomTrail.DrainPerSecond * Buffs.DamageTakenMultiplier * dt);
                break;
            }
            Particles.Tick(dt);

            if (Player.Wax <= 0f) OnExtinguished();
        }

        static void TickList<T>(List<T> list, System.Action<T> tick, System.Predicate<T> dead)
        {
            for (int i = 0; i < list.Count; i++) tick(list[i]);
            list.RemoveAll(dead);
        }

        void SeparateEnemies()
        {
            for (int i = 0; i < Enemies.Count; i++)
                for (int j = i + 1; j < Enemies.Count; j++)
                {
                    var a = Enemies[i];
                    var b = Enemies[j];
                    if (!a.Solid || !b.Solid) continue;
                    Vector2 delta = a.Pos - b.Pos;
                    float min = a.Radius + b.Radius;
                    float sq = delta.sqrMagnitude;
                    if (sq >= min * min || sq < 0.0001f) continue;
                    Vector2 push = delta.normalized * (min - Mathf.Sqrt(sq)) * 0.5f;
                    a.Push(push);
                    b.Push(-push);
                }
        }

        void UpdateRooms(float dt)
        {
            var here = Map.RoomAt(Player.Pos);
            if (here != null && !here.Entered)
            {
                here.Entered = true;
                here.WaveDelay = 0.4f;
                hud.Toast(Loc.T("Something stirs in the dark", "Щось ворушиться в темряві"));
            }
            foreach (var room in Map.Rooms)
            {
                if (!room.Entered || room.WavesLeft <= 0) continue;
                if (HasEnemies(room)) continue;
                room.WaveDelay -= dt;
                if (room.WaveDelay > 0f) continue;
                SpawnWave(room);
                room.WavesLeft--;
                room.WaveIndex++;
                room.WaveDelay = 1.2f;
            }
        }

        bool HasEnemies(Room room)
        {
            foreach (var e in Enemies) if (e.Home == room && !e.Dead) return true;
            return false;
        }

        void SpawnWave(Room room)
        {
            int budget = 4 + Depth * 2 + room.WaveIndex * 2;
            while (budget > 0)
            {
                Vector2 at = Map.RandomPointIn(room, Player.Pos, 5f);
                float roll = Random.value;
                if (budget >= 4 && roll < 0.2f && (Depth > 1 || room.WaveIndex > 0)) { Enemies.Add(new NightCreeper(this, at, room)); budget -= 4; }
                else if (budget >= 2 && roll < 0.55f) { Enemies.Add(new GloomSnuffer(this, at, room)); budget -= 2; }
                else { Enemies.Add(new ShadowMoth(this, at, room)); budget -= 1; }
            }
        }

        // Moths drift in from outside the light, so resting costs something.
        void UpdateAmbient(float dt)
        {
            ambientTimer -= dt;
            if (ambientTimer > 0f) return;
            ambientTimer = Mathf.Max(7f, 15f - Depth);
            if (Enemies.Count >= 18) return;
            foreach (var room in Map.Rooms)
            {
                if (Vector2.Distance(room.Center, Player.Pos) < 12f || Random.value < 0.5f) continue;
                for (int i = 0; i < 1 + Depth; i++)
                    Enemies.Add(new ShadowMoth(this, Map.RandomPointIn(room, Player.Pos, 8f), null));
                return;
            }
        }

        // The longer the player lingers on a floor, the more ghosts hunt them.
        void UpdateGhosts(float dt)
        {
            ghostClock += dt;
            if (ghostClock < FirstGhostDelay) return;
            int wanted = Mathf.Min(Depth + Mathf.FloorToInt(ghostClock / SecondsPerExtraGhost), MaxGhosts);
            int alive = 0;
            foreach (var e in Enemies) if (e is Ghost) alive++;
            if (alive >= wanted) return;
            Enemies.Add(new Ghost(this, GhostSpawnPoint()));
            hud.Toast(alive == 0
                ? Loc.T("A ghost has found you. Run", "Привид знайшов тебе. Тікай")
                : Loc.T("Another ghost joins the hunt", "До полювання долучився ще один привид"));
        }

        public Vector2 GhostSpawnPoint()
        {
            Vector2 p = Player.Pos + Random.insideUnitCircle.normalized * Random.Range(16f, 22f);
            return new Vector2(Mathf.Clamp(p.x, 1f, Map.Width - 1f), Mathf.Clamp(p.y, 1f, Map.Height - 1f));
        }

        void UpdateCamera(float dt)
        {
            if (Player == null) return;
            Vector3 target = new Vector3(Player.Pos.x + Player.Aim.x * 1.2f, Player.Pos.y + Player.Aim.y * 1.2f, -10f);
            camPos = Vector3.Lerp(camPos, target, 1f - Mathf.Exp(-dt * 7f));
            shake = Mathf.Max(0f, shake - dt * 1.4f);
            Vector2 offset = Random.insideUnitCircle * shake;
            cam.transform.position = camPos + new Vector3(offset.x, offset.y, 0f);
        }

        // --- Interactions ---------------------------------------------------------------------

        public Altar NearestUnlitAltar(Vector2 from)
        {
            Altar best = null;
            float bestDistance = float.MaxValue;
            foreach (var altar in Altars)
            {
                if (altar.IsLit) continue;
                float d = Vector2.Distance(altar.Pos, from);
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = altar;
            }
            return best;
        }

        public bool IsInSanctuary(Vector2 p)
        {
            foreach (var altar in Altars)
                if (altar.IsLit && Vector2.Distance(altar.Pos, p) < Altar.SanctuaryRange) return true;
            return false;
        }

        /// <summary>What the prompt should say, or null when no unlit candle is in reach.</summary>
        public string InteractPrompt()
        {
            if (State != GameState.Playing) return null;
            if (Vector2.Distance(CurrentGate.Pos, Player.Pos) <= Gate.InteractRange)
            {
                int missing = CandlesToOpen - LitThisNight;
                if (!CurrentGate.IsOpen) return Loc.T($"Sealed. Candles still needed to open it: {missing}", $"Запечатано. Ще потрібно запалити свічок: {missing}");
                return Depth >= FloorCount
                    ? Loc.T("[E] Escape the castle", "[E] Втекти із замку")
                    : Loc.T("[E] Descend to the next floor", "[E] Спуститися на наступний поверх");
            }
            var altar = NearestUnlitAltar(Player.Pos);
            if (altar == null || Vector2.Distance(altar.Pos, Player.Pos) > Altar.InteractRange) return null;
            if (!altar.Room.Cleared) return Loc.T("The shadows smother this wick. Clear the room first", "Тіні душать цей гніт. Спершу зачисть кімнату");
            if (Player.Wax <= LightCost) return Loc.T("Not enough wax to share", "Замало воску, щоб поділитися");
            return Loc.T("[E] Share your flame (-15 wax)", "[E] Поділитися полум'ям (-15 воску)");
        }

        /// <summary>Where the player's guide arrow points: the nearest candle, then the open gate.</summary>
        public Vector2 Objective(Vector2 from)
        {
            if (CurrentGate.IsOpen) return CurrentGate.Pos;
            var altar = NearestUnlitAltar(from);
            return altar != null ? altar.Pos : CurrentGate.Pos;
        }

        public void Interact()
        {
            if (Vector2.Distance(CurrentGate.Pos, Player.Pos) <= Gate.InteractRange)
            {
                if (CurrentGate.IsOpen) ReachGate();
                else Sound.Deny();
                return;
            }
            var altar = NearestUnlitAltar(Player.Pos);
            if (altar == null || Vector2.Distance(altar.Pos, Player.Pos) > Altar.InteractRange) return;
            if (!altar.Room.Cleared || Player.Wax <= LightCost)
            {
                Sound.Deny();
                return;
            }
            Player.ChangeWax(-LightCost);
            altar.Light();
            LitThisNight++;
            TotalSaved++;
            Sound.Light();
            Particles.Burst(altar.Pos + Vector2.up, 28, Art.Flame, 5f);
            if (LitThisNight == CandlesToOpen)
            {
                CurrentGate.Open();
                ghostClock += SecondsPerExtraGhost; // Opening the gate wakes another ghost.
                hud.Toast(Loc.T("The gate is open. Run!", "Ворота відчинено. Тікай!"));
            }
            State = GameState.Choosing;
            hud.ShowBuffChoice(BuffSet.RollChoices(3));
        }

        public void ChooseBuff(BuffType type)
        {
            if (State != GameState.Choosing) return;
            Buffs.Add(type);
            if (type == BuffType.MaxWax)
            {
                Player.MaxWax += 25f;
                Player.ChangeWax(25f);
            }
            hud.HidePanels();
            State = GameState.Playing;
        }

        void ReachGate()
        {
            Sound.Light();
            if (Depth >= FloorCount)
            {
                State = GameState.Escaped;
                hud.ShowEscaped();
                return;
            }
            State = GameState.FloorCleared;
            hud.ShowFloorCleared();
        }

        void OnExtinguished()
        {
            Altar checkpoint = null;
            float best = float.MaxValue;
            foreach (var altar in Altars)
            {
                if (!altar.HasRekindle) continue;
                float d = Vector2.Distance(altar.Pos, Player.Pos);
                if (d >= best) continue;
                best = d;
                checkpoint = altar;
            }

            Sound.Extinguish();
            Particles.Burst(Player.Pos, 30, new Color(0.4f, 0.38f, 0.45f), 3f);

            if (checkpoint != null)
            {
                checkpoint.HasRekindle = false;
                Player.Revive(checkpoint.Pos + Vector2.down * 1.2f, Player.MaxWax * ReviveFraction);
                foreach (var enemy in Enemies)
                    if (Vector2.Distance(enemy.Pos, checkpoint.Pos) < 7f) enemy.Die(false);
                Enemies.RemoveAll(e => e.Dead);
                foreach (var bolt in bolts) bolt.Kill();
                bolts.Clear();
                foreach (var trail in trails)
                    if (trail.IsNear(checkpoint.Pos, 7f)) trail.Kill();
                trails.RemoveAll(t => t.Dead);
                Shake(0.5f);
                hud.Toast(Loc.T("A rescued candle rekindles you", "Врятована свічка знову запалила тебе"));
                return;
            }

            Player.Wax = 0f;
            Player.GoOut();
            State = GameState.Dying;
            dyingTimer = 0.9f;
            Cursor.visible = true;
        }

        void ShowGameOver()
        {
            State = GameState.GameOver;
            hud.ShowGameOver();
        }

        public int RekindlesLeft()
        {
            int n = 0;
            foreach (var altar in Altars) if (altar.HasRekindle) n++;
            return n;
        }

        // --- Flow ------------------------------------------------------------------------------

        public void StartRun()
        {
            Buffs = new BuffSet();
            Depth = 1;
            Kills = 0;
            TotalSaved = 0;
            SurvivalTime = 0f;
            Player = null;
            BuildNight();
            hud.HidePanels();
            State = GameState.Playing;
        }

        public void NextFloor()
        {
            if (State != GameState.FloorCleared) return;
            Depth++;
            BuildNight();
            Player.ChangeWax(Player.MaxWax * 0.35f);
            hud.HidePanels();
            State = GameState.Playing;
        }

        // --- Spawners used by entities ---------------------------------------------------------

        public void SpawnFlame(Vector2 pos, Vector2 vel, float damage, float startSize, float endSize, float life) =>
            flames.Add(new FlamePuff(this, pos, vel, damage, startSize, endSize, life));
        public void SpawnBolt(Vector2 pos, Vector2 vel) => bolts.Add(new DarkBolt(this, pos, vel));
        public void SpawnDrop(Vector2 pos, int amount) => drops.Add(new WaxDrop(this, pos, amount));
        public void SpawnTrail(Vector2 pos) => trails.Add(new GloomTrail(this, pos));
        public void Shake(float amount) => shake = Mathf.Max(shake, amount);
    }
}
