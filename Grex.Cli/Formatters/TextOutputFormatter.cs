using System.Text;
using Grex.Models;

namespace Grex.Cli.Formatters;

public class TextOutputFormatter : IOutputFormatter
{
    public string Format(IEnumerable<SearchResult> results, string basePath)
    {
        var sb = new StringBuilder();
        foreach (var result in results)
        {
            // grep-compatible format: path:line:column:content
            sb.AppendLine($"{result.RelativePath}:{result.LineNumber}:{result.ColumnNumber}:{result.TrimmedLineContent}");
        }
        return sb.ToString();
    }
}
