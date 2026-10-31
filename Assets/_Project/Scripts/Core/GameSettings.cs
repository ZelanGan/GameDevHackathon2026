using System;
using UnityEngine;

namespace NightTrolleybus.Core
{
    public enum GameLanguage
    {
        Ukrainian,
        English
    }

    /// <summary>Налаштування гравця. Зберігаються окремо від партії, тому переживають нову гру.</summary>
    public static class GameSettings
    {
        private const string VolumeKey = "settings.volume";
        private const string LanguageKey = "settings.language";

        public static event Action LanguageChanged;

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set
            {
                float volume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(VolumeKey, volume);
                AudioListener.volume = volume;
            }
        }

        public static GameLanguage Language
        {
            get
            {
                // Раніше збережена мова могла зникнути з гри, тоді повертаємося до основної.
                var language = (GameLanguage)PlayerPrefs.GetInt(LanguageKey, (int)GameLanguage.Ukrainian);
                return Enum.IsDefined(typeof(GameLanguage), language) ? language : GameLanguage.Ukrainian;
            }
            set
            {
                PlayerPrefs.SetInt(LanguageKey, (int)value);
                LanguageChanged?.Invoke();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // Статична подія може пережити запуск, якщо вимкнено перезавантаження домену.
            LanguageChanged = null;
            AudioListener.volume = Volume;
        }
    }
}
