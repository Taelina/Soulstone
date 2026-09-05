using Soulstone.Localizations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Soulstone.Managers
{
    internal class LocalizationManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static LocalizationManager? instance;
        public static LocalizationManager Instance => instance ??= new LocalizationManager();

        private Configuration? configuration;
        private readonly Dictionary<Language, Localization> localizedLanguages = new();
        private string? externalLocDirectory;

        public Dictionary<Language, Localization> LocalizedLanguages => localizedLanguages;

        public LocalizationManager()
        {
            LoadEmbeddedLanguages();
        }

        public void InitLoc(Plugin? plugin = null)
        {
            if (plugin != null)
            {
                configuration = plugin.Configuration;
                if (!string.IsNullOrEmpty(Plugin.dataLocation))
                {
                    string locDir = Path.Combine(Plugin.dataLocation, "Localizations");
                    if (Directory.Exists(locDir))
                    {
                        externalLocDirectory = locDir;
                        LoadFromDirectory(locDir);
                    }
                }
            }

            if (localizedLanguages.Count == 0)
            {
                LoadEmbeddedLanguages();
            }
        }

        public void InitLoc(Configuration? config)
        {
            configuration = config;
            if (localizedLanguages.Count == 0)
            {
                LoadEmbeddedLanguages();
            }
        }

        public void Reload()
        {
            LoadEmbeddedLanguages();
            if (!string.IsNullOrEmpty(externalLocDirectory) && Directory.Exists(externalLocDirectory))
            {
                LoadFromDirectory(externalLocDirectory);
            }
        }

        public void LoadEmbeddedLanguages()
        {
            var assembly = typeof(LocalizationManager).Assembly;
            var resourceNames = assembly.GetManifestResourceNames();

            foreach (Language lang in Enum.GetValues<Language>())
            {
                string langCode = lang.GetCode();
                string resourceSuffix = $".{langCode}.json";

                string? match = Array.Find(resourceNames, r => r.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    try
                    {
                        using Stream? stream = assembly.GetManifestResourceStream(match);
                        if (stream != null)
                        {
                            using StreamReader reader = new(stream, Encoding.UTF8);
                            string json = reader.ReadToEnd();
                            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
                            if (dict != null)
                            {
                                localizedLanguages[lang] = new Localization(lang, dict);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Error(ex, $"Failed to load embedded localization resource: {match}");
                    }
                }
                else if (!localizedLanguages.ContainsKey(lang))
                {
                    localizedLanguages[lang] = new Localization(lang);
                }
            }
        }

        public void LoadFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            foreach (var filePath in Directory.GetFiles(directoryPath, "*.json"))
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    Language lang = LanguageExtensions.FromCode(fileName);

                    string json = File.ReadAllText(filePath, Encoding.UTF8);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
                    if (dict != null)
                    {
                        if (localizedLanguages.TryGetValue(lang, out var existing))
                        {
                            foreach (var kvp in dict)
                            {
                                existing.LocalizedStrings[kvp.Key] = kvp.Value;
                            }
                        }
                        else
                        {
                            localizedLanguages[lang] = new Localization(lang, dict);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log?.Error(ex, $"Failed to load external localization file: {filePath}");
                }
            }
        }

        public string GetLocalizedString(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return string.Empty;

            Language currentLang = configuration?.Language ?? Language.Français;

            // 1. Primary language lookup
            if (localizedLanguages.TryGetValue(currentLang, out var loc) && loc.TryGetString(fieldName, out var value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            // 2. Fallback to English if primary language is not English
            if (currentLang != Language.English && localizedLanguages.TryGetValue(Language.English, out var fallbackLoc) && fallbackLoc.TryGetString(fieldName, out var fallbackVal) && !string.IsNullOrEmpty(fallbackVal))
            {
                return fallbackVal;
            }

            // 3. Fallback to French if English was primary but missing
            if (currentLang == Language.English && localizedLanguages.TryGetValue(Language.Français, out var frenchLoc) && frenchLoc.TryGetString(fieldName, out var frenchVal) && !string.IsNullOrEmpty(frenchVal))
            {
                return frenchVal;
            }

            // 4. Return the key itself as final fallback
            return fieldName;
        }

        public string GetLocalizedString(string fieldName, params object[] args)
        {
            string format = GetLocalizedString(fieldName);
            if (args == null || args.Length == 0) return format;

            try
            {
                return string.Format(format, args);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to format localized string for key '{fieldName}' with format '{format}'");
                return format;
            }
        }
    }
}
