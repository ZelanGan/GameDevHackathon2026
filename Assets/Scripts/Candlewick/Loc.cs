using UnityEngine;

namespace Candlewick
{
    /// <summary>Interface language. Strings sit next to their use as English/Ukrainian pairs.</summary>
    public static class Loc
    {
        const string PrefKey = "candlewick.language";
        const string English = "en", Ukrainian = "uk";

        static string current;

        static string Current
        {
            get
            {
                if (current == null)
                    current = PlayerPrefs.GetString(PrefKey, Application.systemLanguage == SystemLanguage.Ukrainian ? Ukrainian : English);
                return current;
            }
        }

        public static string T(string en, string uk) => Current == Ukrainian ? uk : en;

        public static void Toggle()
        {
            current = Current == Ukrainian ? English : Ukrainian;
            PlayerPrefs.SetString(PrefKey, current);
            PlayerPrefs.Save();
        }
    }
}
