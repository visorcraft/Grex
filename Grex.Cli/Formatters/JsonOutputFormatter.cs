using System.Text.Json;
using Grex.Models;

namespace Grex.Cli.Formatters;

public class JsonOutputFormatter : IOutputFormatter
{
    public string Format(IEnumerable<SearchResult> results, string basePath)
    {
        var output = results.Select(r => new
        {
            file = r.RelativePath,
            line = r.LineNumber,
            column = r.ColumnNumber,
            content = r.TrimmedLineContent,
            matchCount = r.MatchCount,
            fullPath = r.FullPath
        });

        return JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
