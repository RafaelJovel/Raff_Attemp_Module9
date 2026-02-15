using Microsoft.Extensions.Options;

namespace FeatureAssessment.Core.Configuration;

/// <summary>
/// Validates AnthropicConfiguration at application startup.
/// Ensures API key and configuration values are valid before the application runs.
/// </summary>
public class AnthropicOptionsValidator : IValidateOptions<AnthropicConfiguration>
{
    public ValidateOptionsResult Validate(string? name, AnthropicConfiguration options)
    {
        var errors = new List<string>();

        // Validate API Key
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            errors.Add("Anthropic API key is required. " +
                      "Set via environment variable (ANTHROPIC_API_KEY), " +
                      "User Secrets (dotnet user-secrets set \"Anthropic:ApiKey\" \"sk-ant-...\"), " +
                      "or local config file (appsettings.Development.local.json).");
        }
        else if (!options.ApiKey.StartsWith("sk-ant-"))
        {
            errors.Add("Anthropic API key should start with 'sk-ant-'. " +
                      "Verify your API key at https://console.anthropic.com");
        }

        // Validate ModelName is not empty
        if (string.IsNullOrWhiteSpace(options.ModelName))
        {
            errors.Add("Anthropic model name is required (e.g., 'claude-haiku-4-5').");
        }

        // Validate Temperature is between 0.0 and 1.0
        if (options.Temperature < 0.0 || options.Temperature > 1.0)
        {
            errors.Add($"Temperature must be between 0.0 and 1.0, but was {options.Temperature}.");
        }

        // Validate MaxTokens is positive
        if (options.MaxTokens <= 0)
        {
            errors.Add($"MaxTokens must be greater than 0, but was {options.MaxTokens}.");
        }

        // Validate TimeoutSeconds is positive
        if (options.TimeoutSeconds <= 0)
        {
            errors.Add($"TimeoutSeconds must be greater than 0, but was {options.TimeoutSeconds}.");
        }

        // Validate MaxRetries is non-negative
        if (options.MaxRetries < 0)
        {
            errors.Add($"MaxRetries must be 0 or greater, but was {options.MaxRetries}.");
        }

        if (errors.Count > 0)
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }
}
