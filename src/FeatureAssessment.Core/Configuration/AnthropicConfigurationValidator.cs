using FluentValidation;

namespace FeatureAssessment.Core.Configuration;

/// <summary>
/// FluentValidation validator for AnthropicConfiguration.
/// Ensures Anthropic API key and other settings are valid before startup.
/// </summary>
public class AnthropicConfigurationValidator : AbstractValidator<AnthropicConfiguration>
{
    public AnthropicConfigurationValidator()
    {
        RuleFor(x => x.ApiKey)
            .NotEmpty()
            .WithMessage("Anthropic API key is required. " +
                        "Set via environment variable (ANTHROPIC_API_KEY), " +
                        "User Secrets (dotnet user-secrets set \"Anthropic:ApiKey\" \"sk-ant-...\"), " +
                        "or local config file (appsettings.Development.local.json).");

        RuleFor(x => x.ApiKey)
            .Must(key => string.IsNullOrEmpty(key) || key.StartsWith("sk-ant-"))
            .When(x => !string.IsNullOrEmpty(x.ApiKey))
            .WithMessage("Anthropic API key should start with 'sk-ant-'. " +
                        "Verify your API key at https://console.anthropic.com");

        RuleFor(x => x.ModelName)
            .NotEmpty()
            .WithMessage("Anthropic model name is required (e.g., 'claude-haiku-4-5').");

        RuleFor(x => x.Temperature)
            .InclusiveBetween(0.0, 1.0)
            .WithMessage("Temperature must be between 0.0 and 1.0.");

        RuleFor(x => x.MaxTokens)
            .GreaterThan(0)
            .WithMessage("MaxTokens must be greater than 0.");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .WithMessage("TimeoutSeconds must be greater than 0.");

        RuleFor(x => x.MaxRetries)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxRetries must be non-negative (0 or more).");
    }
}
