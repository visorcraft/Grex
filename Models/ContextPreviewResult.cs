using System.Collections.Generic;

namespace Grex.Models
{
    /// <summary>
    /// Represents the result of loading context lines around a search match.
    /// </summary>
    public class ContextPreviewResult
    {
        /// <summary>
        /// The list of context lines (including the match line).
        /// </summary>
        public List<ContextLine> Lines { get; set; } = new();

        /// <summary>
        /// Index within Lines where the match line is located.
        /// </summary>
        public int MatchLineIndex { get; set; }

        /// <summary>
        /// The file name (without path).
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The full path to the file.
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// The line number of the original match.
        /// </summary>
        public int MatchLineNumber { get; set; }
    }

    /// <summary>
    /// Represents a single line within the context preview.
    /// </summary>
    public class ContextLine
    {
        /// <summary>
        /// The 1-based line number in the file.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The content of the line.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// True if this line is the one containing the search match.
        /// </summary>
        public bool IsMatchLine { get; set; }
    }
}
