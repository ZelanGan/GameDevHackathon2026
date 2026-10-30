using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Candlewick
{
    public enum GameState { Title, Intro, Playing, Paused, Choosing, Sunrise, Dying, GameOver }

    /// <summary>Owns the run: game state, the dungeon, every live entity, and the per-frame update order.</summary>
    public class GameManager : MonoBehaviour
    {
        // The working week, Monday to Friday. Each day is bigger, shorter on time per soul and more dangerous.
        public const int DaysInWeek = 5;
        static readonly int[] SoulsByDay = { 6, 8, 10, 12, 14 };
        static readonly int[] CandlesByDay = { 7, 7, 6, 6, 5 };
        static readonly float[] SecondsByDay = { 180f, 210f, 240f, 270f, 300f };
        const int MaxGhosts = 5;
        const float MenuCameraSize = 13f, PlayCameraSize = 8.5f, DeathCameraSize = 5.5f;
        const float ShadowClearRange = 6f; // A candle can only be lit with no enemy this close to it.
        const float DawnRushSeconds = 6f; // How long the sun takes to rise once no souls are left.
        const float DawnStarts = 0.75f; // The fraction of the night after which the sky begins to lighten.
        const float LightCost = 15f, GatherCost = 5f, LightPerSoul = 10f, ReviveFraction = 0.45f, FirstGhostDelay = 5f, SecondsPerExtraGhost = 45f;
        static readonly Color DawnSky = new Color(1f, 0.62f, 0.4f);

        public GameState State { get; private set; } = GameState.Title;
        public DungeonMap Map { get; private set; }
        public PlayerCandle Player { get; private set; }
        public BuffSet Buffs { get; private set; } = new BuffSet();
        public Particles Particles { get; private set; }
        public Sfx Sound { get; private set; }
        public Transform WorldRoot { get; private set; }
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Altar> Altars = new List<Altar>();
        public readonly List<Soul> Souls = new List<Soul>();
        public int Rescued { get; private set; }
        public int RescuedThisWeek { get; private set; }
        public int Day { get; private set; } = 1;
        public int SoulsToday => SoulsByDay[Day - 1];
        public int CandlesToday => CandlesByDay[Day - 1];
        public static int SoulsInWeek
        {
            get
            {
                int total = 0;
                foreach (int souls in SoulsByDay) total += souls;
                return total;
            }
        }
        public int LostSouls { get; private set; }
        public float NightLeft { get; private set; }

        public int Depth => Day;
        public int Kills;
        public int LitThisNight { get; private set; }
        public int TotalSaved { get; private set; }
        public float SurvivalTime { get; private set; }

        readonly List<RoomDoor> doors = new List<RoomDoor>();
        readonly List<FlamePuff> flames = new List<FlamePuff>();
        readonly List<DarkBolt> bolts = new List<DarkBolt>();
        readonly List<WaxDrop> drops = new List<WaxDrop>();
        readonly List<GloomTrail> trails = new List<GloomTrail>();
        Camera cam;
        Light2D globalLight;
        float dawnRushRate;
        SunrayIntro intro;
        GameState stateBeforePause;
        Light2D menuLight;
        float menuTime;
        readonly List<Palette> weekPalettes = new List<Palette>();
        Palette Today => weekPalettes[Day - 1];
        GameHud hud;
        Vector3 camPos;
        float shake, ambientTimer, ghostClock;

        void Awake()
        {
            Art.Init();
            Sound = gameObject.AddComponent<Sfx>();
            hud = gameObject.AddComponent<GameHud>();
            hud.Build(this);
            SetUpCamera();
            SetUpDarkness();
            EnterMenu();
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

        void SetUpDarkness()
        {
            foreach (var light in FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
                if (light.lightType == Light2D.LightType.Global) globalLight = light;
        }

        // Night holds until late, then the sky warms and the world slowly becomes visible.
        void UpdateDawn()
        {
            float night = 1f - NightLeft / SecondsByDay[Day - 1];
            float dawn = Mathf.Clamp01((night - DawnStarts) / (1f - DawnStarts));
            Map.SetSkyTint(Color.Lerp(Today.NightSky, DawnSky, dawn));
            if (globalLight == null) return;
            globalLight.color = Color.Lerp(Today.Ambient, new Color(1f, 0.72f, 0.5f), dawn);
            globalLight.intensity = Mathf.Lerp(0.05f, 0.45f, dawn);
        }

        // Every day of a week gets its own colours, in a different order each week.
        void ShufflePalettes()
        {
            weekPalettes.Clear();
            weekPalettes.AddRange(Palette.All);
            for (int i = weekPalettes.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (weekPalettes[i], weekPalettes[j]) = (weekPalettes[j], weekPalettes[i]);
            }
        }

        void BuildNight()
        {
            ClearWorld();
            WorldRoot = new GameObject("World").transform;
            Particles = new Particles(WorldRoot);
            Map = new DungeonMap(Day, Today);
            Map.BuildTilemap(WorldRoot);
            LitThisNight = 0;
            foreach (var spot in Map.DoorSpots) doors.Add(new RoomDoor(this, spot));

            // The ray lands in a dorm room; candles and souls are scattered through the other rooms.
            var start = Map.StartRoom;
            start.IsStart = true;
            start.Entered = true;
            var free = new List<Room>();
            foreach (var room in Map.Rooms)
            {
                if (room == start || room.Kind == RoomKind.Stairs || room.Kind == RoomKind.Roof) continue;
                // There are many rooms, so only some of the empty ones hold enemies.
                if (Random.value < 0.5f) room.WavesLeft = 1;
                free.Add(room);
            }

            for (int i = 0; i < CandlesToday; i++)
            {
                var room = TakeRandom(free);
                Altars.Add(new Altar(this, room, room.Stand + Vector2.up * 0.3f));
                room.WavesLeft = 1 + Mathf.Min(1 + Day / 2, 3);
            }
            for (int i = 0; i < SoulsToday; i++)
            {
                var room = TakeRandom(free);
                Souls.Add(new Soul(this, room.Stand + Vector2.up * 0.4f));
                room.WavesLeft = Mathf.Max(room.WavesLeft, 1);
            }

            Rescued = 0;
            LostSouls = 0;
            NightLeft = SecondsByDay[Day - 1];
            dawnRushRate = 0f;
            UpdateDawn();

            float previousWax = Player != null ? Player.Wax : 100f;
            float previousMax = Player != null ? Player.MaxWax : 100f;
            Player = new PlayerCandle(this, WorldRoot, start.Stand) { MaxWax = previousMax };
            Player.Wax = previousWax;
            Map.UpdateFlow(Player.Pos);
            ambientTimer = 14f;
            ghostClock = 0f;
            camPos = new Vector3(Player.Pos.x, Player.Pos.y, -10f);
            cam.transform.position = camPos;
        }

        static Room TakeRandom(List<Room> rooms)
        {
            int pick = Random.Range(0, rooms.Count);
            var room = rooms[pick];
            rooms.RemoveAt(pick);
            return room;
        }

        void ClearWorld()
        {
            Enemies.Clear();
            Altars.Clear();
            Souls.Clear();
            doors.Clear();
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
                case GameState.Title:
                    TickMenu(dt);
                    break;
                case GameState.Paused:
                    break;
                case GameState.Intro:
                    TickIntro(dt);
                    break;
                case GameState.Playing:
                    TickPlaying(dt);
                    break;
                case GameState.Dying:
                    Particles.Tick(dt);
                    // The camera closes in on the child as it goes out.
                    cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, DeathCameraSize, 1f - Mathf.Exp(-dt * 1.4f));
                    if (Player.TickDeath(dt)) ShowGameOver();
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
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (hud.CloseSubMenu()) return;
                if (State == GameState.Playing || State == GameState.Intro) Pause();
                else if (State == GameState.Paused) Resume();
                return;
            }
            if (hud.InSubMenu) return;
            bool confirm = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
            if (!confirm) return;
            if (State == GameState.Title || State == GameState.GameOver) StartRun();
            else if (State == GameState.Sunrise) AfterSunrise();
        }

        // The menu shows a real dormitory at dusk, with the camera drifting slowly along it.
        void EnterMenu()
        {
            Day = 1;
            Buffs = new BuffSet();
            Player = null;
            intro = null;
            ShufflePalettes();
            BuildNight();
            Player.SetPresence(0f);
            cam.orthographicSize = MenuCameraSize;
            Map.SetSkyTint(Color.Lerp(Today.NightSky, DawnSky, 0.45f));
            if (globalLight != null)
            {
                globalLight.color = Color.Lerp(Today.Ambient, new Color(1f, 0.72f, 0.5f), 0.4f);
                globalLight.intensity = 0.22f;
            }
            var lightObject = new GameObject("MenuLight");
            lightObject.transform.SetParent(WorldRoot, false);
            menuLight = Art.AddLight(lightObject, new Color(1f, 0.7f, 0.42f), 26f, 0.75f);
            menuTime = Random.value * 100f;
            State = GameState.Title;
            hud.ShowTitle();
        }

        void TickMenu(float dt)
        {
            menuTime += dt;
            float margin = cam.orthographicSize * cam.aspect;
            float span = Mathf.Max(0f, Map.Width - margin * 2f);
            float x = margin + span * Mathf.PingPong(menuTime * 0.012f, 1f);
            float y = Map.Height * 0.45f;
            cam.transform.position = new Vector3(x, y, -10f);
            // The last light lingers on the right, clear of the menu.
            menuLight.transform.position = new Vector3(x + margin * 0.3f, y, 0f);
            menuLight.intensity = 0.72f + Mathf.PerlinNoise(menuTime * 0.8f, 2f) * 0.12f;
            int order = 0;
            foreach (var soul in Souls) soul.Tick(dt, order++);
            Particles.Tick(dt);
        }

        public void ReturnToMenu()
        {
            hud.HidePanels();
            EnterMenu();
        }

        public void Pause()
        {
            stateBeforePause = State;
            State = GameState.Paused;
            hud.HidePanels();
            hud.ShowPause();
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            State = stateBeforePause;
            hud.HidePanels();
            if (State == GameState.Intro) hud.ShowDayCard();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void TickIntro(float dt)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool skip = (kb != null && kb.anyKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (skip && intro.CanSkip) intro.Skip();
            Particles.Tick(dt);
            if (!intro.Tick(dt)) return;
            intro = null;
            hud.HidePanels();
            Player.SetPresence(1f);
            State = GameState.Playing;
            hud.Toast(Loc.T("Find the lost souls before sunrise", "Знайди заблукані душі до світанку"));
        }

        void TickPlaying(float dt)
        {
            SurvivalTime += dt;
            NightLeft = Mathf.Max(0f, NightLeft - dt * (dawnRushRate > 0f ? dawnRushRate : 1f));
            UpdateDawn();
            Map.UpdateFlow(Player.Pos);
            Player.Tick(dt, cam);
            if (State != GameState.Playing) return;

            if (dawnRushRate <= 0f)
            {
                UpdateRooms(dt);
                UpdateAmbient(dt);
                UpdateGhosts(dt);
            }

            foreach (var door in doors) door.Tick(dt);
            foreach (var altar in Altars) altar.Tick(this, dt);
            int followOrder = 0;
            foreach (var soul in Souls)
            {
                soul.Tick(dt, soul.State == Soul.Status.Following ? followOrder++ : 0);
                if (soul.State == Soul.Status.Following && IsInSanctuary(soul.Pos)) SaveSoul(soul);
            }
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

            if (dawnRushRate > 0f) Player.Wax = Mathf.Max(Player.Wax, 1f); // Nothing can end the run during the last moments of the night.
            if (Player.Wax <= 0f) OnExtinguished();
            else if (NightLeft <= 0f) EndAtSunrise();
            else if (dawnRushRate <= 0f && !AnySoulsLeft()) RushToDawn();
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

        // The longer the night goes on, the more ghosts hunt them.
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

        // Only enemies that can actually reach the candle count, not ones behind a wall or a floor.
        bool ShadowsNear(Vector2 at)
        {
            foreach (var enemy in Enemies)
                if (!enemy.Dead && enemy.Targetable && Vector2.Distance(enemy.Pos, at) < ShadowClearRange && Map.LineOfSight(enemy.Pos, at))
                    return true;
            return false;
        }

        /// <summary>What the prompt should say, or null when no unlit candle is in reach.</summary>
        public string InteractPrompt()
        {
            if (State != GameState.Playing) return null;
            var lost = NearestLost(Player.Pos);
            if (lost != null && Vector2.Distance(lost.Pos, Player.Pos) <= Soul.GatherRange)
            {
                if (Player.Wax <= GatherCost) return Loc.T("Not enough light to share", "Замало світла, щоб поділитися");
                return Loc.T("[E] Reach out to this soul (-5 light)", "[E] Простягнути руку цій душі (-5 світла)");
            }
            var altar = NearestUnlitAltar(Player.Pos);
            if (altar == null || Vector2.Distance(altar.Pos, Player.Pos) > Altar.InteractRange) return null;
            if (ShadowsNear(altar.Pos)) return Loc.T("Shadows are smothering this wick. Drive them away", "Тіні душать цей гніт. Прожени їх");
            if (Player.Wax <= LightCost) return Loc.T("Not enough light to share", "Замало світла, щоб поділитися");
            return Loc.T("[E] Light this candle (-15 light)", "[E] Запалити цю свічку (-15 світла)");
        }

        /// <summary>Where the guide arrow points: a lit candle when souls need one, otherwise a candle to light.</summary>
        public Vector2 Objective(Vector2 from)
        {
            if (FollowerCount > 0)
            {
                Altar nearest = null;
                float best = float.MaxValue;
                foreach (var candle in Altars)
                {
                    float d = Vector2.Distance(candle.Pos, from);
                    if (!candle.IsLit || d >= best) continue;
                    best = d;
                    nearest = candle;
                }
                if (nearest != null) return nearest.Pos;
            }
            var unlit = NearestUnlitAltar(from);
            return unlit != null ? unlit.Pos : from;
        }

        public void Interact()
        {
            var lost = NearestLost(Player.Pos);
            if (lost != null && Vector2.Distance(lost.Pos, Player.Pos) <= Soul.GatherRange)
            {
                GatherSoul(lost);
                return;
            }
            var altar = NearestUnlitAltar(Player.Pos);
            if (altar == null || Vector2.Distance(altar.Pos, Player.Pos) > Altar.InteractRange) return;
            if (ShadowsNear(altar.Pos) || Player.Wax <= LightCost)
            {
                Sound.Deny();
                return;
            }
            Player.ChangeWax(-LightCost);
            altar.Light();
            altar.Room.WavesLeft = 0; // The room is safe now: no more waves come to it.
            LitThisNight++;
            TotalSaved++;
            Sound.Light();
            Particles.Burst(altar.Pos + Vector2.up, 28, Art.Flame, 5f);
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

        void GatherSoul(Soul soul)
        {
            if (Player.Wax <= GatherCost)
            {
                Sound.Deny();
                return;
            }
            Player.ChangeWax(-GatherCost);
            soul.Gather();
            Sound.Light();
            Particles.Burst(soul.Pos, 16, Art.Core, 3.5f);
            hud.Toast(Loc.T("The soul follows you. Lead it to a lit candle", "Душа йде за тобою. Доведи її до запаленої свічки"));
        }

        void SaveSoul(Soul soul)
        {
            soul.Save();
            Rescued++;
            RescuedThisWeek++;
            Player.ChangeWax(LightPerSoul);
            Sound.Light();
            hud.Toast(Loc.T("A soul is free", "Душа вільна"));
        }

        bool AnySoulsLeft()
        {
            foreach (var soul in Souls)
                if (soul.State == Soul.Status.Lost || soul.State == Soul.Status.Following) return true;
            return false;
        }

        public int FollowerCount
        {
            get
            {
                int n = 0;
                foreach (var soul in Souls) if (soul.State == Soul.Status.Following) n++;
                return n;
            }
        }

        public Soul NearestLost(Vector2 from) => Nearest(from, Soul.Status.Lost);
        public Soul NearestFollower(Vector2 from) => Nearest(from, Soul.Status.Following);

        Soul Nearest(Vector2 from, Soul.Status status)
        {
            Soul best = null;
            float bestDistance = float.MaxValue;
            foreach (var soul in Souls)
            {
                if (soul.State != status) continue;
                float d = Vector2.Distance(soul.Pos, from);
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = soul;
            }
            return best;
        }

        public void LoseFollower(Soul soul)
        {
            soul.Snuff();
            LostSouls++;
            Sound.Extinguish();
            Shake(0.25f);
            hud.Toast(Loc.T("A ghost took a soul you were leading", "Привид забрав душу, яку ти вів"));
        }

        // With nobody left to save the night has no reason to go on, so the clock runs out in a few seconds.
        void RushToDawn()
        {
            dawnRushRate = Mathf.Max(NightLeft / DawnRushSeconds, 1f);
            foreach (var enemy in Enemies) enemy.Die(false);
            Enemies.Clear();
            hud.Toast(Loc.T("No souls are left in the dark. The sun is coming", "У темряві більше немає душ. Сонце вже сходить"));
        }

        // Sunrise reaches every soul still at the ray's side; those left in the dark stay lost.
        void EndAtSunrise()
        {
            foreach (var soul in Souls)
                if (soul.State == Soul.Status.Following) SaveSoul(soul);
            foreach (var enemy in Enemies) enemy.Die(false);
            Enemies.Clear();
            Sound.Light();
            State = GameState.Sunrise;
            hud.ShowSunrise();
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
                Player.Revive(checkpoint.Pos, Player.MaxWax * ReviveFraction);
                foreach (var enemy in Enemies)
                    if (Vector2.Distance(enemy.Pos, checkpoint.Pos) < 7f) enemy.Die(false);
                Enemies.RemoveAll(e => e.Dead);
                foreach (var bolt in bolts) bolt.Kill();
                bolts.Clear();
                foreach (var soul in Souls)
                    if (soul.State == Soul.Status.Following) soul.JoinPlayer();
                foreach (var trail in trails)
                    if (trail.IsNear(checkpoint.Pos, 7f)) trail.Kill();
                trails.RemoveAll(t => t.Dead);
                Shake(0.5f);
                hud.Toast(Loc.T("A candle rekindles your light", "Свічка знову запалила твоє світло"));
                return;
            }

            Player.Wax = 0f;
            Player.GoOut();
            State = GameState.Dying;
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
            Kills = 0;
            TotalSaved = 0;
            Day = 1;
            ShufflePalettes();
            cam.orthographicSize = PlayCameraSize;
            RescuedThisWeek = 0;
            SurvivalTime = 0f;
            Player = null;
            BeginDay();
        }

        /// <summary>The button on the sunrise screen: on to the next day, or a new week after Friday.</summary>
        public void AfterSunrise()
        {
            if (State != GameState.Sunrise) return;
            if (Day >= DaysInWeek)
            {
                StartRun();
                return;
            }
            Day++;
            BeginDay();
            Player.Wax = Player.MaxWax; // Each day starts with a full cape; blessings carry over.
        }

        void BeginDay()
        {
            BuildNight();
            hud.HidePanels();
            hud.ShowDayCard();
            intro = new SunrayIntro(this, Map.StartWindow, Player.Pos);
            State = GameState.Intro;
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
