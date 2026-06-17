using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Grex.Models;

namespace Grex.Services
{
    public static class SearchProfilesService
    {
        private static string ProfilesFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Grex",
            "search_profiles.json"
        );

        internal static string ProfilesFilePath
        {
            get => ProfilesFile;
            set => ProfilesFile = value;
        }

        private const int MaxProfiles = 50;
        private static readonly object _lock = new object();

        public static List<SearchProfile> GetProfiles()
        {
            lock (_lock)
            {
                try
                {
                    var directory = Path.GetDirectoryName(ProfilesFile);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (!File.Exists(ProfilesFile))
                    {
                        return new List<SearchProfile>();
                    }

                    var json = File.ReadAllText(ProfilesFile);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return new List<SearchProfile>();
                    }

                    var profiles = JsonSerializer.Deserialize<List<SearchProfile>>(json);
                    return profiles ?? new List<SearchProfile>();
                }
                catch
                {
                    return new List<SearchProfile>();
                }
            }
        }

        public static bool Exists(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            lock (_lock)
            {
                var profiles = GetProfiles();
                return profiles.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static void AddOrUpdateProfile(SearchProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.Name))
            {
                return;
            }

            lock (_lock)
            {
                try
                {
                    var directory = Path.GetDirectoryName(ProfilesFile);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var profiles = GetProfiles();
                    var existing = profiles.FirstOrDefault(p => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));

                    var now = DateTime.Now;
                    if (existing != null)
                    {
                        profile.CreatedAt = existing.CreatedAt == default ? now : existing.CreatedAt;
                        profile.UpdatedAt = now;

                        profiles.Remove(existing);
                        profiles.Insert(0, profile);
                    }
                    else
                    {
                        profile.CreatedAt = profile.CreatedAt == default ? now : profile.CreatedAt;
                        profile.UpdatedAt = now;
                        profiles.Insert(0, profile);
                    }

                    // Keep the profile list bounded so the JSON file and memory don't grow forever.
                    if (profiles.Count > MaxProfiles)
                    {
                        profiles.RemoveRange(MaxProfiles, profiles.Count - MaxProfiles);
                    }

                    var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(ProfilesFile, json);
                }
                catch
                {
                    // Ignore errors when saving
                }
            }
        }

        public static void DeleteProfile(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            lock (_lock)
            {
                try
                {
                    var profiles = GetProfiles();
                    profiles.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                    var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(ProfilesFile, json);
                }
                catch
                {
                    // Ignore errors when deleting
                }
            }
        }

        public static void ClearProfiles()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(ProfilesFile))
                    {
                        File.Delete(ProfilesFile);
                    }
                }
                catch
                {
                    // Ignore errors when deleting
                }
            }
        }
    }
}
