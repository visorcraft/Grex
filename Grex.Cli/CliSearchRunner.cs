using Grex.Cli.Formatters;
using Grex.Cli.Options;
using Grex.Models;
using Grex.Services;

namespace Grex.Cli;

public class CliSearchRunner
{
    private readonly ISearchService _searchService;

    public CliSearchRunner(ISearchService? searchService = null)
    {
        _searchService = searchService ?? new SearchService();
    }

    public async Task<int> RunAsync(SearchOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate path exists
            if (!Directory.Exists(options.Path))
            {
                if (!options.Quiet)
                    Console.Error.WriteLine($"Error: Directory not found: {options.Path}");
                return 2;
            }

            // Resolve to absolute path
            var absolutePath = Path.GetFullPath(options.Path);

            // Map CLI options to SearchService parameters
            var sizeLimitType = ParseSizeLimitType(options.SizeLimitType);
            var sizeUnit = ParseSizeUnit(options.SizeUnit);

            var results = await _searchService.SearchAsync(
                path: absolutePath,
                searchTerm: options.SearchTerm,
                isRegex: options.Regex,
                respectGitignore: options.Gitignore,
                searchCaseSensitive: options.CaseSensitive,
                includeSystemFiles: options.IncludeSystem,
                includeSubfolders: !options.NoSubfolders,
                includeHiddenItems: options.IncludeHidden,
                includeBinaryFiles: options.IncludeBinary,
                includeSymbolicLinks: options.IncludeSymlinks,
                sizeLimitType: sizeLimitType,
                sizeLimitKB: options.SizeLimit,
                sizeUnit: sizeUnit,
                matchFileNames: options.MatchFiles ?? "",
                excludeDirs: options.ExcludeDirs ?? "",
                cancellationToken: cancellationToken
            );

            // Handle quiet mode - exit code only
            if (options.Quiet)
            {
                return results.Count > 0 ? 0 : 1;
            }

            // Handle count mode
            if (options.Count)
            {
                var totalMatches = results.Sum(r => r.MatchCount);
                Console.WriteLine(totalMatches);
                return results.Count > 0 ? 0 : 1;
            }

            // Handle files-only mode
            if (options.FilesOnly)
            {
                var uniqueFiles = results.Select(r => r.FullPath).Distinct();
                foreach (var file in uniqueFiles)
                {
                    Console.WriteLine(file);
                }
                return results.Count > 0 ? 0 : 1;
            }

            // Normal output with formatter
            var formatter = GetFormatter(options.Format);
            var output = formatter.Format(results, absolutePath);
            Console.Write(output);

            return results.Count > 0 ? 0 : 1;
        }
        catch (ArgumentException ex)
        {
            if (!options.Quiet)
                Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
        catch (OperationCanceledException)
        {
            if (!options.Quiet)
                Console.Error.WriteLine("Search cancelled.");
            return 2;
        }
        catch (Exception ex)
        {
            if (!options.Quiet)
                Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
    }

    private static IOutputFormatter GetFormatter(OutputFormat format) => format switch
    {
        OutputFormat.Json => new JsonOutputFormatter(),
        OutputFormat.Csv => new CsvOutputFormatter(),
        _ => new TextOutputFormatter()
    };

    private static SizeLimitType ParseSizeLimitType(string type) => type.ToLowerInvariant() switch
    {
        "less" => SizeLimitType.LessThan,
        "equal" => SizeLimitType.EqualTo,
        "greater" => SizeLimitType.GreaterThan,
        _ => SizeLimitType.NoLimit
    };

    private static SizeUnit ParseSizeUnit(string unit) => unit.ToUpperInvariant() switch
    {
        "MB" => SizeUnit.MB,
        "GB" => SizeUnit.GB,
        _ => SizeUnit.KB
    };

}
