using System.Diagnostics;
using FeatureAssessment.Core.Agents;
using Spectre.Console;
using FeatureAssessment.Core.Clients;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Models;
using FeatureAssessment.Core.Observability;
using FeatureAssessment.Core.Tools;
using FeatureAssessment.Core.Workflow;
using FeatureAssessment.TestHarness;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Locate the src-level local config (same file used by integration tests, gitignored)
// Path: bin/Debug/net10.0 → up 5 levels → repo root → src/FeatureAssessment.Core/
var srcLocalConfig = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..",
    "src", "FeatureAssessment.Core", "appsettings.Development.local.json"));

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(srcLocalConfig, optional: true, reloadOnChange: false)  // local API keys (gitignored)
    .AddEnvironmentVariables()
    .Build();

// Setup DI container
var services = new ServiceCollection();

// Configure LLM Provider
services.Configure<LlmProviderConfiguration>(configuration.GetSection(LlmProviderConfiguration.SectionName));

// Configure Ollama
services.Configure<OllamaConfiguration>(configuration.GetSection(OllamaConfiguration.SectionName));
services.AddSingleton<IValidateOptions<OllamaConfiguration>, OllamaConfigurationValidator>();

// Configure Anthropic
services.Configure<AnthropicConfiguration>(configuration.GetSection(AnthropicConfiguration.SectionName));
services.AddSingleton<IValidateOptions<AnthropicConfiguration>, AnthropicOptionsValidator>();

// Determine data directory path (relative to project root)
// When running from tests/FeatureAssessment.TestHarness/bin/Debug/net10.0, go up 5 levels to project root, then into data
// Note: FeatureLookupTools will append "incoming" subdirectory internally
var dataDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data"));

// Register application services
services.AddSingleton<IFeatureLookupTools>(sp => new FeatureLookupTools(dataDirectory));
services.AddSingleton<IKernelFactory, KernelFactory>();
services.AddSingleton<IFeatureLookupAgent, FeatureLookupAgent>();
services.AddSingleton<ICoordinatorAgent, CoordinatorAgent>();
services.AddSingleton<IAssessmentWorkflow, AssessmentWorkflow>();
services.AddSingleton<ConsoleOutputHelper>();

// Configure OpenTelemetry with console exporter
services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "FeatureAssessment.TestHarness", serviceVersion: "1.0.0"))
    .WithTracing(builder => builder
        .AddSource(ActivitySources.FeatureLookup.Name)
        .AddSource(ActivitySources.Tools.Name)
        .AddSource(ActivitySources.Coordinator.Name)
        .AddConsoleExporter());

// Configure logging from appsettings.json (console + file)
var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "test-harness-.log");
services.AddLogging(builder => builder
    .AddConfiguration(configuration.GetSection("Logging"))
    .AddConsole()
    .AddFile(logFilePath));

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Validate configuration at startup
try
{
    var providerConfig = serviceProvider.GetRequiredService<IOptions<LlmProviderConfiguration>>().Value;
    Console.WriteLine($"Using LLM Provider: {providerConfig.Provider}");

    if (providerConfig.Provider == LlmProvider.Ollama)
    {
        var ollamaConfig = serviceProvider.GetRequiredService<IOptions<OllamaConfiguration>>().Value;
        var validator = serviceProvider.GetRequiredService<IValidateOptions<OllamaConfiguration>>();
        var validationResult = validator.Validate(OllamaConfiguration.SectionName, ollamaConfig);

        if (validationResult.Failed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ollama configuration validation failed: {validationResult.FailureMessage}");
            Console.ResetColor();
            return 1;
        }
    }
    else if (providerConfig.Provider == LlmProvider.Anthropic)
    {
        var anthropicConfig = serviceProvider.GetRequiredService<IOptions<AnthropicConfiguration>>().Value;
        var validator = serviceProvider.GetRequiredService<IValidateOptions<AnthropicConfiguration>>();
        var validationResult = validator.Validate(AnthropicConfiguration.SectionName, anthropicConfig);

        if (validationResult.Failed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Anthropic configuration validation failed: {validationResult.FailureMessage}");
            Console.ResetColor();
            return 1;
        }

        Console.WriteLine($"Using Anthropic model: {anthropicConfig.ModelName}");
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Startup error: {ex.Message}");
    Console.ResetColor();
    return 1;
}

// Run interactive loop
await RunInteractiveLoop(serviceProvider);

return 0;

// =============================================
// Main Interactive Loop
// =============================================

static async Task RunInteractiveLoop(ServiceProvider serviceProvider)
{
    var agent = serviceProvider.GetRequiredService<IFeatureLookupAgent>();
    var workflow = serviceProvider.GetRequiredService<IAssessmentWorkflow>();
    var outputHelper = serviceProvider.GetRequiredService<ConsoleOutputHelper>();

    outputHelper.DisplayWelcomeBanner();
    outputHelper.DisplayConfiguration();

    while (true)
    {
        var choice = outputHelper.DisplayMenu();

        try
        {
            switch (choice)
            {
                case "Run all scenarios":
                    await RunAllScenarios(agent, outputHelper);
                    break;

                case "Run scenarios by category":
                    await RunScenariosByCategory(agent, outputHelper);
                    break;

                case "Run single scenario":
                    await RunSingleScenario(agent, outputHelper);
                    break;

                case "Enter custom query":
                    await RunCustomQuery(agent, outputHelper);
                    break;

                case "Run coordinator assessment":
                    await RunCoordinatorAssessment(workflow, outputHelper);
                    break;

                case "Show configuration":
                    outputHelper.DisplayConfiguration();
                    break;

                case "Exit":
                    Console.WriteLine("Goodbye!");
                    return;
            }
        }
        catch (Exception ex)
        {
            outputHelper.DisplayError("An error occurred", ex);
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
        Console.Clear();
        outputHelper.DisplayWelcomeBanner();
    }
}

// =============================================
// Scenario Execution Methods
// =============================================

static async Task RunAllScenarios(IFeatureLookupAgent agent, ConsoleOutputHelper outputHelper)
{
    var allScenarios = TestScenarios.GetAllScenarios();
    var totalScenarios = allScenarios.Count;
    var successCount = 0;
    var failureCount = 0;

    Console.WriteLine($"Running {totalScenarios} scenarios...");
    Console.WriteLine();

    foreach (var (category, scenario) in allScenarios)
    {
        var result = await ExecuteScenario(agent, outputHelper, category, scenario);

        if (result.IsSuccess)
            successCount++;
        else
            failureCount++;

        Console.WriteLine();
    }

    // Summary
    Console.WriteLine("=".PadRight(60, '='));
    Console.WriteLine($"Total: {totalScenarios} | Success: {successCount} | Failed: {failureCount}");
    Console.WriteLine("=".PadRight(60, '='));
}

static async Task RunScenariosByCategory(IFeatureLookupAgent agent, ConsoleOutputHelper outputHelper)
{
    var categories = TestScenarios.Scenarios.Keys.ToList();
    var selectedCategory = outputHelper.SelectCategory(categories);

    var scenarios = TestScenarios.GetScenariosByCategory(selectedCategory);

    Console.WriteLine($"Running {scenarios.Count} scenarios in category '{selectedCategory}'...");
    Console.WriteLine();

    foreach (var scenario in scenarios)
    {
        await ExecuteScenario(agent, outputHelper, selectedCategory, scenario);
        Console.WriteLine();
    }
}

static async Task RunSingleScenario(IFeatureLookupAgent agent, ConsoleOutputHelper outputHelper)
{
    var categories = TestScenarios.Scenarios.Keys.ToList();
    var selectedCategory = outputHelper.SelectCategory(categories);
    var scenarios = TestScenarios.GetScenariosByCategory(selectedCategory);
    var selectedScenario = outputHelper.SelectScenario(scenarios);

    await ExecuteScenario(agent, outputHelper, selectedCategory, selectedScenario);
}

static async Task RunCoordinatorAssessment(
    IAssessmentWorkflow workflow,
    ConsoleOutputHelper outputHelper)
{
    var query = outputHelper.PromptForCustomQuery();

    outputHelper.DisplayScenarioHeader("Coordinator Assessment", "Full Pipeline: Lookup → Coordinator");
    outputHelper.DisplayQuery(query);

    var stopwatch = Stopwatch.StartNew();

    try
    {
        var finalState = await outputHelper.WithSpinner(
            "Running assessment pipeline (Lookup → Coordinator)...",
            () => workflow.RunAsync(query, CancellationToken.None));

        stopwatch.Stop();
        outputHelper.DisplayCoordinatorResult(finalState, stopwatch.Elapsed);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        outputHelper.DisplayError($"Pipeline failed after {stopwatch.Elapsed.TotalSeconds:F2}s", ex);
    }
}

static async Task RunCustomQuery(IFeatureLookupAgent agent, ConsoleOutputHelper outputHelper)
{
    var query = outputHelper.PromptForCustomQuery();

    outputHelper.DisplayScenarioHeader("Custom Query", "User Input");
    outputHelper.DisplayQuery(query);

    var stopwatch = Stopwatch.StartNew();
    Activity? activity = null;

    try
    {
        var result = await outputHelper.WithSpinner(
            "Executing query...",
            async () =>
            {
                activity = Activity.Current;
                return await agent.LookupFeatureAsync(query, CancellationToken.None);
            }
        );

        stopwatch.Stop();
        outputHelper.DisplayResult(result, stopwatch.Elapsed);

        if (activity != null)
        {
            outputHelper.DisplayTraceInfo(activity);
        }
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        outputHelper.DisplayError($"Query execution failed after {stopwatch.Elapsed.TotalSeconds:F2}s", ex);
    }
}

static async Task<FeatureAssessment.Core.Models.FeatureLookupResult> ExecuteScenario(
    IFeatureLookupAgent agent,
    ConsoleOutputHelper outputHelper,
    string category,
    TestScenario scenario)
{
    outputHelper.DisplayScenarioHeader(category, scenario.Name);
    outputHelper.DisplayQuery(scenario.Query);
    outputHelper.DisplayExpectedBehavior(scenario.ExpectedBehavior);

    var stopwatch = Stopwatch.StartNew();
    Activity? activity = null;

    try
    {
        var result = await outputHelper.WithSpinner(
            "Executing...",
            async () =>
            {
                activity = Activity.Current;
                return await agent.LookupFeatureAsync(scenario.Query, CancellationToken.None);
            }
        );

        stopwatch.Stop();
        outputHelper.DisplayResult(result, stopwatch.Elapsed);

        // Note: Activity might be disposed at this point, only show if available
        if (activity != null && !activity.IsAllDataRequested)
        {
            outputHelper.DisplayTraceInfo(activity);
        }

        return result;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        outputHelper.DisplayError($"Scenario failed after {stopwatch.Elapsed.TotalSeconds:F2}s", ex);

        return new FeatureAssessment.Core.Models.FeatureLookupResult
        {
            IsSuccess = false,
            ErrorMessage = ex.Message
        };
    }
}
