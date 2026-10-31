using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NightTrolleybus.EditorTools
{
    /// <summary>Малює прості піксельні спрайти кодом. Файл спрайта можна замінити власним малюнком: наявні файли не перезаписуються.</summary>
    public static class PixelArt
    {
        private const string Folder = BuilderTools.Root + "/Art/Sprites/Generated";
        private const int PixelsPerUnit = 32;

        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
        private static readonly Color Skin = C(226, 178, 138);
        private static readonly Color Coat = C(58, 84, 130);
        private static readonly Color CoatDark = C(40, 58, 92);
        private static readonly Color Trousers = C(44, 44, 56);
        private static readonly Color Boots = C(26, 24, 28);
        private static readonly Color Cap = C(34, 44, 70);
        private static readonly Color Metal = C(140, 140, 150);
        private static readonly Color MetalDark = C(84, 84, 96);
        private static readonly Color Wood = C(120, 84, 50);
        private static readonly Color WoodDark = C(84, 58, 34);

        private static readonly Dictionary<string, Func<Canvas>> Painters = new Dictionary<string, Func<Canvas>>
        {
            { "Player_Idle", () => Person(0) },
            { "Player_Walk_A", () => Person(1) },
            { "Player_Walk_B", () => Person(2) },
            { "Passenger", Passenger },
            { "Shade_A", () => Shade(0) },
            { "Shade_B", () => Shade(1) },
            { "Flame_A", () => Flame(0) },
            { "Flame_B", () => Flame(1) },
            { "Flame_C", () => Flame(2) },
            { "Item_candle", Candle },
            { "Item_match", Match },
            { "Item_battery", Battery },
            { "Item_ticket", Ticket },
            { "BusWall", BusWall },
            { "BusFloor", () => Hull(576, 32, C(74, 70, 68), true) },
            { "BusCeiling", () => Hull(576, 32, C(86, 84, 84), false) },
            { "BusEnd", () => Hull(32, 192, C(78, 74, 72), false) },
            { "Seat", Seat },
            { "DriverSeat", DriverSeat },
            { "Heater", Heater },
            { "Switch", Switch },
            { "DoorLever", DoorLever },
            { "ExitDoor", ExitDoor },
            { "RepairPanel", RepairPanel },
            { "Crate", Crate },
            { "Charger", Charger },
            { "Counter", Counter },
            { "HiddenPlank", HiddenPlank },
            { "StopDoor", StopDoor },
            { "GroundTile", GroundTile },
            { "BrickWall", BrickWall },
            { "Building_A", () => Building(1, 96, 160) },
            { "Building_B", () => Building(2, 128, 192) },
            { "Building_C", () => Building(3, 96, 224) },
            { "Swing", Swing },
            { "MenuBackground", MenuBackground },
            { "MenuGlow", MenuGlow },
            { "Top_Player_Idle", () => TopPerson(0) },
            { "Top_Player_Walk_A", () => TopPerson(1) },
            { "Top_Player_Walk_B", () => TopPerson(-1) },
            { "Top_Shade_A", () => TopShade(0) },
            { "Top_Shade_B", () => TopShade(1) },
            { "Top_Floor", TopFloor },
            { "Top_Wall", TopWall },
            { "Top_Crate", TopCrate },
            { "Top_Counter", TopCounter },
            { "Top_Charger", TopCharger },
            { "Top_Door", TopDoor },
            { "Top_HiddenWall", TopHiddenWall },
        };

        /// <summary>Повертає спрайт, за потреби намалювавши його вперше.</summary>
        public static Sprite Get(string spriteName)
        {
            string path = $"{Folder}/{spriteName}.png";
            if (!File.Exists(path))
            {
                BuilderTools.EnsureFolder(Folder);
                File.WriteAllBytes(path, Painters[spriteName]().ToPng());
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                // Повний прямокутник потрібен для плиткового малювання тла.
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        private static Canvas Person(int walkFrame)
        {
            var c = new Canvas(26, 52);
            int leftLeg = walkFrame == 1 ? 3 : walkFrame == 2 ? 0 : 1;
            int rightLeg = walkFrame == 2 ? 3 : walkFrame == 1 ? 0 : 1;
            c.Rect(7, leftLeg, 5, 18 - leftLeg, Trousers);
            c.Rect(14, rightLeg, 5, 18 - rightLeg, Trousers);
            c.Rect(6, leftLeg, 6, 3, Boots);
            c.Rect(14, rightLeg, 6, 3, Boots);
            c.Rect(5, 16, 16, 20, Coat);
            c.Rect(5, 16, 3, 20, CoatDark);
            c.Rect(12, 18, 2, 18, CoatDark);
            c.Rect(5, 16, 16, 2, Boots);
            c.Rect(2, 19, 3, 15, CoatDark);
            c.Rect(21, 19, 3, 15, Coat);
            c.Rect(2, 17, 3, 2, Skin);
            c.Rect(21, 17, 3, 2, Skin);
            c.Rect(8, 36, 11, 9, Skin);
            c.Rect(14, 39, 2, 2, Boots);
            c.Rect(17, 39, 1, 2, Boots);
            c.Rect(6, 44, 15, 4, Cap);
            c.Rect(14, 43, 9, 2, Boots);
            c.Rect(8, 48, 11, 2, Cap);
            return c;
        }

        // Сірий рисунок: колір пасажиру задає відтінок у даних.
        private static Canvas Passenger()
        {
            var c = new Canvas(22, 44);
            Color body = C(210, 210, 210);
            Color shade = C(165, 165, 165);
            c.Rect(5, 0, 5, 14, shade);
            c.Rect(12, 0, 5, 14, shade);
            c.Rect(3, 12, 16, 18, body);
            c.Rect(3, 12, 3, 18, shade);
            c.Rect(6, 30, 10, 9, C(245, 245, 245));
            c.Rect(5, 37, 12, 5, shade);
            c.Rect(12, 33, 2, 2, C(40, 40, 40));
            return c;
        }

        private static Canvas Shade(int frame)
        {
            var c = new Canvas(29, 51);
            Color body = C(44, 36, 74);
            Color edge = C(76, 62, 120);
            c.Ellipse(14, 34, 11, 16, body);
            for (int x = 3; x < 26; x++)
            {
                int height = 16 + ((x + frame * 3) % 6);
                c.Rect(x, 22 - height + 2, 1, height, body);
            }

            for (int y = 22; y < 48; y += 2)
                c.Set(3 + Math.Abs(34 - y) / 3, y, edge);

            c.Rect(9, 36, 4, 3, C(230, 225, 255));
            c.Rect(17, 36, 4, 3, C(230, 225, 255));
            c.Rect(11, 36, 2, 2, Boots);
            c.Rect(19, 36, 2, 2, Boots);
            return c;
        }

        // Майже білий рисунок: колір полум'я задає FlameView за станом Вогника.
        private static Canvas Flame(int frame)
        {
            var c = new Canvas(16, 20);
            int sway = frame == 0 ? 0 : frame == 1 ? 1 : -1;
            c.Ellipse(8, 6, 6, 6, C(190, 190, 190));
            for (int y = 8; y < 18; y++)
            {
                int half = Math.Max(0, 6 - (y - 8) * 6 / 10);
                int center = 8 + (y > 12 ? sway : 0);
                c.Rect(center - half, y, half * 2, 1, C(190, 190, 190));
            }

            c.Ellipse(8, 6, 3, 4, Color.white);
            c.Rect(7 + sway / 2, 8, 2, 5, Color.white);
            c.Rect(6, 5, 1, 2, Boots);
            c.Rect(10, 5, 1, 2, Boots);
            return c;
        }

        private static Canvas Candle()
        {
            var c = new Canvas(8, 20);
            c.Rect(1, 0, 6, 3, MetalDark);
            c.Rect(2, 3, 4, 12, C(240, 232, 200));
            c.Rect(2, 3, 1, 12, C(200, 190, 160));
            c.Rect(4, 15, 1, 2, Boots);
            c.Rect(3, 17, 2, 3, C(255, 190, 80));
            return c;
        }

        private static Canvas Match()
        {
            var c = new Canvas(4, 14);
            c.Rect(1, 0, 2, 11, C(214, 176, 120));
            c.Rect(0, 11, 4, 3, C(200, 60, 40));
            return c;
        }

        private static Canvas Battery()
        {
            var c = new Canvas(14, 16);
            c.Rect(0, 0, 14, 12, C(50, 70, 92));
            c.Rect(0, 8, 14, 4, C(90, 150, 200));
            c.Rect(2, 12, 3, 2, Metal);
            c.Rect(9, 12, 3, 2, Metal);
            c.Rect(5, 2, 4, 4, C(230, 220, 90));
            return c;
        }

        private static Canvas Ticket()
        {
            var c = new Canvas(14, 8);
            c.Rect(0, 0, 14, 8, C(226, 212, 130));
            c.Rect(2, 5, 8, 1, WoodDark);
            c.Rect(2, 3, 5, 1, WoodDark);
            c.Set(12, 0, Clear);
            c.Set(12, 7, Clear);
            return c;
        }

        private static Canvas BusWall()
        {
            const int width = 512;
            const int height = 128;
            var c = new Canvas(width, height);
            Color panel = C(150, 138, 118);
            Color lower = C(124, 62, 50);
            c.Rect(0, 0, width, height, panel);
            c.Rect(0, 0, width, 40, lower);
            c.Rect(0, 40, width, 3, C(226, 204, 150));
            for (int x = 15; x < width; x += 32)
            {
                c.Rect(x, 0, 1, 40, C(100, 50, 40));
                c.Set(x, 118, MetalDark);
                c.Set(x, 44, MetalDark);
            }

            // Отвори вікон збігаються з простінками, щоб за склом було видно місто.
            foreach (int windowX in new[] { 58, 186, 314 })
            {
                c.Rect(windowX - 3, 45, 70, 70, MetalDark);
                c.Rect(windowX, 48, 64, 64, Clear);
                c.Rect(windowX + 31, 48, 2, 64, MetalDark);
            }

            return c;
        }

        private static Canvas Hull(int width, int height, Color color, bool ribbed)
        {
            var c = new Canvas(width, height);
            c.Rect(0, 0, width, height, color);
            c.Rect(0, height - 2, width, 2, C(190, 186, 180));
            c.Rect(0, 0, width, 2, C(40, 38, 38));
            if (ribbed)
            {
                for (int x = 0; x < width; x += 8)
                    c.Rect(x, 6, 1, height - 10, C(50, 48, 46));
            }

            return c;
        }

        private static Canvas Seat()
        {
            var c = new Canvas(35, 32);
            Color cushion = C(150, 62, 52);
            c.Rect(3, 0, 3, 12, MetalDark);
            c.Rect(29, 0, 3, 12, MetalDark);
            c.Rect(0, 11, 35, 8, cushion);
            c.Rect(0, 11, 35, 2, C(100, 40, 34));
            c.Rect(0, 19, 8, 13, cushion);
            c.Rect(0, 29, 8, 3, C(190, 90, 70));
            return c;
        }

        private static Canvas DriverSeat()
        {
            var c = new Canvas(32, 38);
            c.Rect(13, 0, 6, 12, MetalDark);
            c.Rect(4, 12, 22, 7, C(60, 80, 124));
            c.Rect(0, 12, 7, 24, C(60, 80, 124));
            c.Rect(0, 32, 7, 4, C(100, 124, 170));
            c.Rect(24, 6, 3, 24, MetalDark);
            c.Ellipse(26, 30, 6, 6, Boots);
            c.Ellipse(26, 30, 4, 4, Clear);
            return c;
        }

        private static Canvas Heater()
        {
            var c = new Canvas(38, 32);
            c.Rect(0, 0, 38, 32, C(146, 86, 42));
            c.Rect(2, 2, 34, 28, C(70, 40, 26));
            for (int x = 4; x < 34; x += 5)
                c.Rect(x, 6, 3, 18, C(214, 100, 40));
            c.Rect(0, 27, 38, 5, MetalDark);
            c.Rect(30, 28, 3, 3, C(240, 220, 90));
            return c;
        }

        private static Canvas Switch()
        {
            var c = new Canvas(13, 19);
            c.Rect(0, 0, 13, 19, Metal);
            c.Rect(1, 1, 11, 17, C(220, 214, 180));
            c.Rect(5, 5, 3, 9, C(180, 50, 40));
            return c;
        }

        private static Canvas DoorLever()
        {
            var c = new Canvas(16, 26);
            c.Rect(0, 0, 16, 26, MetalDark);
            c.Rect(2, 2, 12, 22, C(70, 122, 110));
            c.Rect(7, 6, 2, 14, Boots);
            c.Rect(5, 18, 6, 3, C(226, 204, 150));
            return c;
        }

        private static Canvas ExitDoor()
        {
            var c = new Canvas(20, 52);
            c.Rect(0, 0, 20, 52, C(70, 230, 110));
            c.Rect(3, 3, 14, 46, C(30, 110, 60));
            c.Rect(5, 28, 10, 18, C(170, 255, 190));
            c.Rect(13, 20, 3, 3, Color.white);
            return c;
        }

        // Майже білий рисунок: станція фарбується в сірий або червоний за станом несправності.
        private static Canvas RepairPanel()
        {
            var c = new Canvas(20, 20);
            c.Rect(0, 0, 20, 20, C(200, 200, 200));
            c.Rect(2, 2, 16, 16, C(240, 240, 240));
            c.Rect(8, 4, 4, 9, C(70, 70, 70));
            c.Rect(8, 14, 4, 3, C(70, 70, 70));
            return c;
        }

        private static Canvas Crate()
        {
            var c = new Canvas(48, 48);
            c.Rect(0, 0, 48, 48, WoodDark);
            c.Rect(3, 3, 42, 42, Wood);
            for (int i = 0; i < 42; i++)
            {
                c.Rect(3 + i, 3 + i, 3, 1, WoodDark);
            }

            c.Rect(3, 22, 42, 3, WoodDark);
            return c;
        }

        private static Canvas Charger()
        {
            var c = new Canvas(29, 51);
            c.Rect(0, 0, 29, 51, C(70, 96, 130));
            c.Rect(3, 28, 23, 18, C(30, 44, 60));
            c.Rect(6, 32, 15, 3, C(120, 220, 250));
            c.Rect(6, 38, 9, 3, C(120, 220, 250));
            c.Rect(11, 8, 7, 14, C(230, 220, 90));
            return c;
        }

        private static Canvas Counter()
        {
            var c = new Canvas(38, 38);
            c.Rect(0, 0, 38, 28, WoodDark);
            c.Rect(0, 24, 38, 4, Wood);
            c.Rect(3, 3, 32, 18, Wood);
            c.Rect(6, 29, 26, 3, Metal);
            c.Rect(10, 31, 4, 7, WoodDark);
            return c;
        }

        private static Canvas HiddenPlank()
        {
            var c = new Canvas(128, 32);
            c.Rect(0, 0, 128, 32, C(150, 134, 220));
            c.Rect(0, 26, 128, 6, C(220, 210, 255));
            for (int x = 15; x < 128; x += 16)
                c.Rect(x, 0, 1, 26, C(100, 90, 170));
            return c;
        }

        private static Canvas StopDoor()
        {
            var c = new Canvas(45, 64);
            c.Rect(0, 0, 45, 64, C(150, 138, 118));
            c.Rect(3, 0, 39, 60, C(40, 66, 60));
            c.Rect(6, 0, 15, 57, C(80, 130, 118));
            c.Rect(24, 0, 15, 57, C(80, 130, 118));
            c.Rect(8, 26, 11, 26, C(180, 220, 210));
            c.Rect(26, 26, 11, 26, C(180, 220, 210));
            return c;
        }

        private static Canvas GroundTile()
        {
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 32, 32, C(150, 146, 140));
            c.Rect(0, 29, 32, 3, C(210, 206, 196));
            c.Rect(0, 0, 32, 1, C(90, 86, 82));
            c.Rect(0, 0, 1, 32, C(90, 86, 82));
            c.Rect(6, 10, 3, 2, C(110, 106, 100));
            c.Rect(20, 20, 4, 2, C(110, 106, 100));
            c.Rect(24, 6, 2, 3, C(110, 106, 100));
            return c;
        }

        private static Canvas BrickWall()
        {
            var c = new Canvas(64, 32);
            c.Rect(0, 0, 64, 32, C(110, 88, 84));
            Color mortar = C(70, 58, 58);
            for (int y = 0; y < 32; y += 8)
            {
                c.Rect(0, y, 64, 1, mortar);
                int offset = y % 16 == 0 ? 0 : 8;
                for (int x = offset; x < 64; x += 16)
                    c.Rect(x, y, 1, 8, mortar);
            }

            return c;
        }

        private static Canvas Building(int seed, int width, int height)
        {
            var random = new System.Random(seed);
            var c = new Canvas(width, height);
            c.Rect(0, 0, width, height, C(210, 210, 215));
            c.Rect(0, height - 4, width, 4, C(235, 235, 240));
            for (int y = 12; y < height - 16; y += 22)
            {
                for (int x = 10; x < width - 14; x += 22)
                {
                    // Світяться лише поодинокі вікна: місто вже майже згасло.
                    int roll = random.Next(12);
                    Color window = roll == 0 ? C(255, 214, 120) : C(120, 120, 130);
                    c.Rect(x, y, 12, 14, window);
                }
            }

            return c;
        }

        private static Canvas Swing()
        {
            var c = new Canvas(45, 12);
            for (int x = 0; x < 45; x++)
            {
                int height = 2 + (x * 9 / 45);
                c.Rect(x, 6 - height / 2, 1, height, new Color(1f, 1f, 1f, 0.5f + x / 90f));
            }

            return c;
        }

        // Заставка меню: згасле місто, дроти й тролейбус з єдиним теплим вікном.
        private static Canvas MenuBackground()
        {
            const int width = 480;
            const int height = 270;
            var random = new System.Random(7);
            var c = new Canvas(width, height);

            for (int y = 0; y < height; y++)
                c.Rect(0, y, width, 1, Color.Lerp(C(30, 26, 50), C(5, 6, 16), y / (float)height));

            // Кілька останніх зір, решта вже згасла.
            for (int i = 0; i < 26; i++)
                c.Set(random.Next(width), 150 + random.Next(120), random.Next(4) == 0 ? C(190, 190, 220) : C(80, 80, 110));

            MenuSkyline(c, random, C(14, 15, 30), 70, 150, 34, 0);
            MenuSkyline(c, random, C(22, 21, 40), 60, 110, 46, 14);

            c.Rect(0, 0, width, 50, C(13, 13, 18));
            c.Rect(0, 49, width, 1, C(40, 38, 46));
            for (int x = 6; x < width; x += 44)
                c.Rect(x, 24, 22, 2, C(70, 68, 66));

            // Контактні дроти.
            c.Rect(0, 200, width, 1, C(60, 60, 76));
            c.Rect(0, 206, width, 1, C(60, 60, 76));

            const int busX = 225;
            const int busRight = 455;
            Color hull = C(38, 30, 34);
            c.Rect(busX, 52, busRight - busX, 22, C(74, 28, 26));
            c.Rect(busX, 74, busRight - busX, 3, C(150, 120, 80));
            c.Rect(busX, 77, busRight - busX, 58, hull);
            c.Rect(busX + 4, 135, busRight - busX - 8, 4, C(26, 22, 26));

            // Штанги до дротів.
            for (int i = 0; i < 62; i++)
            {
                c.Rect(320 + i, 139 + i, 2, 1, Metal);
                c.Rect(306 + i, 139 + i + (i < 61 ? 0 : 5), 2, 1, MetalDark);
            }

            // Вікна темні, крім одного: там сидить Вогник.
            for (int x = busX + 10; x < busRight - 30; x += 36)
            {
                bool lit = x + 15 >= 322 && x + 15 < 358;
                c.Rect(x, 88, 30, 38, lit ? C(150, 82, 34) : C(16, 18, 30));
                c.Rect(x, 88, 30, 2, MetalDark);
            }

            c.Rect(busRight - 26, 84, 22, 44, C(22, 28, 44));
            c.Ellipse(busX + 50, 50, 15, 15, Boots);
            c.Ellipse(busX + 50, 50, 7, 7, MetalDark);
            c.Ellipse(busRight - 50, 50, 15, 15, Boots);
            c.Ellipse(busRight - 50, 50, 7, 7, MetalDark);
            return c;
        }

        private static void MenuSkyline(Canvas c, System.Random random, Color color, int minHeight, int maxHeight, int blockWidth, int start)
        {
            for (int x = start; x < 480; x += blockWidth + random.Next(8))
            {
                int blockHeight = random.Next(minHeight, maxHeight);
                c.Rect(x, 40, blockWidth, blockHeight, color);
                for (int y = 56; y < 34 + blockHeight; y += 12)
                {
                    for (int wx = x + 5; wx < x + blockWidth - 5; wx += 9)
                        c.Rect(wx, y, 4, 6, random.Next(40) == 0 ? C(120, 92, 50) : C(10, 10, 20));
                }
            }
        }

        private static Canvas MenuGlow()
        {
            const int size = 128;
            var c = new Canvas(size, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float alpha = Mathf.Clamp01(1f - distance);
                    c.Set(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            return c;
        }

        // Вид зверху: персонаж дивиться вправо, а в грі спрайт обертається за поглядом.
        private static Canvas TopPerson(int step)
        {
            var c = new Canvas(28, 28);
            c.Rect(16 + step * 3, 21, 7, 4, Skin);
            c.Rect(16 - step * 3, 3, 7, 4, Skin);
            c.Rect(9 + step * 2, 19, 10, 5, CoatDark);
            c.Rect(9 - step * 2, 4, 10, 5, CoatDark);
            c.Ellipse(13, 14, 8, 11, Coat);
            c.Ellipse(11, 14, 3, 8, CoatDark);
            c.Ellipse(15, 14, 6, 6, Cap);
            c.Rect(20, 11, 3, 6, Boots);
            c.Rect(14, 13, 3, 2, C(70, 90, 130));
            return c;
        }

        private static Canvas TopShade(int frame)
        {
            var c = new Canvas(30, 30);
            Color body = C(44, 36, 74);
            Color edge = C(76, 62, 120);
            c.Ellipse(15, 15, 13, 13, edge);
            c.Ellipse(15, 15, 11, 11, body);
            for (int i = 0; i < 8; i++)
            {
                float angle = (i + frame * 0.5f) * Mathf.PI / 4f;
                c.Rect(14 + (int)(Mathf.Cos(angle) * 13f), 14 + (int)(Mathf.Sin(angle) * 13f), 3, 3, body);
            }

            c.Rect(9, 16, 4, 4, C(230, 225, 255));
            c.Rect(17, 16, 4, 4, C(230, 225, 255));
            c.Rect(11, 17, 2, 2, Boots);
            c.Rect(19, 17, 2, 2, Boots);
            return c;
        }

        private static Canvas TopFloor()
        {
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 32, 32, C(150, 146, 140));
            c.Rect(0, 0, 32, 1, C(110, 106, 100));
            c.Rect(0, 0, 1, 32, C(110, 106, 100));
            c.Rect(7, 11, 3, 2, C(128, 124, 118));
            c.Rect(21, 22, 4, 2, C(128, 124, 118));
            c.Rect(25, 6, 2, 3, C(170, 166, 160));
            return c;
        }

        private static Canvas TopWall()
        {
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 32, 32, C(96, 76, 72));
            c.Rect(0, 0, 32, 2, C(60, 48, 46));
            c.Rect(0, 30, 32, 2, C(140, 118, 110));
            c.Rect(0, 15, 32, 1, C(60, 48, 46));
            c.Rect(10, 16, 1, 14, C(60, 48, 46));
            c.Rect(22, 2, 1, 13, C(60, 48, 46));
            return c;
        }

        private static Canvas TopCrate()
        {
            var c = new Canvas(48, 48);
            c.Rect(0, 0, 48, 48, WoodDark);
            c.Rect(3, 3, 42, 42, Wood);
            for (int y = 11; y < 45; y += 8)
                c.Rect(3, y, 42, 1, WoodDark);
            c.Rect(3, 3, 5, 42, WoodDark);
            c.Rect(40, 3, 5, 42, WoodDark);
            return c;
        }

        private static Canvas TopCounter()
        {
            var c = new Canvas(38, 38);
            c.Rect(0, 0, 38, 38, WoodDark);
            c.Rect(2, 2, 34, 34, Wood);
            c.Rect(6, 17, 26, 4, Metal);
            c.Rect(26, 12, 5, 14, MetalDark);
            return c;
        }

        private static Canvas TopCharger()
        {
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 32, 32, C(70, 96, 130));
            c.Rect(4, 4, 24, 24, C(30, 44, 60));
            c.Rect(14, 8, 5, 8, C(230, 220, 90));
            c.Rect(11, 15, 6, 8, C(230, 220, 90));
            return c;
        }

        private static Canvas TopDoor()
        {
            var c = new Canvas(32, 64);
            c.Rect(0, 0, 32, 64, C(124, 62, 50));
            c.Rect(0, 0, 32, 5, C(226, 204, 150));
            c.Rect(0, 59, 32, 5, C(226, 204, 150));
            c.Rect(24, 8, 8, 48, C(80, 230, 130));
            c.Rect(4, 12, 16, 40, C(86, 84, 84));
            return c;
        }

        private static Canvas TopHiddenWall()
        {
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 32, 32, C(96, 76, 72));
            c.Rect(0, 15, 32, 1, C(60, 48, 46));
            c.Rect(10, 16, 1, 16, C(60, 48, 46));
            c.Rect(22, 0, 1, 15, C(60, 48, 46));
            // Ледь помітна фіолетова тріщина: уважний гравець здогадається підсвітити це місце.
            for (int i = 4; i < 28; i += 3)
                c.Rect(i, i, 2, 2, C(150, 134, 220));
            return c;
        }

        private class Canvas
        {
            private readonly int width;
            private readonly int height;
            private readonly Color[] pixels;

            public Canvas(int width, int height)
            {
                this.width = width;
                this.height = height;
                pixels = new Color[width * height];
                Array.Fill(pixels, Clear);
            }

            public void Set(int x, int y, Color color)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                    pixels[y * width + x] = color;
            }

            public void Rect(int x, int y, int rectWidth, int rectHeight, Color color)
            {
                for (int py = y; py < y + rectHeight; py++)
                {
                    for (int px = x; px < x + rectWidth; px++)
                        Set(px, py, color);
                }
            }

            public void Ellipse(int centerX, int centerY, int radiusX, int radiusY, Color color)
            {
                for (int py = centerY - radiusY; py <= centerY + radiusY; py++)
                {
                    for (int px = centerX - radiusX; px <= centerX + radiusX; px++)
                    {
                        float dx = (px - centerX) / (float)radiusX;
                        float dy = (py - centerY) / (float)radiusY;
                        if (dx * dx + dy * dy <= 1f)
                            Set(px, py, color);
                    }
                }
            }

            public byte[] ToPng()
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.SetPixels(pixels);
                texture.Apply();
                byte[] png = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);
                return png;
            }
        }
    }
}
