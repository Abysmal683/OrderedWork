#nullable disable
using Verse;

namespace OrderedWork.Localization
{
    /// <summary>Keyed UI text with an English fallback for missing translations.</summary>
    public static class OrderedWorkText
    {
        public static string T(string key, string fallback)
        {
            string translated = key.Translate();
            return string.IsNullOrEmpty(translated) || translated == key ? fallback : translated;
        }
    }
}