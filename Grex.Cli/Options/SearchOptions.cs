namespace Grex.Cli.Options;

public enum OutputFormat
{
    Text,
    Json,
    Csv
}

public class SearchOptions
{
    // Required positional arguments
    public required string Path { get; set; }
    public required string SearchTerm { get; set; }

    // Search behavior flags
    public bool Regex { get; set; }
    public bool CaseSensitive { get; set; }
    public bool Gitignore { get; set; }
    public bool IncludeHidden { get; set; }
    public bool IncludeBinary { get; set; }
    public bool IncludeSystem { get; set; }
    public bool NoSubfolders { get; set; }
    public bool IncludeSymlinks { get; set; }

    // File filters
    public string? MatchFiles { get; set; }
    public string? ExcludeDirs { get; set; }
    public long? SizeLimit { get; set; }
    public string SizeUnit { get; set; } = "KB";
    public string SizeLimitType { get; set; } = "less";

    // Output options
    public OutputFormat Format { get; set; } = OutputFormat.Text;
    public bool Count { get; set; }
    public bool FilesOnly { get; set; }
    public bool Quiet { get; set; }

    // Advanced options
    public string StringComparison { get; set; } = "ordinal";
    public string? UnicodeNormalization { get; set; }
    public bool DiacriticInsensitive { get; set; }
    public string? Culture { get; set; }
}
