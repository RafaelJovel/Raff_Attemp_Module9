using Microsoft.Extensions.Options;

namespace FeatureAssessment.Core.Configuration;

/// <summary>
/// Validates OllamaConfiguration at application startup.
/// Ensures configuration values are valid before the application runs.
/// </summary>
public class OllamaConfigurationValidator : IValidateOptions<OllamaConfiguration>
{
    public ValidateOptionsResult Validate(string? name, OllamaConfiguration options)
    {
        var errors = new List<string>();

        // Validate Endpoint is a valid URI
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            errors.Add("Endpoint cannot be empty.");
        }
        else if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            errors.Add($"Endpoint '{options.Endpoint}' must be a valid HTTP or HTTPS URL.");
        }

        // Validate ModelName is not empty
        if (string.IsNullOrWhiteSpace(options.ModelName))
        {
            errors.Add("ModelName cannot be empty.");
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
