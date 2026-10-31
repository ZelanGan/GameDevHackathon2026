using System.Collections.Generic;
using NightTrolleybus.Core;

namespace NightTrolleybus.UI
{
    /// <summary>Тексти меню двома мовами. Порядок перекладів: українська, англійська.</summary>
    public static class MenuText
    {
        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            { "title", new[] { "НІЧНИЙ ТРОЛЕЙБУС", "NIGHT TROLLEYBUS" } },
            { "subtitle", new[] { "Довези Вогника до кінцевої", "Carry the Flame to the last stop" } },
            { "start", new[] { "Почати", "Start" } },
            { "continue", new[] { "Продовжити", "Continue" } },
            { "settings", new[] { "Налаштування", "Settings" } },
            { "quit", new[] { "Вийти", "Quit" } },
            { "pause", new[] { "ПАУЗА", "PAUSE" } },
            { "exitToMenu", new[] { "Вийти в головне меню", "Exit to main menu" } },
            { "volume", new[] { "Звук", "Sound" } },
            { "language", new[] { "Мова", "Language" } },
            { "back", new[] { "Назад", "Back" } },
            { "confirmNew", new[] { "Почати нову гру?\nПоточне збереження буде втрачено.", "Start a new game?\nYour current save will be lost." } },
            { "confirmExit", new[] { "Вийти в головне меню?\nПрогрес від останнього входу на мапу буде втрачено.", "Exit to the main menu?\nProgress since you last saw the map will be lost." } },
            { "yes", new[] { "Так", "Yes" } },
            { "no", new[] { "Ні", "No" } },
            { "languageNote", new[] { "Поки що мова діє лише в меню", "For now the language applies to menus only" } },
        };

        private static readonly string[] LanguageNames = { "Українська", "English" };

        public static string Get(string key) => Table[key][(int)GameSettings.Language];

        public static string CurrentLanguageName => LanguageNames[(int)GameSettings.Language];
    }
}
