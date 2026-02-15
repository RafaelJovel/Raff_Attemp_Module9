using System.Diagnostics;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Models;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace FeatureAssessment.TestHarness;

/// <summary>
/// Helper for formatting and displaying console output.
/// </summary>
public class ConsoleOutputHelper
{
    private readonly IOptions<LlmProviderConfiguration> _providerConfig;
    private readonly IOptions<OllamaConfiguration> _ollamaConfig;
    private readonly IOptions<AnthropicConfiguration> _anthropicConfig;

    public ConsoleOutputHelper(
        IOptions<LlmProviderConfiguration> providerConfig,
        IOptions<OllamaConfiguration> ollamaConfig,
        IOptions<AnthropicConfiguration> anthropicConfig)
    {
        _providerConfig = providerConfig;
        _ollamaConfig = ollamaConfig;
        _anthropicConfig = anthropicConfig;
    }

    /// <summary>
    /// Display current configuration settings based on active provider.
    /// </summary>
    public void DisplayConfiguration()
    {
        var provider = _providerConfig.Value.Provider;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Setting[/]").Centered())
            .AddColumn(new TableColumn("[bold]Value[/]"));

        table.AddRow("Provider", $"[cyan]{provider}[/]");

        if (provider == LlmProvider.Ollama)
        {
            var config = _ollamaConfig.Value;
            table.AddRow("Endpoint", $"[cyan]{config.Endpoint}[/]");
            table.AddRow("Model Name", $"[cyan]{config.ModelName}[/]");
            table.AddRow("Timeout (seconds)", $"[cyan]{config.TimeoutSeconds}[/]");
            table.AddRow("Max Retries", $"[cyan]{config.MaxRetries}[/]");
        }
        else if (provider == LlmProvider.Anthropic)
        {
            var config = _anthropicConfig.Value;
            table.AddRow("Model Name", $"[cyan]{config.ModelName}[/]");
            table.AddRow("Temperature", $"[cyan]{config.Temperature}[/]");
            table.AddRow("Max Tokens", $"[cyan]{config.MaxTokens}[/]");
            table.AddRow("Timeout (seconds)", $"[cyan]{config.TimeoutSeconds}[/]");
            table.AddRow("Max Retries", $"[cyan]{config.MaxRetries}[/]");
            table.AddRow("API Key", $"[cyan]{MaskApiKey(config.ApiKey)}[/]");
        }

        AnsiConsole.Write(
            new Panel(table)
                .Header("[bold yellow]Current Configuration[/]")
                .BorderColor(Color.Yellow)
        );
        AnsiConsole.WriteLine();
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 12)
            return "***";

        return $"{apiKey.Substring(0, 10)}...{apiKey.Substring(apiKey.Length - 4)}";
    }

    /// <summary>
    /// Display scenario header.
    /// </summary>
    public void DisplayScenarioHeader(string category, string scenarioName)
    {
        var rule = new Rule($"[bold blue]{category}[/]: {scenarioName}")
        {
            Justification = Justify.Left
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display query being executed.
    /// </summary>
    public void DisplayQuery(string query)
    {
        AnsiConsole.MarkupLine($"[bold]Query:[/] [italic]{query}[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display expected behavior.
    /// </summary>
    public void DisplayExpectedBehavior(string expectedBehavior)
    {
        AnsiConsole.MarkupLine($"[dim]Expected: {expectedBehavior}[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display the result of a feature lookup.
    /// </summary>
    public void DisplayResult(FeatureLookupResult result, TimeSpan elapsed)
    {
        if (result.IsSuccess)
        {
            var table = new Table()
                .Border(TableBorder.Square)
                .BorderColor(Color.Green)
                .AddColumn(new TableColumn("[bold]Field[/]"))
                .AddColumn(new TableColumn("[bold]Value[/]"));

            table.AddRow("Status", "[green]✓ Success[/]");
            table.AddRow("Feature Key", $"[cyan]{result.FeatureKey ?? "N/A"}[/]");
            table.AddRow("Feature ID", $"[cyan]{result.FeatureId ?? "N/A"}[/]");
            table.AddRow("Target Environment", $"[cyan]{result.TargetEnvironment ?? "N/A"}[/]");
            table.AddRow("Execution Time", $"[yellow]{elapsed.TotalSeconds:F2}s[/]");

            AnsiConsole.Write(table);
        }
        else
        {
            var panel = new Panel(
                new Markup($"[red]✗ Failed[/]\n\n[dim]{result.ErrorMessage ?? "Unknown error"}[/]")
            )
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red),
                Padding = new Padding(2, 1)
            };

            AnsiConsole.Write(panel);
            AnsiConsole.MarkupLine($"[dim]Execution Time: {elapsed.TotalSeconds:F2}s[/]");
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display trace/activity information if available.
    /// </summary>
    public void DisplayTraceInfo(Activity? activity)
    {
        if (activity == null)
        {
            AnsiConsole.MarkupLine("[dim]No trace information available[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold]Trace Information:[/]");
        AnsiConsole.MarkupLine($"  Trace ID: [dim]{activity.TraceId}[/]");
        AnsiConsole.MarkupLine($"  Span ID: [dim]{activity.SpanId}[/]");
        AnsiConsole.MarkupLine($"  Duration: [yellow]{activity.Duration.TotalMilliseconds:F2}ms[/]");

        if (activity.Tags.Any())
        {
            AnsiConsole.MarkupLine("  [bold]Tags:[/]");
            foreach (var tag in activity.Tags)
            {
                AnsiConsole.MarkupLine($"    {tag.Key}: [cyan]{tag.Value}[/]");
            }
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display error message.
    /// </summary>
    public void DisplayError(string message, Exception? ex = null)
    {
        AnsiConsole.MarkupLine($"[red bold]✗ Error:[/] {message}");

        if (ex != null)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display welcome banner.
    /// </summary>
    public void DisplayWelcomeBanner()
    {
        var banner = new FigletText("Feature Lookup")
            .Centered()
            .Color(Color.Blue);

        AnsiConsole.Write(banner);
        AnsiConsole.MarkupLine("[dim]Manual Testing Harness for Feature Lookup Agent[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display menu options and get user choice.
    /// </summary>
    public string DisplayMenu()
    {
        AnsiConsole.WriteLine();
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]What would you like to do?[/]")
                .AddChoices(new[]
                {
                    "Run all scenarios",
                    "Run scenarios by category",
                    "Run single scenario",
                    "Enter custom query",
                    "Show configuration",
                    "Exit"
                })
        );

        return choice;
    }

    /// <summary>
    /// Display category selection and get user choice.
    /// </summary>
    public string SelectCategory(List<string> categories)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select a category:[/]")
                .AddChoices(categories)
        );
    }

    /// <summary>
    /// Display scenario selection and get user choice.
    /// </summary>
    public TestScenario SelectScenario(List<TestScenario> scenarios)
    {
        var scenarioNames = scenarios.Select(s => s.Name).ToList();
        var selectedName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select a scenario:[/]")
                .AddChoices(scenarioNames)
        );

        return scenarios.First(s => s.Name == selectedName);
    }

    /// <summary>
    /// Prompt for custom query input.
    /// </summary>
    public string PromptForCustomQuery()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("[bold]Enter your query:[/]")
                .PromptStyle("cyan")
                .ValidationErrorMessage("[red]Query cannot be empty[/]")
                .Validate(query => !string.IsNullOrWhiteSpace(query))
        );
    }

    /// <summary>
    /// Display progress spinner while executing.
    /// </summary>
    public async Task<T> WithSpinner<T>(string message, Func<Task<T>> action)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("yellow"))
            .StartAsync(message, async ctx => await action());
    }

    /// <summary>
    /// Ask if user wants to continue.
    /// </summary>
    public bool AskToContinue()
    {
        return AnsiConsole.Confirm("[dim]Press enter to continue...[/]", false);
    }
}
