using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Grex.Models;

namespace Grex.Services
{
    public class RecentSearchesService
    {
        private static readonly string RecentSearchesFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Grex",
            "search_history.json"
        );

        private const int MaxRecentSearches = 20;
        private static readonly object _lock = new object();

        public static List<RecentSearch> GetRecentSearches()
        {
            lock (_lock)
            {
                try
                {
                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(RecentSearchesFile);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (!File.Exists(RecentSearchesFile))
                    {
                        return new List<RecentSearch>();
                    }

                    var json = File.ReadAllText(RecentSearchesFile);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return new List<RecentSearch>();
                    }

                    var searches = JsonSerializer.Deserialize<List<RecentSearch>>(json);
                    return searches ?? new List<RecentSearch>();
                }
                catch
                {
                    return new List<RecentSearch>();
                }
            }
        }

        public static void AddRecentSearch(RecentSearch search)
        {
            if (search == null || string.IsNullOrWhiteSpace(search.SearchTerm))
                return;

            lock (_lock)
            {
                try
                {
                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(RecentSearchesFile);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var recentSearches = GetRecentSearches();

                    // Remove if already exists with same key (to move it to top with updated data)
                    var searchKey = search.GetKey();
                    recentSearches.RemoveAll(s => s.GetKey() == searchKey);

                    // Add to beginning
                    recentSearches.Insert(0, search);

                    // Keep only the most recent searches
                    if (recentSearches.Count > MaxRecentSearches)
                    {
                        recentSearches = recentSearches.Take(MaxRecentSearches).ToList();
                    }

                    // Save to file
                    var json = JsonSerializer.Serialize(recentSearches, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(RecentSearchesFile, json);
                }
                catch
                {
                    // Ignore errors when saving
                }
            }
        }

        public static void RemoveRecentSearch(RecentSearch search)
        {
            if (search == null)
                return;

            lock (_lock)
            {
                try
                {
                    var recentSearches = GetRecentSearches();
                    var searchKey = search.GetKey();
                    recentSearches.RemoveAll(s => s.GetKey() == searchKey);

                    // Save to file
                    var json = JsonSerializer.Serialize(recentSearches, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(RecentSearchesFile, json);
                }
                catch
                {
                    // Ignore errors when saving
                }
            }
        }

        public static void ClearHistory()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(RecentSearchesFile))
                    {
                        File.Delete(RecentSearchesFile);
                    }
                }
                catch
                {
                    // Ignore errors when deleting
                }
            }
        }

        public static List<RecentSearch> FilterSearches(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return GetRecentSearches();
            }

            var allSearches = GetRecentSearches();
            var searchLower = searchText.ToLowerInvariant();

            return allSearches
                .Where(s => s.SearchTerm.ToLowerInvariant().Contains(searchLower) ||
                           s.SearchPath.ToLowerInvariant().Contains(searchLower))
                .ToList();
        }
    }
}
