using System.Collections.Generic;
using NightTrolleybus.Core;

namespace NightTrolleybus.UI
{
    /// <summary>Тексти меню двома мовами. Порядок перекладів: українська, англійська.</summary>
    public static class MenuText
    {
        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            { "title", new[] { "LAST LIGHT", "LAST LIGHT" } },
            { "subtitle", new[] { "Довези Вогника до кінцевої", "Carry the Flame to the last stop" } },
            { "start", new[] { "Почати", "Start" } },
            { "continue", new[] { "Продовжити", "Continue" } },
            { "settings", new[] { "Налаштування", "Settings" } },
            { "quit", new[] { "Вийти", "Quit" } },
            { "pause", new[] { "ПАУЗА", "PAUSE" } },
            { "exitToMenu", new[] { "Вийти в головне меню", "Exit to main menu" } },
            { "controls", new[] { "Керування", "Controls" } },
            {
                "controlsList", new[]
                {
                    "A / D: хода в салоні\nW / A / S / D: рух на зупинці\nW / S за кермом: швидкість\nМиша: погляд на зупинці\nЛКМ: удар\nE: дія\nF: Спалах\nR: Підсвітити\nG: зігріти салон\nQ: використати предмет\nКолесо миші, Tab, 1-6: вибір предмета\nEsc: пауза",
                    "A / D: walk in the bus\nW / A / S / D: move at a stop\nW / S at the wheel: speed\nMouse: look at a stop\nLMB: strike\nE: interact\nF: Flash\nR: Reveal\nG: warm the cabin\nQ: use item\nMouse wheel, Tab, 1-6: select item\nEsc: pause"
                }
            },
            { "volume", new[] { "Звук", "Sound" } },
            { "musicVolume", new[] { "Музика", "Music" } },
            { "language", new[] { "Мова", "Language" } },
            { "back", new[] { "Назад", "Back" } },
            { "confirmNew", new[] { "Почати нову гру?\nПоточне збереження буде втрачено.", "Start a new game?\nYour current save will be lost." } },
            { "confirmExit", new[] { "Вийти в головне меню?\nПрогрес від останнього входу на мапу буде втрачено.", "Exit to the main menu?\nProgress since you last saw the map will be lost." } },
            { "skip", new[] { "Пробіл: пропустити", "Space: skip" } },
            { "yes", new[] { "Так", "Yes" } },
            { "no", new[] { "Ні", "No" } },
        };

        private static readonly string[] LanguageNames = { "Українська", "English" };

        public static string Get(string key) => Table[key][(int)GameSettings.Language];

        public static string CurrentLanguageName => LanguageNames[(int)GameSettings.Language];
    }
}
