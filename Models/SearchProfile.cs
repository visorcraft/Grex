using System;

namespace Grex.Models
{
    public class SearchProfile
    {
        public string Name { get; set; } = string.Empty;
        public string SearchPath { get; set; } = string.Empty;
        public string SearchTerm { get; set; } = string.Empty;

        public bool IsRegexSearch { get; set; }
        public bool IsFilesSearch { get; set; }

        public bool RespectGitignore { get; set; }
        public bool SearchCaseSensitive { get; set; }
        public bool IncludeSystemFiles { get; set; }
        public bool IncludeSubfolders { get; set; } = true;
        public bool IncludeHiddenItems { get; set; }
        public bool IncludeBinaryFiles { get; set; }
        public bool IncludeSymbolicLinks { get; set; }
        public bool UseWindowsSearchIndex { get; set; }

        public string MatchFileNames { get; set; } = string.Empty;
        public string ExcludeDirs { get; set; } = string.Empty;

        public SizeLimitType SizeLimitType { get; set; } = SizeLimitType.NoLimit;
        public long? SizeLimitKB { get; set; }
        public SizeUnit SizeUnit { get; set; } = SizeUnit.KB;

        public StringComparisonMode StringComparisonMode { get; set; } = StringComparisonMode.Ordinal;
        public UnicodeNormalizationMode UnicodeNormalizationMode { get; set; } = UnicodeNormalizationMode.None;
        public bool DiacriticSensitive { get; set; } = true;
        public string Culture { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string SecondaryText
        {
            get
            {
                var path = SearchPath.Length > 50 ? "..." + SearchPath.Substring(Math.Max(0, SearchPath.Length - 47)) : SearchPath;
                var term = SearchTerm.Length > 40 ? SearchTerm.Substring(0, 37) + "..." : SearchTerm;
                return $"{term} | {path}";
            }
        }
    }
}
