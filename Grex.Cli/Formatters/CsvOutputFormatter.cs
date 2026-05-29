using System.Text;
using Grex.Models;

namespace Grex.Cli.Formatters;

public class CsvOutputFormatter : IOutputFormatter
{
    public string Format(IEnumerable<SearchResult> results, string basePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("File,Line,Column,Content,FullPath,MatchCount");

        foreach (var result in results)
        {
            sb.AppendLine($"{EscapeCsv(result.RelativePath)},{result.LineNumber},{result.ColumnNumber},{EscapeCsv(result.TrimmedLineContent)},{EscapeCsv(result.FullPath)},{result.MatchCount}");
        }
        return sb.ToString();
    }

    private static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            return $"\"{field.Replace("\"", "\"\"")}\"";

        return field;
    }
}
