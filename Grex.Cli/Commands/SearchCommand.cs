using System.CommandLine;
using Grex.Cli.Options;

namespace Grex.Cli.Commands;

public static class SearchCommand
{
    public static RootCommand Create()
    {
        // Positional arguments
        var pathArg = new Argument<string>("path", "Directory path to search");
        var termArg = new Argument<string>("term", "Search term or regex pattern");

        // Search behavior options
        var regexOpt = new Option<bool>(
            new[] { "-E", "--regex" },
            "Treat search term as regex pattern");

        var caseOpt = new Option<bool>(
            new[] { "-i", "--case-sensitive" },
            "Case sensitive search");

        var gitignoreOpt = new Option<bool>(
            new[] { "-g", "--gitignore" },
            "Respect .gitignore files");

        var hiddenOpt = new Option<bool>(
            new[] { "-H", "--include-hidden" },
            "Include hidden files");

        var binaryOpt = new Option<bool>(
            new[] { "-b", "--include-binary" },
            "Include binary files");

        var systemOpt = new Option<bool>(
            new[] { "-s", "--include-system" },
            "Include system files");

        var noSubfoldersOpt = new Option<bool>(
            new[] { "-d", "--no-subfolders" },
            "Don't recurse into subdirectories");

        var symlinksOpt = new Option<bool>(
            new[] { "-L", "--include-symlinks" },
            "Follow symbolic links");

        // File filter options
        var matchFilesOpt = new Option<string?>(
            new[] { "-m", "--match-files" },
            "File name pattern (e.g., *.cs;*.txt)");

        var excludeDirsOpt = new Option<string?>(
            new[] { "-x", "--exclude-dirs" },
            "Directories to exclude (semicolon-separated)");

        var sizeLimitOpt = new Option<long?>(
            "--size-limit",
            "File size limit");

        var sizeUnitOpt = new Option<string>(
            "--size-unit",
            () => "KB",
            "Size unit: KB, MB, GB");

        var sizeLimitTypeOpt = new Option<string>(
            "--size-type",
            () => "less",
            "Size comparison: less, equal, greater");

        // Output options
        var formatOpt = new Option<OutputFormat>(
            new[] { "-f", "--format" },
            () => OutputFormat.Text,
            "Output format: text, json, csv");

        var countOpt = new Option<bool>(
            new[] { "-c", "--count" },
            "Only print total match count");

        var filesOnlyOpt = new Option<bool>(
            new[] { "-l", "--files-only" },
            "Only print file names with matches");

        var quietOpt = new Option<bool>(
            new[] { "-q", "--quiet" },
            "Suppress all output, exit code indicates match");

        var rootCommand = new RootCommand("Grex - Fast file content search for Windows")
        {
            pathArg,
            termArg,
            regexOpt,
            caseOpt,
            gitignoreOpt,
            hiddenOpt,
            binaryOpt,
            systemOpt,
            noSubfoldersOpt,
            symlinksOpt,
            matchFilesOpt,
            excludeDirsOpt,
            sizeLimitOpt,
            sizeUnitOpt,
            sizeLimitTypeOpt,
            formatOpt,
            countOpt,
            filesOnlyOpt,
            quietOpt
        };

        rootCommand.SetHandler(async (context) =>
        {
            var options = new SearchOptions
            {
                Path = context.ParseResult.GetValueForArgument(pathArg),
                SearchTerm = context.ParseResult.GetValueForArgument(termArg),
                Regex = context.ParseResult.GetValueForOption(regexOpt),
                CaseSensitive = context.ParseResult.GetValueForOption(caseOpt),
                Gitignore = context.ParseResult.GetValueForOption(gitignoreOpt),
                IncludeHidden = context.ParseResult.GetValueForOption(hiddenOpt),
                IncludeBinary = context.ParseResult.GetValueForOption(binaryOpt),
                IncludeSystem = context.ParseResult.GetValueForOption(systemOpt),
                NoSubfolders = context.ParseResult.GetValueForOption(noSubfoldersOpt),
                IncludeSymlinks = context.ParseResult.GetValueForOption(symlinksOpt),
                MatchFiles = context.ParseResult.GetValueForOption(matchFilesOpt),
                ExcludeDirs = context.ParseResult.GetValueForOption(excludeDirsOpt),
                SizeLimit = context.ParseResult.GetValueForOption(sizeLimitOpt),
                SizeUnit = context.ParseResult.GetValueForOption(sizeUnitOpt) ?? "KB",
                SizeLimitType = context.ParseResult.GetValueForOption(sizeLimitTypeOpt) ?? "less",
                Format = context.ParseResult.GetValueForOption(formatOpt),
                Count = context.ParseResult.GetValueForOption(countOpt),
                FilesOnly = context.ParseResult.GetValueForOption(filesOnlyOpt),
                Quiet = context.ParseResult.GetValueForOption(quietOpt)
            };

            var runner = new CliSearchRunner();
            context.ExitCode = await runner.RunAsync(options, context.GetCancellationToken());
        });

        return rootCommand;
    }
}
