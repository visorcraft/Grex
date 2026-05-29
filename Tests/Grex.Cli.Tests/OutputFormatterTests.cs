using FluentAssertions;
using Grex.Cli.Formatters;
using Grex.Models;
using Xunit;

namespace Grex.Cli.Tests;

public class OutputFormatterTests
{
    private readonly List<SearchResult> _sampleResults = new()
    {
        new SearchResult
        {
            FileName = "file.cs",
            RelativePath = "src/file.cs",
            FullPath = "C:\\Project\\src\\file.cs",
            LineNumber = 42,
            ColumnNumber = 10,
            LineContent = "    // TODO: Fix this",
            MatchCount = 1
        },
        new SearchResult
        {
            FileName = "other.cs",
            RelativePath = "src/other.cs",
            FullPath = "C:\\Project\\src\\other.cs",
            LineNumber = 100,
            ColumnNumber = 5,
            LineContent = "  TODO: Another item",
            MatchCount = 1
        }
    };

    [Fact]
    public void TextOutputFormatter_SingleResult_ReturnsGrepCompatibleFormat()
    {
        var formatter = new TextOutputFormatter();
        var results = new List<SearchResult>
        {
            new()
            {
                RelativePath = "src/file.cs",
                LineNumber = 42,
                ColumnNumber = 10,
                LineContent = "    // TODO: Fix this"
            }
        };

        var output = formatter.Format(results, "C:\\Project");

        output.Should().Be("src/file.cs:42:10:// TODO: Fix this\r\n");
    }

    [Fact]
    public void TextOutputFormatter_MultipleResults_FormatsEachLine()
    {
        var formatter = new TextOutputFormatter();

        var output = formatter.Format(_sampleResults, "C:\\Project");

        output.Should().Contain("src/file.cs:42:10:// TODO: Fix this");
        output.Should().Contain("src/other.cs:100:5:TODO: Another item");
    }

    [Fact]
    public void TextOutputFormatter_EmptyResults_ReturnsEmptyString()
    {
        var formatter = new TextOutputFormatter();

        var output = formatter.Format(new List<SearchResult>(), "C:\\Project");

        output.Should().BeEmpty();
    }

    [Fact]
    public void JsonOutputFormatter_ReturnsValidJson()
    {
        var formatter = new JsonOutputFormatter();

        var output = formatter.Format(_sampleResults, "C:\\Project");

        output.Should().StartWith("[");
        output.Should().EndWith("]");
        output.Should().Contain("\"file\": \"src/file.cs\"");
        output.Should().Contain("\"line\": 42");
        output.Should().Contain("\"column\": 10");
    }

    [Fact]
    public void JsonOutputFormatter_EmptyResults_ReturnsEmptyArray()
    {
        var formatter = new JsonOutputFormatter();

        var output = formatter.Format(new List<SearchResult>(), "C:\\Project");

        output.Trim().Should().Be("[]");
    }

    [Fact]
    public void CsvOutputFormatter_IncludesHeader()
    {
        var formatter = new CsvOutputFormatter();

        var output = formatter.Format(_sampleResults, "C:\\Project");

        output.Should().StartWith("File,Line,Column,Content,FullPath,MatchCount");
    }

    [Fact]
    public void CsvOutputFormatter_FormatsDataRows()
    {
        var formatter = new CsvOutputFormatter();

        var output = formatter.Format(_sampleResults, "C:\\Project");
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(3); // Header + 2 data rows
        lines[1].Should().StartWith("src/file.cs,42,10,");
    }

    [Fact]
    public void CsvOutputFormatter_EscapesCommasInContent()
    {
        var formatter = new CsvOutputFormatter();
        var results = new List<SearchResult>
        {
            new()
            {
                RelativePath = "test.cs",
                LineNumber = 1,
                ColumnNumber = 1,
                LineContent = "var x = \"hello, world\";",
                FullPath = "C:\\test.cs",
                MatchCount = 1
            }
        };

        var output = formatter.Format(results, "C:\\");

        // Content with comma should be quoted
        output.Should().Contain("\"var x = \"\"hello, world\"\";\"");
    }
}
