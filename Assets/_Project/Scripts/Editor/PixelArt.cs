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
            { "Flame_Candle", FlameCandle },
            { "Flame_Bowl", FlameBowl },
            { "Key_E", KeyE },
            { "UI_Frame", () => UiFrame(C(150, 112, 70), C(18, 15, 16)) },
            { "UI_Paper", () => UiFrame(C(120, 88, 56), C(232, 222, 190)) },
            { "UI_IconFlame", UiIconFlame },
            { "UI_IconHeat", UiIconHeat },
            { "UI_IconPower", UiIconPower },
            { "UI_IconFear", UiIconFear },
            { "UI_IconHealth", UiIconHealth },
            { "UI_Arrow", UiArrow },
            { "UI_Trolley", UiTrolley },
            { "UI_StopMark", UiStopMark },
            { "UI_Dial", UiDial },
            { "Top_Barrel", TopBarrel },
            { "Top_Spool", TopSpool },
            { "Top_Tires", TopTires },
            { "Top_Workbench", TopWorkbench },
            { "Top_Cart", TopCart },
            { "Top_BoxPile", TopBoxPile },
            { "Top_Checkout", TopCheckout },
            { "Top_Shelf", TopShelf },
            { "Top_AmmoBox", TopAmmoBox },
            { "Top_AmmoBox_Open", TopAmmoBoxOpen },
            { "Top_Target", TopTarget },
            { "Top_GunRack", TopGunRack },
            { "Decal_Oil", () => Stain(40, 30, new Color(0.03f, 0.03f, 0.04f, 0.6f)) },
            { "Decal_Spill", () => Stain(36, 28, new Color(0.45f, 0.5f, 0.2f, 0.45f)) },
            { "Decal_Dust", () => Stain(44, 36, new Color(0f, 0f, 0f, 0.18f)) },
            { "Decal_Crack", DecalCrack },
            { "Decal_Cable", DecalCable },
            { "Decal_Paper", DecalPaper },
            { "Decal_Can", DecalCan },
            { "Decal_Shells", DecalShells },
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
            { "Heater_Off", () => Heater(0) },
            { "Heater_On_A", () => Heater(1) },
            { "Heater_On_B", () => Heater(2) },
            { "DoorLever_Open", DoorLeverOpen },
            { "CabinDoor_Closed", CabinDoorClosed },
            { "CabinDoor_Open", CabinDoorOpen },
            { "Switch", Switch },
            { "DoorLever", DoorLever },
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
            { "MenuBackground", MenuBackground },
            { "MenuGlow", MenuGlow },
            { "Top_Player_Idle", () => TopPerson(0) },
            { "Top_Player_Walk_A", () => TopPerson(1) },
            { "Top_Player_Walk_B", () => TopPerson(-1) },
            { "Top_Player_Back_Idle", () => TopPersonBack(0) },
            { "Top_Player_Back_Walk_A", () => TopPersonBack(1) },
            { "Top_Player_Back_Walk_B", () => TopPersonBack(-1) },
            { "Top_Shade_A", () => TopShade(0) },
            { "Top_Shade_B", () => TopShade(1) },
            { "Top_Floor_Depot", TopFloorDepot },
            { "Top_Wall_Depot", TopWallDepot },
            { "Top_Floor_Market", TopFloorMarket },
            { "Top_Wall_Market", TopWallMarket },
            { "Top_Floor_GunShop", TopFloorGunShop },
            { "Top_Wall_GunShop", TopWallGunShop },
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
                // Інакше Unity може нарізати малюнок на частини й обрізати його.
                importer.spriteImportMode = SpriteImportMode.Single;
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

        /// <summary>Спрайт для рамки, що розтягується: кути лишаються незмінними.</summary>
        public static Sprite GetSliced(string spriteName, int border)
        {
            Sprite sprite = Get(spriteName);
            var importer = (TextureImporter)AssetImporter.GetAtPath($"{Folder}/{spriteName}.png");
            var wanted = new Vector4(border, border, border, border);
            if (importer.spriteBorder == wanted)
                return sprite;

            importer.spriteBorder = wanted;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{spriteName}.png");
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
            // Коробка сірників: етикетка з вогником і сірник, що стирчить зверху.
            var c = new Canvas(18, 18);
            c.Rect(4, 10, 2, 5, C(226, 190, 130));
            c.Rect(3, 15, 4, 3, C(220, 60, 40));
            c.Rect(9, 10, 2, 3, C(226, 190, 130));
            c.Rect(8, 13, 4, 2, C(220, 60, 40));
            c.Rect(0, 0, 18, 11, C(70, 40, 30));
            c.Rect(1, 1, 16, 9, C(196, 70, 48));
            c.Rect(0, 0, 18, 2, C(46, 36, 34));
            c.Rect(7, 3, 4, 5, C(250, 190, 70));
            c.Rect(8, 3, 2, 2, C(255, 240, 170));
            return c;
        }

        private static Canvas Battery()
        {
            // Акумулятор: дві клеми, плюс і мінус та блискавка на корпусі.
            var c = new Canvas(20, 18);
            c.Rect(0, 0, 20, 15, C(36, 48, 66));
            c.Rect(1, 1, 18, 13, C(62, 88, 116));
            c.Rect(1, 11, 18, 3, C(110, 170, 220));
            c.Rect(3, 15, 4, 3, C(220, 70, 56));
            c.Rect(13, 15, 4, 3, Metal);
            c.Rect(3, 12, 3, 1, C(255, 240, 230));
            c.Rect(4, 11, 1, 3, C(255, 240, 230));
            c.Rect(14, 12, 3, 1, C(20, 24, 32));
            c.Rect(10, 6, 3, 4, C(250, 216, 70));
            c.Rect(8, 5, 4, 2, C(250, 216, 70));
            c.Rect(7, 2, 3, 3, C(250, 216, 70));
            return c;
        }

        private static Canvas Ticket()
        {
            // Квиток: рожевий корінець із перфорацією, номер і маленький тролейбус.
            var c = new Canvas(20, 12);
            c.Rect(0, 0, 20, 12, C(120, 96, 50));
            c.Rect(1, 1, 18, 10, C(240, 224, 140));
            c.Rect(1, 1, 4, 10, C(226, 120, 120));
            for (int y = 1; y < 11; y += 2)
                c.Set(5, y, C(120, 96, 50));
            c.Rect(7, 8, 10, 1, WoodDark);
            c.Rect(8, 3, 8, 4, C(60, 110, 150));
            c.Rect(9, 5, 2, 1, C(220, 240, 250));
            c.Rect(12, 5, 2, 1, C(220, 240, 250));
            c.Set(0, 5, Clear);
            c.Set(19, 5, Clear);
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

        // Кадр 0: піч холодна, сірі ґрати й згаслий вогник. Кадри 1 і 2: розпечені ґрати й полум'я, що ворушиться.
        private static Canvas Heater(int frame)
        {
            bool isOn = frame > 0;
            var c = new Canvas(38, 32);
            c.Rect(0, 0, 38, 32, isOn ? C(146, 86, 42) : C(84, 78, 76));
            c.Rect(2, 2, 34, 28, isOn ? C(70, 30, 18) : C(30, 28, 30));
            if (isOn)
            {
                for (int x = 4; x < 34; x += 5)
                {
                    int height = 8 + (x + frame * 3) % 7;
                    c.Rect(x, 4, 4, height, C(240, 120, 36));
                    c.Rect(x + 1, 4, 2, height / 2 + 1, C(255, 220, 110));
                }
            }

            Color bars = isOn ? C(255, 170, 70) : C(64, 62, 66);
            for (int x = 4; x < 34; x += 5)
                c.Rect(x + 3, 4, 1, 20, bars);

            c.Rect(0, 27, 38, 5, MetalDark);
            c.Rect(30, 28, 3, 3, isOn ? C(255, 80, 50) : C(50, 44, 44));
            return c;
        }

        private static Canvas DoorLeverOpen()
        {
            var c = new Canvas(16, 26);
            c.Rect(0, 0, 16, 26, MetalDark);
            c.Rect(2, 2, 12, 22, C(70, 122, 110));
            c.Rect(7, 6, 2, 14, Boots);
            c.Rect(5, 5, 6, 3, C(120, 220, 170));
            return c;
        }

        private static Canvas CabinDoorClosed()
        {
            Color frame = C(58, 52, 54);
            Color panel = C(120, 44, 40);
            Color glass = C(32, 40, 58);
            var c = new Canvas(30, 56);
            c.Rect(0, 0, 30, 56, frame);
            c.Rect(2, 0, 12, 54, panel);
            c.Rect(16, 0, 12, 54, panel);
            c.Rect(4, 24, 8, 26, glass);
            c.Rect(18, 24, 8, 26, glass);
            c.Rect(14, 0, 2, 54, C(20, 18, 20));
            c.Rect(11, 16, 2, 5, Metal);
            c.Rect(17, 16, 2, 5, Metal);
            return c;
        }

        // Стулки складені до країв, а в прорізі видно ніч і мокру дорогу.
        private static Canvas CabinDoorOpen()
        {
            Color frame = C(58, 52, 54);
            var c = new Canvas(30, 56);
            c.Rect(0, 0, 30, 56, frame);
            c.Rect(2, 0, 26, 54, C(10, 14, 30));
            c.Rect(2, 0, 26, 9, C(26, 26, 32));
            c.Rect(5, 4, 10, 1, C(70, 68, 66));
            c.Rect(7, 40, 1, 1, C(190, 190, 220));
            c.Rect(19, 47, 1, 1, C(190, 190, 220));
            c.Rect(13, 30, 1, 1, C(120, 120, 150));
            c.Rect(2, 0, 3, 54, C(120, 44, 40));
            c.Rect(25, 0, 3, 54, C(120, 44, 40));
            c.Rect(3, 24, 1, 26, C(32, 40, 58));
            c.Rect(26, 24, 1, 26, C(32, 40, 58));
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
            // Той самий водій, що в салоні, але стиснутий по висоті: згори видно кашкет і плечі, а ноги короткі.
            var c = new Canvas(26, 36);
            TopLegs(c, step);
            c.Rect(5, 8, 16, 13, Coat);
            c.Rect(5, 8, 3, 13, CoatDark);
            c.Rect(12, 9, 2, 12, CoatDark);
            c.Rect(5, 8, 16, 1, Boots);
            c.Rect(5, 19, 16, 2, C(78, 106, 156));
            c.Rect(2, 11, 3, 9, CoatDark);
            c.Rect(21, 11, 3, 9, Coat);
            c.Rect(2, 9, 3, 2, Skin);
            c.Rect(21, 9, 3, 2, Skin);
            c.Rect(8, 21, 11, 5, Skin);
            c.Rect(14, 23, 2, 1, Boots);
            c.Rect(17, 23, 1, 1, Boots);
            c.Rect(15, 25, 7, 1, Boots);
            c.Rect(7, 26, 13, 4, Cap);
            c.Rect(8, 30, 11, 2, C(50, 62, 94));
            return c;
        }

        // Водій спиною: без обличчя й козирка, а пальто суцільне.
        private static Canvas TopPersonBack(int step)
        {
            var c = new Canvas(26, 36);
            TopLegs(c, step);
            c.Rect(5, 8, 16, 13, Coat);
            c.Rect(5, 8, 3, 13, CoatDark);
            c.Rect(18, 8, 3, 13, CoatDark);
            c.Rect(5, 8, 16, 1, Boots);
            c.Rect(5, 19, 16, 2, C(78, 106, 156));
            c.Rect(2, 11, 3, 9, CoatDark);
            c.Rect(21, 11, 3, 9, CoatDark);
            c.Rect(2, 9, 3, 2, Skin);
            c.Rect(21, 9, 3, 2, Skin);
            c.Rect(8, 21, 11, 5, Skin);
            c.Rect(8, 24, 11, 2, C(52, 38, 30));
            c.Rect(7, 26, 13, 4, Cap);
            c.Rect(8, 30, 11, 2, C(50, 62, 94));
            return c;
        }

        private static void TopLegs(Canvas c, int step)
        {
            int leftLeg = step == 1 ? 3 : step == -1 ? 1 : 2;
            int rightLeg = step == -1 ? 3 : step == 1 ? 1 : 2;
            c.Ellipse(13, 3, 9, 3, new Color(0f, 0f, 0f, 0.35f));
            c.Rect(8, leftLeg, 4, 9 - leftLeg, Trousers);
            c.Rect(14, rightLeg, 4, 9 - rightLeg, Trousers);
            c.Rect(7, leftLeg, 5, 2, Boots);
            c.Rect(14, rightLeg, 5, 2, Boots);
        }

        private static Canvas FlameCandle()
        {
            var c = new Canvas(16, 10);
            c.Rect(5, 0, 6, 8, C(253, 231, 205));
            c.Rect(4, 8, 9, 2, C(252, 229, 206));
            return c;
        }

        // Металева бочка: кришка зверху, обручі й іржа.
        private static Canvas TopBarrel()
        {
            var c = new Canvas(28, 34);
            c.Rect(2, 2, 24, 28, C(70, 96, 110));
            c.Rect(2, 2, 5, 28, C(46, 64, 76));
            c.Rect(20, 2, 3, 28, C(104, 136, 150));
            c.Rect(2, 8, 24, 2, C(36, 48, 58));
            c.Rect(2, 20, 24, 2, C(36, 48, 58));
            c.Rect(9, 12, 7, 6, C(226, 180, 50));
            c.Rect(11, 13, 3, 3, C(30, 28, 30));
            c.Ellipse(14, 2, 12, 2, C(46, 64, 76));
            c.Ellipse(14, 29, 12, 4, C(104, 136, 150));
            c.Ellipse(14, 29, 9, 2, C(58, 80, 94));
            c.Rect(19, 29, 3, 2, C(36, 48, 58));
            return c;
        }

        // Дерев'яна котушка, намотана помаранчевим кабелем, з кінцем, що звисає.
        private static Canvas TopSpool()
        {
            var c = new Canvas(36, 32);
            c.Rect(5, 7, 26, 18, C(214, 110, 40));
            for (int y = 8; y < 25; y += 3)
                c.Rect(5, y, 26, 1, C(150, 70, 24));
            c.Ellipse(5, 16, 5, 15, C(130, 92, 56));
            c.Ellipse(5, 16, 2, 5, C(70, 48, 28));
            c.Rect(29, 1, 4, 30, C(130, 92, 56));
            c.Rect(33, 1, 2, 30, C(84, 58, 34));
            c.Rect(25, 1, 2, 7, C(214, 110, 40));
            return c;
        }

        // Стос із трьох шин: видно протектор збоку й отвір зверху.
        private static Canvas TopTires()
        {
            var c = new Canvas(36, 36);
            for (int level = 0; level < 3; level++)
            {
                int y = 1 + level * 9;
                c.Rect(2, y + 3, 32, 8, C(30, 30, 34));
                c.Ellipse(18, y + 3, 16, 3, C(30, 30, 34));
                for (int x = 4; x < 33; x += 4)
                    c.Rect(x, y + 3, 2, 8, C(58, 58, 64));
                c.Rect(2, y + 10, 32, 1, C(66, 66, 72));
            }

            c.Ellipse(18, 29, 16, 5, C(44, 44, 50));
            c.Ellipse(18, 29, 8, 2, C(12, 12, 14));
            return c;
        }

        // Верстат: стільниця, лещата, молоток і ключ зверху, шухляди й ніжки спереду.
        private static Canvas TopWorkbench()
        {
            var c = new Canvas(64, 40);
            c.Rect(3, 0, 5, 14, C(58, 40, 26));
            c.Rect(56, 0, 5, 14, C(58, 40, 26));
            c.Rect(14, 4, 36, 16, C(100, 72, 46));
            c.Rect(16, 6, 15, 5, C(136, 100, 64));
            c.Rect(33, 6, 15, 5, C(136, 100, 64));
            c.Rect(16, 13, 15, 5, C(136, 100, 64));
            c.Rect(33, 13, 15, 5, C(136, 100, 64));
            for (int y = 8; y < 16; y += 7)
            {
                c.Rect(22, y, 3, 1, Metal);
                c.Rect(39, y, 3, 1, Metal);
            }

            c.Rect(0, 18, 64, 4, C(92, 62, 36));
            c.Rect(0, 22, 64, 8, C(168, 124, 78));
            c.Rect(5, 28, 12, 7, C(70, 90, 110));
            c.Rect(5, 34, 12, 2, Metal);
            c.Rect(17, 30, 4, 2, Metal);
            c.Rect(29, 24, 2, 11, C(120, 80, 44));
            c.Rect(25, 34, 10, 4, MetalDark);
            c.Rect(42, 25, 14, 2, Metal);
            c.Rect(54, 24, 4, 5, Metal);
            return c;
        }

        // Магазинний візок збоку: сітчастий кошик, ручка ліворуч, колеса й покупки всередині.
        private static Canvas TopCart()
        {
            Color steel = C(176, 182, 190);
            Color dark = C(84, 88, 96);
            var c = new Canvas(44, 36);
            c.Rect(1, 30, 9, 3, C(200, 50, 44));
            c.Rect(8, 12, 2, 20, steel);
            c.Rect(12, 28, 28, 6, C(226, 90, 70));
            c.Rect(26, 26, 9, 8, C(240, 200, 90));
            c.Rect(16, 26, 8, 6, C(90, 170, 110));
            c.Rect(10, 12, 32, 2, steel);
            c.Rect(10, 26, 32, 2, steel);
            for (int y = 16; y < 26; y += 4)
                c.Rect(11 + (26 - y) / 4, y, 30 - (26 - y) / 2, 1, dark);
            for (int x = 14; x < 42; x += 5)
                c.Rect(x, 14, 1, 13, dark);
            c.Rect(40, 14, 2, 13, steel);
            c.Rect(10, 8, 30, 2, steel);
            c.Rect(14, 8, 2, 4, steel);
            c.Rect(34, 8, 2, 4, steel);
            c.Ellipse(15, 3, 3, 3, C(24, 24, 28));
            c.Ellipse(35, 3, 3, 3, C(24, 24, 28));
            return c;
        }

        // Гора картонних коробок зі скотчем і стрілками «цей бік угору».
        private static Canvas TopBoxPile()
        {
            var c = new Canvas(44, 40);
            foreach ((int x, int y, int w, int h) in new[] { (0, 0, 22, 20), (22, 0, 22, 20), (10, 20, 24, 20) })
            {
                c.Rect(x, y, w, h, C(92, 62, 34));
                c.Rect(x + 1, y + 1, w - 2, h - 2, C(190, 142, 88));
                c.Rect(x + 1, y + h - 5, w - 2, 4, C(214, 170, 112));
                c.Rect(x + w / 2 - 2, y + 1, 4, h - 2, C(226, 208, 160));
                c.Rect(x + 4, y + 4, 1, 6, C(92, 62, 34));
                c.Rect(x + 3, y + 8, 3, 1, C(92, 62, 34));
            }

            return c;
        }

        // Каса: чорна стрічка з товаром, апарат із зеленим екраном і кнопками.
        private static Canvas TopCheckout()
        {
            var c = new Canvas(64, 40);
            c.Rect(0, 0, 64, 18, C(120, 124, 136));
            c.Rect(0, 16, 64, 2, C(150, 154, 166));
            c.Rect(2, 18, 60, 12, C(70, 72, 82));
            c.Rect(3, 20, 38, 8, C(26, 26, 30));
            for (int x = 6; x < 40; x += 6)
                c.Rect(x, 20, 1, 8, C(62, 62, 68));
            c.Rect(8, 24, 7, 7, C(220, 80, 66));
            c.Rect(22, 24, 6, 9, C(90, 170, 200));
            c.Rect(44, 18, 18, 10, C(46, 50, 60));
            c.Rect(45, 28, 16, 12, C(32, 36, 44));
            c.Rect(47, 31, 12, 7, C(120, 230, 160));
            for (int x = 46; x < 60; x += 4)
                c.Rect(x, 22, 2, 2, C(210, 210, 200));
            return c;
        }

        // Полиця магазину: дерев'яні полички з рядами банок і пляшок.
        private static Canvas TopShelf()
        {
            Color[] goods = { C(214, 70, 60), C(70, 150, 210), C(236, 196, 80), C(90, 180, 110) };
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 32, 32, C(62, 44, 32));
            c.Rect(2, 0, 28, 32, C(96, 72, 52));
            for (int row = 0; row < 2; row++)
            {
                int y = row * 16;
                c.Rect(0, y, 32, 3, C(150, 112, 74));
                for (int i = 0; i < 4; i++)
                {
                    int x = 3 + i * 7;
                    Color good = goods[(i + row * 2) % goods.Length];
                    c.Rect(x, y + 3, 5, 9, good);
                    c.Rect(x + 1, y + 12, 3, 2, good);
                    c.Rect(x, y + 6, 5, 2, C(240, 234, 216));
                }
            }

            return c;
        }

        // Армійський ящик із набоями: зачинений, із ручкою, замком і малюнком патрона.
        private static Canvas TopAmmoBox()
        {
            Color olive = C(84, 104, 62);
            Color dark = C(44, 56, 34);
            Color brass = C(220, 180, 80);
            var c = new Canvas(36, 28);
            c.Rect(0, 0, 36, 20, dark);
            c.Rect(1, 1, 34, 18, olive);
            c.Rect(0, 18, 36, 6, dark);
            c.Rect(1, 19, 34, 4, C(112, 134, 82));
            c.Rect(13, 24, 10, 2, MetalDark);
            c.Rect(13, 24, 2, 4, MetalDark);
            c.Rect(21, 24, 2, 4, MetalDark);
            c.Rect(16, 15, 4, 5, Metal);
            for (int x = 6; x < 30; x += 9)
            {
                c.Rect(x, 4, 4, 7, brass);
                c.Rect(x + 1, 11, 2, 2, C(170, 110, 60));
            }

            return c;
        }

        // Той самий ящик відкритий і порожній: кришка відкинута назад.
        private static Canvas TopAmmoBoxOpen()
        {
            Color olive = C(84, 104, 62);
            Color dark = C(44, 56, 34);
            var c = new Canvas(36, 28);
            c.Rect(0, 0, 36, 16, dark);
            c.Rect(1, 1, 34, 14, olive);
            c.Rect(3, 10, 30, 6, C(18, 22, 16));
            c.Rect(0, 16, 36, 12, dark);
            c.Rect(1, 17, 34, 10, C(112, 134, 82));
            c.Rect(16, 19, 4, 3, Metal);
            return c;
        }

        // Мішень для стрільби на дерев'яній підставці, з дірками від куль.
        private static Canvas TopTarget()
        {
            var c = new Canvas(32, 40);
            c.Rect(8, 0, 3, 12, C(92, 62, 36));
            c.Rect(21, 0, 3, 12, C(92, 62, 36));
            c.Rect(4, 0, 24, 2, C(70, 48, 28));
            c.Ellipse(16, 24, 15, 15, C(236, 230, 214));
            c.Ellipse(16, 24, 11, 11, C(190, 44, 38));
            c.Ellipse(16, 24, 7, 7, C(236, 230, 214));
            c.Ellipse(16, 24, 3, 3, C(190, 44, 38));
            c.Rect(10, 30, 2, 2, C(20, 20, 22));
            c.Rect(21, 20, 2, 2, C(20, 20, 22));
            return c;
        }

        // Стійка зі зброєю: дві рушниці й сокира на дерев'яному щиті.
        private static Canvas TopGunRack()
        {
            Color steel = C(58, 60, 68);
            Color stock = C(150, 96, 52);
            var c = new Canvas(64, 36);
            c.Rect(0, 0, 64, 36, C(62, 42, 28));
            c.Rect(2, 2, 60, 32, C(112, 78, 50));
            c.Rect(2, 30, 60, 2, C(84, 58, 36));
            for (int x = 8; x < 40; x += 18)
            {
                c.Rect(x, 16, 3, 16, steel);
                c.Rect(x - 1, 5, 6, 12, stock);
                c.Rect(x + 3, 16, 2, 4, steel);
            }

            c.Rect(50, 4, 3, 26, stock);
            c.Rect(44, 22, 13, 8, C(190, 44, 38));
            c.Rect(42, 24, 3, 5, Metal);
            return c;
        }

        private static Canvas Stain(int width, int height, Color color)
        {
            var c = new Canvas(width, height);
            c.Ellipse(width / 2, height / 2, width / 2 - 4, height / 2 - 4, color);
            c.Ellipse(width / 3, height / 3, width / 5, height / 5, color);
            c.Ellipse(width * 2 / 3, height * 2 / 3, width / 6, height / 6, color);
            return c;
        }

        private static Canvas DecalCrack()
        {
            var c = new Canvas(40, 40);
            Color crack = new Color(0f, 0f, 0f, 0.55f);
            int y = 4;
            for (int x = 3; x < 37; x += 3)
            {
                c.Rect(x, y, 3, 1, crack);
                c.Rect(x + 2, y, 1, 3, crack);
                y += x % 2 == 0 ? 4 : 2;
            }

            c.Rect(20, 8, 1, 10, crack);
            c.Rect(20, 8, 8, 1, crack);
            return c;
        }

        private static Canvas DecalCable()
        {
            var c = new Canvas(48, 12);
            for (int x = 0; x < 48; x += 4)
                c.Rect(x, 5 + (x / 4 % 3 == 0 ? 2 : x / 4 % 3 == 1 ? 0 : -2), 5, 2, C(22, 22, 26));
            c.Rect(44, 3, 4, 6, C(180, 120, 50));
            return c;
        }

        private static Canvas DecalPaper()
        {
            var c = new Canvas(16, 14);
            Color line = C(130, 126, 116);
            c.Rect(1, 1, 14, 12, C(198, 192, 176));
            c.Rect(3, 9, 10, 1, line);
            c.Rect(3, 6, 8, 1, line);
            c.Rect(3, 3, 9, 1, line);
            return c;
        }

        private static Canvas DecalCan()
        {
            var c = new Canvas(14, 10);
            c.Rect(1, 2, 10, 6, C(170, 60, 54));
            c.Rect(3, 4, 6, 2, C(222, 214, 196));
            c.Rect(11, 3, 2, 4, Metal);
            return c;
        }

        private static Canvas DecalShells()
        {
            var c = new Canvas(20, 20);
            Color brass = C(206, 168, 76);
            foreach ((int x, int y) in new[] { (2, 4), (8, 12), (13, 6), (14, 15), (3, 16) })
            {
                c.Rect(x, y, 4, 2, brass);
                c.Rect(x + 3, y, 1, 2, C(120, 90, 40));
            }

            return c;
        }

        // Рамка 12 на 12 з обрізаними кутами: чорний контур, кольорова смуга й заливка всередині.
        private static Canvas UiFrame(Color edge, Color inner)
        {
            var c = new Canvas(12, 12);
            c.Rect(1, 0, 10, 12, C(10, 8, 8));
            c.Rect(0, 1, 12, 10, C(10, 8, 8));
            c.Rect(1, 1, 10, 10, edge);
            c.Rect(2, 2, 8, 8, inner);
            return c;
        }

        private static Canvas UiIconFlame()
        {
            var c = new Canvas(16, 16);
            c.Ellipse(8, 5, 5, 5, C(240, 130, 40));
            c.Rect(6, 9, 5, 3, C(240, 130, 40));
            c.Rect(7, 12, 3, 3, C(240, 130, 40));
            c.Ellipse(8, 4, 3, 3, C(255, 220, 110));
            return c;
        }

        private static Canvas UiIconHeat()
        {
            var c = new Canvas(16, 16);
            Color glass = C(220, 226, 236);
            c.Rect(6, 5, 4, 10, glass);
            c.Ellipse(8, 4, 4, 4, glass);
            c.Rect(7, 5, 2, 8, C(90, 170, 240));
            c.Ellipse(8, 4, 2, 2, C(90, 170, 240));
            return c;
        }

        private static Canvas UiIconPower()
        {
            var c = new Canvas(16, 16);
            Color bolt = C(255, 216, 70);
            c.Rect(8, 11, 4, 4, bolt);
            c.Rect(6, 8, 5, 3, bolt);
            c.Rect(4, 6, 8, 2, bolt);
            c.Rect(6, 3, 4, 3, bolt);
            c.Rect(5, 0, 3, 3, bolt);
            return c;
        }

        private static Canvas UiIconFear()
        {
            var c = new Canvas(16, 16);
            c.Ellipse(8, 8, 7, 5, C(214, 196, 232));
            c.Ellipse(8, 8, 3, 3, C(120, 60, 170));
            c.Ellipse(8, 8, 1, 1, C(20, 10, 30));
            return c;
        }

        private static Canvas UiIconHealth()
        {
            var c = new Canvas(16, 16);
            Color heart = C(220, 60, 60);
            c.Ellipse(5, 10, 3, 3, heart);
            c.Ellipse(11, 10, 3, 3, heart);
            c.Rect(3, 6, 10, 4, heart);
            c.Rect(5, 3, 6, 3, heart);
            c.Rect(7, 1, 2, 2, heart);
            return c;
        }

        private static Canvas UiArrow()
        {
            var c = new Canvas(9, 8);
            Color arrow = Color.white;
            c.Rect(3, 0, 3, 4, arrow);
            c.Rect(1, 3, 7, 2, arrow);
            c.Rect(2, 5, 5, 1, arrow);
            c.Rect(3, 6, 3, 1, arrow);
            c.Rect(4, 7, 1, 1, arrow);
            return c;
        }

        private static Canvas UiTrolley()
        {
            var c = new Canvas(24, 12);
            c.Rect(1, 2, 22, 7, C(170, 44, 40));
            c.Rect(1, 5, 22, 1, C(226, 190, 110));
            c.Rect(3, 6, 4, 3, C(190, 220, 240));
            c.Rect(9, 6, 4, 3, C(190, 220, 240));
            c.Rect(15, 6, 4, 3, C(190, 220, 240));
            c.Ellipse(6, 2, 2, 2, C(26, 26, 30));
            c.Ellipse(18, 2, 2, 2, C(26, 26, 30));
            c.Rect(8, 9, 1, 3, Metal);
            c.Rect(14, 9, 1, 3, Metal);
            return c;
        }

        private static Canvas UiStopMark()
        {
            var c = new Canvas(12, 12);
            c.Ellipse(6, 6, 5, 5, C(26, 22, 22));
            c.Ellipse(6, 6, 4, 4, C(240, 200, 110));
            c.Ellipse(6, 6, 2, 2, C(26, 22, 22));
            return c;
        }

        private static Canvas UiDial()
        {
            const int size = 48;
            const int center = size / 2;
            var c = new Canvas(size, size);
            c.Ellipse(center, center, 23, 23, C(10, 8, 8));
            c.Ellipse(center, center, 22, 22, C(150, 112, 70));
            c.Ellipse(center, center, 20, 20, C(18, 15, 16));

            // Шкала йде за годинниковою стрілкою від -120° до +120° відносно верху й червоніє до кінця.
            for (float t = 0f; t <= 1f; t += 0.01f)
            {
                float angle = (120f - t * 240f + 90f) * Mathf.Deg2Rad;
                Color color = Color.Lerp(C(90, 190, 100), Color.Lerp(C(235, 190, 70), C(220, 60, 50), t), t);
                for (int radius = 16; radius <= 18; radius++)
                    c.Set(center + Mathf.RoundToInt(Mathf.Cos(angle) * radius), center + Mathf.RoundToInt(Mathf.Sin(angle) * radius), color);
            }

            for (int tick = 0; tick <= 6; tick++)
            {
                float angle = (120f - tick * 40f + 90f) * Mathf.Deg2Rad;
                c.Set(center + Mathf.RoundToInt(Mathf.Cos(angle) * 14f), center + Mathf.RoundToInt(Mathf.Sin(angle) * 14f), C(240, 230, 210));
            }

            c.Ellipse(center, center, 2, 2, C(200, 190, 170));
            return c;
        }

        private static Canvas KeyE()
        {
            Color frame = C(30, 26, 30);
            Color key = C(240, 228, 200);
            var c = new Canvas(13, 13);
            c.Rect(1, 0, 11, 13, frame);
            c.Rect(0, 1, 13, 11, frame);
            c.Rect(1, 2, 11, 10, key);
            c.Rect(1, 1, 11, 1, C(170, 156, 130));
            c.Rect(4, 3, 2, 7, frame);
            c.Rect(6, 3, 3, 1, frame);
            c.Rect(6, 6, 2, 1, frame);
            c.Rect(6, 9, 3, 1, frame);
            return c;
        }

        private static Canvas FlameBowl()
        {
            Color body = C(120, 104, 90);
            Color shade = C(84, 72, 62);
            var c = new Canvas(14, 6);
            c.Rect(0, 5, 14, 1, C(158, 140, 120));
            c.Rect(1, 2, 12, 3, body);
            c.Rect(1, 2, 3, 3, shade);
            c.Rect(3, 1, 8, 1, shade);
            c.Rect(4, 0, 6, 1, shade);
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

        // Депо: сірий бетон зі швами й плямами мастила.
        private static Canvas TopFloorDepot()
        {
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 32, 32, C(112, 114, 116));
            c.Rect(0, 0, 32, 1, C(78, 80, 84));
            c.Rect(0, 0, 1, 32, C(78, 80, 84));
            c.Rect(6, 10, 4, 2, C(92, 94, 98));
            c.Rect(20, 22, 5, 3, C(86, 86, 88));
            c.Rect(25, 6, 2, 2, C(132, 134, 136));
            c.Rect(12, 26, 2, 1, C(132, 134, 136));
            return c;
        }

        // Депо: профнастил із жовто-чорною смугою небезпеки.
        private static Canvas TopWallDepot()
        {
            Color steel = C(84, 96, 108);
            Color groove = C(58, 68, 78);
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 32, 32, steel);
            for (int x = 3; x < 32; x += 6)
                c.Rect(x, 0, 2, 32, groove);
            c.Rect(0, 12, 32, 8, C(226, 180, 50));
            for (int x = 0; x < 32; x += 8)
                c.Rect(x, 12, 4, 8, C(30, 28, 30));
            return c;
        }

        // Супермаркет: шахова плитка, світла й бірюзова.
        private static Canvas TopFloorMarket()
        {
            Color light = C(206, 208, 198);
            Color tint = C(112, 170, 164);
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 16, 16, light);
            c.Rect(16, 16, 16, 16, light);
            c.Rect(16, 0, 16, 16, tint);
            c.Rect(0, 16, 16, 16, tint);
            c.Rect(0, 15, 32, 1, C(150, 156, 150));
            c.Rect(15, 0, 1, 32, C(150, 156, 150));
            return c;
        }

        // Супермаркет: біла кахельна стіна з кольоровою смугою.
        private static Canvas TopWallMarket()
        {
            Color joint = C(176, 178, 174);
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 32, 32, C(226, 226, 218));
            c.Rect(0, 0, 32, 1, joint);
            c.Rect(0, 8, 32, 1, joint);
            c.Rect(0, 24, 32, 1, joint);
            c.Rect(0, 0, 1, 32, joint);
            c.Rect(16, 0, 1, 32, joint);
            c.Rect(0, 12, 32, 8, C(44, 134, 136));
            return c;
        }

        // Магазин зброї: темні дерев'яні дошки.
        private static Canvas TopFloorGunShop()
        {
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 32, 32, C(92, 60, 40));
            c.Rect(0, 16, 32, 16, C(80, 52, 34));
            c.Rect(0, 0, 32, 1, C(46, 30, 20));
            c.Rect(0, 16, 32, 1, C(46, 30, 20));
            c.Rect(10, 0, 1, 16, C(46, 30, 20));
            c.Rect(24, 16, 1, 16, C(46, 30, 20));
            c.Rect(18, 7, 6, 1, C(70, 46, 30));
            c.Rect(4, 25, 7, 1, C(66, 42, 26));
            return c;
        }

        // Магазин зброї: темно-червона цегла.
        private static Canvas TopWallGunShop()
        {
            Color mortar = C(46, 30, 30);
            var c = new Canvas(32, 32);
            c.Rect(0, 0, 32, 32, C(122, 48, 40));
            c.Rect(0, 0, 32, 2, mortar);
            c.Rect(0, 15, 32, 2, mortar);
            c.Rect(0, 30, 32, 2, mortar);
            c.Rect(10, 2, 2, 13, mortar);
            c.Rect(26, 2, 2, 13, mortar);
            c.Rect(2, 17, 2, 13, mortar);
            c.Rect(18, 17, 2, 13, mortar);
            return c;
        }

        // Дерев'яний ящик із хрестовими планками.
        private static Canvas TopCrate()
        {
            Color plank = C(166, 118, 70);
            Color edge = C(92, 62, 36);
            var c = new Canvas(36, 36);
            c.Rect(0, 0, 36, 36, edge);
            c.Rect(2, 2, 32, 32, plank);
            for (int y = 9; y < 34; y += 8)
                c.Rect(2, y, 32, 1, C(130, 90, 52));
            for (int i = 0; i < 28; i++)
            {
                c.Rect(4 + i, 4 + i, 3, 2, edge);
                c.Rect(4 + i, 30 - i, 3, 2, edge);
            }

            c.Rect(0, 0, 36, 4, edge);
            c.Rect(0, 32, 36, 4, edge);
            c.Rect(0, 0, 4, 36, edge);
            c.Rect(32, 0, 4, 36, edge);
            return c;
        }

        // Прилавок магазину: скляна вітрина зверху, дерево спереду й табличка з ціною.
        private static Canvas TopCounter()
        {
            var c = new Canvas(48, 40);
            c.Rect(0, 0, 48, 24, C(84, 56, 34));
            c.Rect(2, 2, 44, 20, C(132, 92, 56));
            for (int x = 10; x < 44; x += 8)
                c.Rect(x, 2, 1, 20, C(100, 68, 40));
            c.Rect(0, 22, 48, 3, C(190, 150, 96));
            c.Rect(3, 25, 42, 15, C(60, 86, 100));
            c.Rect(5, 27, 38, 11, C(120, 170, 190));
            c.Rect(8, 30, 20, 3, MetalDark);
            c.Rect(26, 29, 5, 6, Metal);
            c.Rect(14, 6, 20, 10, C(236, 226, 190));
            c.Rect(17, 9, 14, 1, C(90, 70, 50));
            c.Rect(17, 12, 10, 1, C(90, 70, 50));
            return c;
        }

        // Зарядна шафа: металевий корпус, блискавка, шкала й кабель.
        private static Canvas TopCharger()
        {
            var c = new Canvas(36, 44);
            c.Rect(2, 0, 32, 44, C(44, 60, 84));
            c.Rect(4, 2, 28, 40, C(70, 96, 130));
            c.Rect(7, 30, 22, 9, C(20, 28, 40));
            for (int x = 9; x < 27; x += 4)
                c.Rect(x, 32, 2, 5, C(90, 220, 240));
            Color bolt = C(250, 216, 70);
            c.Rect(18, 22, 6, 5, bolt);
            c.Rect(14, 17, 9, 5, bolt);
            c.Rect(16, 12, 7, 5, bolt);
            c.Rect(13, 6, 6, 6, bolt);
            c.Rect(0, 4, 2, 8, C(30, 30, 34));
            return c;
        }

        // Двері тролейбуса, куди треба повернутися: червоні стулки зі склом і зелена стрілка ліворуч.
        private static Canvas TopDoor()
        {
            Color panel = C(150, 48, 42);
            Color arrow = C(90, 240, 130);
            var c = new Canvas(32, 64);
            c.Rect(0, 0, 32, 64, C(50, 44, 46));
            c.Rect(2, 2, 13, 60, panel);
            c.Rect(17, 2, 13, 60, panel);
            c.Rect(4, 34, 9, 24, C(44, 60, 84));
            c.Rect(19, 34, 9, 24, C(44, 60, 84));
            c.Rect(12, 24, 2, 7, Metal);
            c.Rect(18, 24, 2, 7, Metal);
            c.Rect(8, 11, 18, 4, arrow);
            for (int i = 0; i < 6; i++)
                c.Rect(8 - i, 8 + i, 1, 10 - i * 2, arrow);
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
