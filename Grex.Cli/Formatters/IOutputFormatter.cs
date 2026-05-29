using Grex.Models;

namespace Grex.Cli.Formatters;

public interface IOutputFormatter
{
    string Format(IEnumerable<SearchResult> results, string basePath);
}
