using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Grex.Cli.Commands;

namespace Grex.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = SearchCommand.Create();

        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler((ex, context) =>
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 2;
            })
            .Build();

        return await parser.InvokeAsync(args);
    }
}
