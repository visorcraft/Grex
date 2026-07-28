using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Grex.Models;

namespace Grex.Services
{
    public enum ThemePreference
    {
        System,
        Light,
        Dark,
        GentleGecko,
        BlackKnight,
        Diamond,
        Dreams,
        Paranoid,
        RedVelvet,
        Subspace,
        Tiefling,
        Vibes
    }

    public class DefaultSettings
    {
        public bool IsRegexSearch { get; set; } = false;
        public bool IsFilesSearch { get; set; } = false;
        public bool RespectGitignore { get; set; } = false;
        public bool SearchCaseSensitive { get; set; } = false;
        public bool IncludeSystemFiles { get; set; } = false;
        public bool IncludeSubfolders { get; set; } = true;
        public bool IncludeHiddenItems { get; set; } = false;
        public bool IncludeBinaryFiles { get; set; } = false;
        public bool IncludeSymbolicLinks { get; set; } = false;
        public bool UseWindowsSearchIndex { get; set; } = false;
        public bool EnableDockerSearch { get; set; } = false;
        public Models.SizeUnit SizeUnit { get; set; } = Models.SizeUnit.KB;
        public ThemePreference ThemePreference { get; set; } = ThemePreference.GentleGecko;
        public string UILanguage { get; set; } = "en-US"; // Default to English (United States)
        
        // Culture-aware string comparison settings
        public Models.StringComparisonMode StringComparisonMode { get; set; } = Models.StringComparisonMode.Ordinal;
        public Models.UnicodeNormalizationMode UnicodeNormalizationMode { get; set; } = Models.UnicodeNormalizationMode.None;
        public bool DiacriticSensitive { get; set; } = true;
        public string Culture { get; set; } = CultureInfo.CurrentCulture.Name;
        
        // Default filter values
        public string DefaultMatchFiles { get; set; } = string.Empty;
        public string DefaultExcludeDirs { get; set; } = string.Empty;
        
        // Content table column visibility
        public bool ContentLineColumnVisible { get; set; } = true;
        public bool ContentColumnColumnVisible { get; set; } = true;
        public bool ContentPathColumnVisible { get; set; } = true;
        
        // Files table column visibility
        public bool FilesSizeColumnVisible { get; set; } = true;
        public bool FilesMatchesColumnVisible { get; set; } = true;
        public bool FilesPathColumnVisible { get; set; } = true;
        public bool FilesExtColumnVisible { get; set; } = true;
        public bool FilesEncodingColumnVisible { get; set; } = true;
        public bool FilesDateModifiedColumnVisible { get; set; } = true;
        
        // Window position and size
        public int? WindowX { get; set; } = null;
        public int? WindowY { get; set; } = null;
        public int? WindowWidth { get; set; } = 1100;
	public int? WindowHeight { get; set; } = 700;

        // Context preview settings
        public int ContextPreviewLinesBefore { get; set; } = 5;
        public int ContextPreviewLinesAfter { get; set; } = 5;

        // Future AI search settings
        public string AiSearchEndpoint { get; set; } = "https://api.openai.com/v1";
        public string AiSearchApiKey { get; set; } = string.Empty;
        public string AiSearchModel { get; set; } = "gpt-4o-mini";
    }

    public static class SettingsService
    {
        public static event EventHandler<bool>? DockerSearchEnabledChanged;
        
        private static readonly string DefaultSettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Grex",
            "settings.json");

        private static string? _settingsFilePathOverride;

        private static DefaultSettings? _cachedSettings;
        private static readonly object _lock = new object();

        private static string GetSettingsFilePath()
        {
            if (!string.IsNullOrWhiteSpace(_settingsFilePathOverride))
            {
                return _settingsFilePathOverride!;
            }
            return DefaultSettingsFilePath;
        }

        private static DefaultSettings LoadSettings()
        {
            lock (_lock)
            {
                if (_cachedSettings != null)
                    return _cachedSettings;

                try
                {
                    var settingsFilePath = GetSettingsFilePath();
                    if (File.Exists(settingsFilePath))
                    {
                        var json = File.ReadAllText(settingsFilePath);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                            AllowTrailingCommas = true
                        };
                        _cachedSettings = JsonSerializer.Deserialize<DefaultSettings>(json, options) ?? new DefaultSettings();
                    }
                    else
                    {
                        _cachedSettings = new DefaultSettings();
                    }
                }
                catch
                {
                    _cachedSettings = new DefaultSettings();
                }

                return _cachedSettings;
            }
        }

        private static void SaveSettings(DefaultSettings settings)
        {
            lock (_lock)
            {
                try
                {
                    var settingsFilePath = GetSettingsFilePath();
                    var directory = Path.GetDirectoryName(settingsFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(settingsFilePath, json);
                    _cachedSettings = settings;
                }
                catch
                {
                    // Ignore save errors
                }
            }
        }

        public static DefaultSettings GetDefaultSettings()
        {
            return LoadSettings();
        }

        public static void SetDefaultIsRegexSearch(bool value)
        {
            var settings = LoadSettings();
            settings.IsRegexSearch = value;
            SaveSettings(settings);
        }

        public static void SetDefaultIsFilesSearch(bool value)
        {
            var settings = LoadSettings();
            settings.IsFilesSearch = value;
            SaveSettings(settings);
        }

        public static void SetDefaultRespectGitignore(bool value)
        {
            var settings = LoadSettings();
            settings.RespectGitignore = value;
            SaveSettings(settings);
        }

        public static void SetDefaultSearchCaseSensitive(bool value)
        {
            var settings = LoadSettings();
            settings.SearchCaseSensitive = value;
            SaveSettings(settings);
        }

        public static void SetDefaultIncludeSystemFiles(bool value)
        {
            var settings = LoadSettings();
            settings.IncludeSystemFiles = value;
            SaveSettings(settings);
        }

        public static void SetDefaultIncludeSubfolders(bool value)
        {
            var settings = LoadSettings();
            settings.IncludeSubfolders = value;
            SaveSettings(settings);
        }

        public static void SetDefaultIncludeHiddenItems(bool value)
        {
            var settings = LoadSettings();
            settings.IncludeHiddenItems = value;
            SaveSettings(settings);
        }

        public static void SetDefaultIncludeBinaryFiles(bool value)
        {
            var settings = LoadSettings();
            settings.IncludeBinaryFiles = value;
            SaveSettings(settings);
        }

        public static void SetDefaultIncludeSymbolicLinks(bool value)
        {
            var settings = LoadSettings();
            settings.IncludeSymbolicLinks = value;
            SaveSettings(settings);
        }

        public static void SetDefaultUseWindowsSearchIndex(bool value)
        {
            var settings = LoadSettings();
            settings.UseWindowsSearchIndex = value;
            SaveSettings(settings);
        }

        public static bool GetEnableDockerSearch()
        {
            var settings = LoadSettings();
            return settings.EnableDockerSearch;
        }

        public static void SetEnableDockerSearch(bool value)
        {
            var settings = LoadSettings();
            if (settings.EnableDockerSearch == value)
                return;

            settings.EnableDockerSearch = value;
            SaveSettings(settings);
            DockerSearchEnabledChanged?.Invoke(null, value);
        }

        public static void SetDefaultSizeUnit(Models.SizeUnit value)
        {
            var settings = LoadSettings();
            settings.SizeUnit = value;
            SaveSettings(settings);
        }

        public static void SetDefaultContentLineColumnVisible(bool value)
        {
            var settings = LoadSettings();
            settings.ContentLineColumnVisible = value;
            SaveSettings(settings);
        }

        public static void SetDefaultContentColumnColumnVisible(bool value)
        {
            var settings = LoadSettings();
            settings.ContentColumnColumnVisible = value;
            SaveSettings(settings);
        }

        public static void SetDefaultContentPathColumnVisible(bool value)
        {
            var settings = LoadSettings();
            settings.ContentPathColumnVisible = value;
            SaveSettings(settings);
        }

        public static void SetDefaultFilesSizeColumnVisible(bool value)
        {
            var settings = LoadSettings();
            settings.FilesSizeColumnVisible = value;
            SaveSettings(settings);
        }

        public static void SetDefaultFilesMatchesColumnVisible(bool value)
        {
            var settings = LoadSettings();
            settings.FilesMatchesColumnVisible = value;
            SaveSettings(settings);
        }

        public static void SetDefaultFilesPathColumnVisible(bool value)
        {
            var settings = LoadSettings();
            settings.FilesPathColumnVisible = value;
            SaveSettings(settings);
        }

        public static void SetDefaultFilesExtColumnVisible(bool value)
        {
            var settings = LoadSettings();
            settings.FilesExtColumnVisible = value;
            SaveSettings(settings);
        }

        public static void SetDefaultFilesEncodingColumnVisible(bool value)
        {
            var settings = LoadSettings();
            settings.FilesEncodingColumnVisible = value;
            SaveSettings(settings);
        }

        public static void SetDefaultFilesDateModifiedColumnVisible(bool value)
        {
            var settings = LoadSettings();
            settings.FilesDateModifiedColumnVisible = value;
            SaveSettings(settings);
        }

        public static void SetWindowPosition(int x, int y, int width, int height)
        {
            var settings = LoadSettings();
            settings.WindowX = x;
            settings.WindowY = y;
            settings.WindowWidth = width;
            settings.WindowHeight = height;
            SaveSettings(settings);
        }

        public static (int? x, int? y, int? width, int? height) GetWindowPosition()
        {
            var settings = LoadSettings();
            return (settings.WindowX, settings.WindowY, settings.WindowWidth, settings.WindowHeight);
        }

        public static ThemePreference GetThemePreference()
        {
            var settings = LoadSettings();
            return settings.ThemePreference;
        }

        public static void SetThemePreference(ThemePreference value)
        {
            var settings = LoadSettings();
            settings.ThemePreference = value;
            SaveSettings(settings);
        }

        public static void SetDefaultStringComparisonMode(Models.StringComparisonMode value)
        {
            var settings = LoadSettings();
            settings.StringComparisonMode = value;
            SaveSettings(settings);
        }

        public static void SetDefaultUnicodeNormalizationMode(Models.UnicodeNormalizationMode value)
        {
            var settings = LoadSettings();
            settings.UnicodeNormalizationMode = value;
            SaveSettings(settings);
        }

        public static void SetDefaultDiacriticSensitive(bool value)
        {
            var settings = LoadSettings();
            settings.DiacriticSensitive = value;
            SaveSettings(settings);
        }

        public static void SetDefaultCulture(string value)
        {
            var settings = LoadSettings();
            settings.Culture = value;
            SaveSettings(settings);
        }

        public static void SetDefaultMatchFiles(string value)
        {
            var settings = LoadSettings();
            settings.DefaultMatchFiles = value ?? string.Empty;
            SaveSettings(settings);
        }

        public static void SetDefaultExcludeDirs(string value)
        {
            var settings = LoadSettings();
            settings.DefaultExcludeDirs = value ?? string.Empty;
            SaveSettings(settings);
        }

        public static string GetUILanguage()
        {
            var settings = LoadSettings();
            return settings.UILanguage ?? string.Empty;
        }

        public static void SetUILanguage(string value)
        {
            var settings = LoadSettings();
            settings.UILanguage = value ?? string.Empty;
            SaveSettings(settings);
        }

        public static int GetContextPreviewLinesBefore()
        {
            var settings = LoadSettings();
            return Math.Max(1, Math.Min(20, settings.ContextPreviewLinesBefore));
        }

        public static void SetContextPreviewLinesBefore(int value)
        {
            var settings = LoadSettings();
            settings.ContextPreviewLinesBefore = Math.Max(1, Math.Min(20, value));
            SaveSettings(settings);
        }

        public static int GetContextPreviewLinesAfter()
        {
            var settings = LoadSettings();
            return Math.Max(1, Math.Min(20, settings.ContextPreviewLinesAfter));
        }

        public static void SetContextPreviewLinesAfter(int value)
        {
            var settings = LoadSettings();
            settings.ContextPreviewLinesAfter = Math.Max(1, Math.Min(20, value));
            SaveSettings(settings);
        }

        public static string GetAiSearchEndpoint()
        {
            var settings = LoadSettings();
            return settings.AiSearchEndpoint ?? string.Empty;
        }

        public static void SetAiSearchEndpoint(string value)
        {
            var settings = LoadSettings();
            settings.AiSearchEndpoint = value?.Trim() ?? string.Empty;
            SaveSettings(settings);
        }

        public static string GetAiSearchApiKey()
        {
            var settings = LoadSettings();
            return settings.AiSearchApiKey ?? string.Empty;
        }

        public static void SetAiSearchApiKey(string value)
        {
            var settings = LoadSettings();
            settings.AiSearchApiKey = value ?? string.Empty;
            SaveSettings(settings);
        }

        public static string GetAiSearchModel()
        {
            var settings = LoadSettings();
            return settings.AiSearchModel ?? string.Empty;
        }

        public static void SetAiSearchModel(string value)
        {
            var settings = LoadSettings();
            settings.AiSearchModel = value?.Trim() ?? string.Empty;
            SaveSettings(settings);
        }

        public static void InvalidateCache()
        {
            lock (_lock)
            {
                _cachedSettings = null;
            }
        }

        public static void SetSettingsFilePathOverride(string? customPath)
        {
            lock (_lock)
            {
                _settingsFilePathOverride = customPath;
                _cachedSettings = null;
            }
        }

        /// <summary>
        /// Export current settings as a JSON string.
        /// </summary>
        public static string ExportSettingsAsJson()
        {
            var settings = LoadSettings();
            return JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Import settings from a JSON string, merging with existing settings.
        /// Returns (success, error message if failed).
        /// </summary>
        public static (bool Success, string? ErrorMessage) ImportSettingsFromJson(string json)
        {
            lock (_lock)
            {
                try
                {
                    // First, try to parse the JSON to validate it
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };

                    using var document = JsonDocument.Parse(json, new JsonDocumentOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    });
                    if (document.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        return (false, "Invalid settings file format.");
                    }

                    var importedSettings = JsonSerializer.Deserialize<DefaultSettings>(json, options);
                    if (importedSettings == null)
                    {
                        return (false, "Invalid settings file format.");
                    }

                    var importedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        importedProperties.Add(property.Name);
                    }
                    bool Has(string propertyName) => importedProperties.Contains(propertyName);
                    
                    if ((Has(nameof(DefaultSettings.SizeUnit)) &&
                         !Enum.IsDefined(typeof(Models.SizeUnit), importedSettings.SizeUnit)) ||
                        (Has(nameof(DefaultSettings.ThemePreference)) &&
                         !Enum.IsDefined(typeof(ThemePreference), importedSettings.ThemePreference)) ||
                        (Has(nameof(DefaultSettings.StringComparisonMode)) &&
                         !Enum.IsDefined(typeof(Models.StringComparisonMode), importedSettings.StringComparisonMode)) ||
                        (Has(nameof(DefaultSettings.UnicodeNormalizationMode)) &&
                         !Enum.IsDefined(typeof(Models.UnicodeNormalizationMode), importedSettings.UnicodeNormalizationMode)))
                    {
                        return (false, "Settings file contains an invalid option value.");
                    }
                    
                    // Load current settings (or create new if none exist)
                    var currentSettings = LoadSettings();
                    
                    if (Has(nameof(DefaultSettings.IsRegexSearch))) currentSettings.IsRegexSearch = importedSettings.IsRegexSearch;
                    if (Has(nameof(DefaultSettings.IsFilesSearch))) currentSettings.IsFilesSearch = importedSettings.IsFilesSearch;
                    if (Has(nameof(DefaultSettings.RespectGitignore))) currentSettings.RespectGitignore = importedSettings.RespectGitignore;
                    if (Has(nameof(DefaultSettings.SearchCaseSensitive))) currentSettings.SearchCaseSensitive = importedSettings.SearchCaseSensitive;
                    if (Has(nameof(DefaultSettings.IncludeSystemFiles))) currentSettings.IncludeSystemFiles = importedSettings.IncludeSystemFiles;
                    if (Has(nameof(DefaultSettings.IncludeSubfolders))) currentSettings.IncludeSubfolders = importedSettings.IncludeSubfolders;
                    if (Has(nameof(DefaultSettings.IncludeHiddenItems))) currentSettings.IncludeHiddenItems = importedSettings.IncludeHiddenItems;
                    if (Has(nameof(DefaultSettings.IncludeBinaryFiles))) currentSettings.IncludeBinaryFiles = importedSettings.IncludeBinaryFiles;
                    if (Has(nameof(DefaultSettings.IncludeSymbolicLinks))) currentSettings.IncludeSymbolicLinks = importedSettings.IncludeSymbolicLinks;
                    if (Has(nameof(DefaultSettings.UseWindowsSearchIndex))) currentSettings.UseWindowsSearchIndex = importedSettings.UseWindowsSearchIndex;
                    if (Has(nameof(DefaultSettings.EnableDockerSearch))) currentSettings.EnableDockerSearch = importedSettings.EnableDockerSearch;
                    if (Has(nameof(DefaultSettings.SizeUnit))) currentSettings.SizeUnit = importedSettings.SizeUnit;
                    if (Has(nameof(DefaultSettings.ThemePreference))) currentSettings.ThemePreference = importedSettings.ThemePreference;
                    if (Has(nameof(DefaultSettings.UILanguage))) currentSettings.UILanguage = importedSettings.UILanguage ?? string.Empty;
                    if (Has(nameof(DefaultSettings.StringComparisonMode))) currentSettings.StringComparisonMode = importedSettings.StringComparisonMode;
                    if (Has(nameof(DefaultSettings.UnicodeNormalizationMode))) currentSettings.UnicodeNormalizationMode = importedSettings.UnicodeNormalizationMode;
                    if (Has(nameof(DefaultSettings.DiacriticSensitive))) currentSettings.DiacriticSensitive = importedSettings.DiacriticSensitive;
                    if (Has(nameof(DefaultSettings.Culture))) currentSettings.Culture = importedSettings.Culture ?? string.Empty;
                    if (Has(nameof(DefaultSettings.DefaultMatchFiles))) currentSettings.DefaultMatchFiles = importedSettings.DefaultMatchFiles ?? string.Empty;
                    if (Has(nameof(DefaultSettings.DefaultExcludeDirs))) currentSettings.DefaultExcludeDirs = importedSettings.DefaultExcludeDirs ?? string.Empty;
                    if (Has(nameof(DefaultSettings.ContentLineColumnVisible))) currentSettings.ContentLineColumnVisible = importedSettings.ContentLineColumnVisible;
                    if (Has(nameof(DefaultSettings.ContentColumnColumnVisible))) currentSettings.ContentColumnColumnVisible = importedSettings.ContentColumnColumnVisible;
                    if (Has(nameof(DefaultSettings.ContentPathColumnVisible))) currentSettings.ContentPathColumnVisible = importedSettings.ContentPathColumnVisible;
                    if (Has(nameof(DefaultSettings.FilesSizeColumnVisible))) currentSettings.FilesSizeColumnVisible = importedSettings.FilesSizeColumnVisible;
                    if (Has(nameof(DefaultSettings.FilesMatchesColumnVisible))) currentSettings.FilesMatchesColumnVisible = importedSettings.FilesMatchesColumnVisible;
                    if (Has(nameof(DefaultSettings.FilesPathColumnVisible))) currentSettings.FilesPathColumnVisible = importedSettings.FilesPathColumnVisible;
                    if (Has(nameof(DefaultSettings.FilesExtColumnVisible))) currentSettings.FilesExtColumnVisible = importedSettings.FilesExtColumnVisible;
                    if (Has(nameof(DefaultSettings.FilesEncodingColumnVisible))) currentSettings.FilesEncodingColumnVisible = importedSettings.FilesEncodingColumnVisible;
                    if (Has(nameof(DefaultSettings.FilesDateModifiedColumnVisible))) currentSettings.FilesDateModifiedColumnVisible = importedSettings.FilesDateModifiedColumnVisible;
                    if (Has(nameof(DefaultSettings.ContextPreviewLinesBefore))) currentSettings.ContextPreviewLinesBefore = Math.Clamp(importedSettings.ContextPreviewLinesBefore, 1, 20);
                    if (Has(nameof(DefaultSettings.ContextPreviewLinesAfter))) currentSettings.ContextPreviewLinesAfter = Math.Clamp(importedSettings.ContextPreviewLinesAfter, 1, 20);
                    
                    // Note: We intentionally do NOT import window position/size
                    // as this is machine-specific and may not work well on other displays

                    // AI search settings
                    if (Has(nameof(DefaultSettings.AiSearchEndpoint))) currentSettings.AiSearchEndpoint = importedSettings.AiSearchEndpoint?.Trim() ?? string.Empty;
                    if (Has(nameof(DefaultSettings.AiSearchApiKey))) currentSettings.AiSearchApiKey = importedSettings.AiSearchApiKey ?? string.Empty;
                    if (Has(nameof(DefaultSettings.AiSearchModel))) currentSettings.AiSearchModel = importedSettings.AiSearchModel?.Trim() ?? string.Empty;

                    // Save the merged settings
                    SaveSettings(currentSettings);
                    _cachedSettings = null; // Invalidate cache

                    return (true, null);
                }
                catch (JsonException ex)
                {
                    return (false, $"Invalid JSON format: {ex.Message}");
                }
                catch (Exception ex)
                {
                    return (false, $"Error importing settings: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Delete the settings file to restore defaults.
        /// </summary>
        public static void DeleteSettingsFile()
        {
            lock (_lock)
            {
                try
                {
                    var settingsFilePath = GetSettingsFilePath();
                    if (File.Exists(settingsFilePath))
                    {
                        File.Delete(settingsFilePath);
                    }
                    _cachedSettings = null;
                }
                catch
                {
                    // Ignore deletion errors
                }
            }
        }
    }
}

