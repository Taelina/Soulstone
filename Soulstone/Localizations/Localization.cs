using System;
using System.Collections.Generic;

namespace Soulstone.Localizations
{
    public enum Language
    {
        Français = 0,
        English = 1
    }

    public static class LanguageExtensions
    {
        public static string GetCode(this Language language) => language switch
        {
            Language.Français => "fr",
            Language.English => "en",
            _ => "en"
        };

        public static string GetDisplayName(this Language language) => language switch
        {
            Language.Français => "Français",
            Language.English => "English",
            _ => language.ToString()
        };

        public static Language FromCode(string? code) => (code?.Trim().ToLowerInvariant()) switch
        {
            "fr" or "français" or "francais" or "french" => Language.Français,
            "en" or "english" or "us" or "gb" => Language.English,
            _ => Language.English
        };
    }

    internal class Localization
    {
        public Language Language { get; set; } = Language.English;
        public Dictionary<string, string> LocalizedStrings { get; set; } = new(StringComparer.Ordinal);

        public Localization() { }

        public Localization(Language language, Dictionary<string, string>? strings = null)
        {
            Language = language;
            LocalizedStrings = strings ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public bool TryGetString(string key, out string? value)
        {
            return LocalizedStrings.TryGetValue(key, out value);
        }
    }
}
