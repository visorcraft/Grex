using System;

namespace Grex.Models
{
    public class RecentSearch
    {
        public string SearchTerm { get; set; } = string.Empty;
        public string SearchPath { get; set; } = string.Empty;
        public string MatchFileNames { get; set; } = string.Empty;
        public string ExcludeDirs { get; set; } = string.Empty;
        public bool IsRegexSearch { get; set; }
        public bool IsFilesSearch { get; set; }
        public bool SearchCaseSensitive { get; set; }
        public bool RespectGitignore { get; set; }
        public bool IncludeSubfolders { get; set; } = true;
        public bool IncludeHiddenItems { get; set; }
        public bool IncludeBinaryFiles { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int ResultCount { get; set; }

        /// <summary>
        /// Display text for the search history list.
        /// Format: "search term" (Regex) - 42 results
        /// </summary>
        public string DisplayText
        {
            get
            {
                var searchType = IsRegexSearch ? " (Regex)" : "";
                var resultsText = ResultCount == 1 ? "1 result" : $"{ResultCount} results";
                var term = SearchTerm.Length > 40 ? SearchTerm.Substring(0, 37) + "..." : SearchTerm;
                return $"\"{term}\"{searchType} - {resultsText}";
            }
        }

        /// <summary>
        /// Secondary text showing path and timestamp.
        /// Format: path | timestamp
        /// </summary>
        public string SecondaryText
        {
            get
            {
                var path = SearchPath.Length > 50 ? "..." + SearchPath.Substring(SearchPath.Length - 47) : SearchPath;
                return $"{path} | {Timestamp:g}";
            }
        }

        /// <summary>
        /// Creates a unique key for this search based on term, path, and settings.
        /// Used to prevent duplicate entries in history.
        /// </summary>
        public string GetKey()
        {
            return $"{SearchTerm}|{SearchPath}|{IsRegexSearch}|{IsFilesSearch}|{SearchCaseSensitive}|{MatchFileNames}|{ExcludeDirs}";
        }
    }
}
