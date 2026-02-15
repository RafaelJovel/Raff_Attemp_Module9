# Task 6 PLAN: Anthropic LLM Support - Planning Summary

## Research Findings (2026-02-15)

### Official Anthropic SDK

**Package Information:**
- **Package Name**: `Anthropic` (version 10+)
- **Official Status**: Maintained directly by Anthropic
- **NuGet**: https://www.nuget.org/packages/Anthropic/
- **Latest Version**: 10+ (as of January 2026)
- **GitHub**: https://github.com/anthropics/anthropic-sdk-csharp

**Key Features:**
- ✅ Full support for `Microsoft.Extensions.AI.Abstractions` IChatClient
- ✅ Built-in `.AsIChatClient()` extension method
- ✅ `.UseFunctionInvocation()` for automatic tool calling
- ✅ Streaming support with `IAsyncEnumerable`
- ✅ .NET Standard 2.0, .NET 8.0, .NET 10.0 support
- ✅ Comprehensive error handling with typed exceptions

**Installation:**
```bash
dotnet add package Anthropic
dotnet add package Microsoft.Extensions.AI.Abstractions
dotnet add package Microsoft.Extensions.AI
```

### Microsoft.Extensions.AI Integration

**Package Information:**
- **Package**: Microsoft.Extensions.AI.Abstractions 10.2.0 (stable)
- **Purpose**: Common abstraction layer for AI services
- **Key Interface**: `IChatClient` - provider-agnostic chat completion

**Integration Pattern:**
```csharp
using Anthropic;
using Microsoft.Extensions.AI;

// Create Anthropic client
var anthropicClient = new AnthropicClient(apiKey);

// Convert to IChatClient with function invocation
IChatClient chatClient = anthropicClient
    .AsIChatClient("claude-haiku-4-5")
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();
```

### Semantic Kernel Integration

**Current Status:**
- ✅ Semantic Kernel can consume `IChatClient` implementations
- ✅ No official `Microsoft.SemanticKernel.Connectors.Anthropic` exists
- ✅ Use `IChatClient` as the integration point
- ✅ Tool/plugin registration works transparently with IChatClient

**Approach:**
1. Create `IChatClient` from Anthropic SDK (or Ollama)
2. Pass `IChatClient` to Semantic Kernel
3. Register plugins/tools with Kernel
4. Agent code remains provider-agnostic

---

## Implementation Approach

### Architecture Overview

```
┌─────────────────────────────────────────────────┐
│           FeatureLookupAgent                    │
│  (Accepts IChatClient, provider-agnostic)       │
└─────────────────┬───────────────────────────────┘
                  │
                  │ Uses
                  ▼
┌─────────────────────────────────────────────────┐
│         IChatClientFactory                      │
│  (Configuration-based provider selection)       │
└─────────────┬───────────────────────────────────┘
              │
              ├─── Provider: Anthropic
              │      │
              │      ▼
              │    ┌─────────────────────────────┐
              │    │  AnthropicClient            │
              │    │  .AsIChatClient()           │
              │    │  .UseFunctionInvocation()   │
              │    └─────────────────────────────┘
              │
              └─── Provider: Ollama
                     │
                     ▼
                   ┌─────────────────────────────┐
                   │  Semantic Kernel            │
                   │  Ollama Connector           │
                   │  → IChatClient wrapper      │
                   └─────────────────────────────┘
```

### Key Design Decisions

1. **Use Official Anthropic SDK** (package: `Anthropic`)
   - Maintained by Anthropic, better long-term support
   - Native IChatClient integration
   - No custom adapters needed

2. **Microsoft.Extensions.AI as Common Layer**
   - `IChatClient` provides provider-agnostic abstraction
   - Both providers expose IChatClient
   - Semantic Kernel consumes IChatClient
   - Agent code never knows about specific provider

3. **Factory Pattern for Provider Selection**
   - `IChatClientFactory` returns `IChatClient`
   - Configuration determines Ollama vs Anthropic
   - Easy to add more providers (Azure OpenAI, AWS Bedrock)

4. **Hybrid API Key Management** (Developer Choice)
   - **Three methods** supported:
     - **Config file**: `appsettings.Development.local.json` (gitignored, easiest)
     - **User Secrets**: `dotnet user-secrets` (most secure, outside repo)
     - **Environment variables**: Production standard
   - **Template file**: `appsettings.Development.template.json` (committed, shows structure)
   - **Configuration hierarchy**: Env vars override User Secrets override local files
   - **Zero commit risk**: All three methods are safe from accidental commits
   - **Developer experience**: Choose method that fits workflow

4. **Default to Anthropic for Production**
   - More deterministic than local LLMs
   - Reliable tool calling support
   - Cost-effective with Haiku model
   - Ollama remains for local development

5. **Hybrid API Key Management**
   - Three methods: config files, User Secrets, env vars
   - Template file for easy developer onboarding
   - Gitignored local config prevents accidental commits
   - Hierarchical config loading (env vars override all)

---

## Implementation Tasks Breakdown

### Phase 1: Configuration & Infrastructure (Files 2-5, 12-16)

**New Configuration Classes:**
- `AnthropicConfiguration.cs` - Settings (API key, model, timeouts)
- `AnthropicConfigurationValidator.cs` - FluentValidation rules
- `LlmProviderConfiguration.cs` - Provider enum and selection
- Update `appsettings.json` structure

**API Key Management (Hybrid Approach):**
- **Three methods** supported (developer's choice):
  1. Local config file (`appsettings.Development.local.json`) - gitignored, easy
  2. .NET User Secrets - stored outside project, most secure
  3. Environment variables - production standard
- **Template file**: `appsettings.Development.template.json` (committed)
- **Configuration hierarchy**: Env vars → User Secrets → `.local.json` → base config
- **Update .gitignore**: Add `*.local.json` patterns
- Fail-fast validation at startup

**Estimated Complexity:** 🟢 Low (config classes straightforward, multiple options improve DX)

---

### Phase 2: Client Factory (Files 5-7)

**New Client Infrastructure:**
- `IChatClientFactory.cs` - Factory interface returning IChatClient
- `ChatClientFactory.cs` - Implementation with provider switching
- `SemanticKernelHelper.cs` - Kernel integration utilities

**Provider Implementations:**
- `CreateAnthropicChatClient()` - Uses official Anthropic SDK
- `CreateOllamaChatClient()` - Wraps existing Ollama connector
- Both return `IChatClient`

**Estimated Complexity:** 🟡 Medium (requires understanding SK + IChatClient integration)

---

### Phase 3: Agent Integration (File 1)

**Modify FeatureLookupAgent:**
- Accept `IChatClient` (not provider-specific client)
- Or accept `IChatClientFactory` and create client
- Remove provider-specific code
- Tool registration remains unchanged

**Estimated Complexity:** 🟢 Low (minimal changes to agent)

---

### Phase 4: Testing (Files 8-11)

**Unit Tests:**
- `AnthropicConfigurationValidatorTests.cs` - Config validation
- `ChatClientFactoryTests.cs` - Factory behavior with mocks

**Integration Tests:**
- `AnthropicEndToEndTests.cs` - Full flow with real Anthropic API
  - Requires `ANTHROPIC_API_KEY` environment variable
  - Mark with `[TestCategory("Integration")]`
  - Test all existing scenarios (feature lookup, tool calling, etc.)
- Update `OllamaEndToEndTests.cs` to ensure no regressions

**Estimated Complexity:** 🟡 Medium (integration tests require API key setup)

---

### Phase 5: Documentation & Test Harness (Files 14-15)

**Documentation Updates:**
- `TESTING.md` - Anthropic setup, API key management, cost info
- `TestHarness/README.md` - Provider switching instructions

**Test Harness Enhancement:**
- Add `--provider` command-line flag
- Display current provider and model
- Support both Ollama and Anthropic in interactive mode

**Estimated Complexity:** 🟢 Low (documentation and harness updates)

---

## Test Strategy Summary

### Unit Tests (Fast, No API Calls)

1. **Configuration Validation**
   - Valid configs pass
   - Missing API key fails with clear error
   - Invalid timeouts/retries fail

2. **Factory Behavior**
   - Creates Ollama client when Provider=Ollama
   - Creates Anthropic client when Provider=Anthropic
   - Throws for unsupported provider
   - CurrentProvider property correct

### Integration Tests (Requires External Services)

3. **Anthropic E2E** (Requires `ANTHROPIC_API_KEY`)
   - Feature lookup by JIRA key
   - Feature lookup by name (fuzzy match)
   - Environment extraction (Production, UAT)
   - Tool calling (list_all_features, get_feature_metadata)
   - Error handling (non-existent feature)
   - Tracing with Anthropic provider

4. **Ollama Regression Tests** (Requires Ollama running)
   - All existing Ollama tests still pass
   - No regressions from adding Anthropic

### Manual Testing (Test Harness)

5. **Interactive Verification**
   - Run harness with `--provider anthropic`
   - Run harness with `--provider ollama`
   - Verify tool calling visible in both
   - Compare response quality

---

## Success Criteria Checklist

**Configuration & Setup:**
- [ ] Configuration supports both Ollama and Anthropic providers
- [ ] Default provider is Anthropic with Claude Haiku 4.5
- [ ] API key loaded from environment variable securely
- [ ] Configuration validation prevents common misconfigurations

**Implementation:**
- [ ] `IChatClientFactory` creates correct client based on config
- [ ] Agent code is provider-agnostic (no if/else for providers)
- [ ] Tool calling works correctly with both providers
- [ ] Semantic Kernel integration functional

**Testing:**
- [ ] All unit tests pass (configuration, factory)
- [ ] Anthropic integration tests pass (with API key)
- [ ] Ollama tests still pass (no regressions)
- [ ] Test harness supports provider selection

**Documentation:**
- [ ] `TESTING.md` updated with Anthropic setup instructions
- [ ] API key management documented (env vars + user secrets)
- [ ] Cost considerations documented
- [ ] Troubleshooting guide complete

**Quality:**
- [ ] All E2E tests pass with Anthropic as default provider
- [ ] Provider switching works without code changes (config only)
- [ ] Zero warnings or errors in build
- [ ] Code formatting passes (`dotnet format --verify-no-changes`)

---

## Risk Assessment

### Low Risk
- ✅ Official SDK with stable IChatClient support
- ✅ Clear integration pattern documented
- ✅ Provider abstraction tested pattern
- ✅ Multiple API key management options reduce setup friction

### Medium Risk
- ⚠️ Ollama IChatClient integration not yet implemented
  - May need custom wrapper
  - Semantic Kernel's support for Ollama + IChatClient unclear
- ⚠️ Cost of integration tests with Anthropic API
  - Mitigated: Use Haiku model (cheap), skip in some CI runs

### Low Risk (Reduced by Hybrid Config)
- ⚠️ Accidental API key commits
  - Mitigated: Template file + .gitignore + User Secrets options
  - Mitigated: Multiple safe methods (`.local.json` gitignored, User Secrets outside repo)
  - Mitigated: Clear documentation of all three methods

### Mitigation Strategies
1. Implement Anthropic first (lower risk)
2. Test Ollama IChatClient integration separately
3. Use test categories to skip expensive integration tests
4. Document known limitations clearly
5. Provide template file for easy, safe local development setup

---

## References & Sources

### Official Documentation
- [Microsoft.Extensions.AI - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)
- [IChatClient Interface - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.ichatclient?view=net-10.0-pp)
- [Anthropic C# SDK - GitHub](https://github.com/anthropics/anthropic-sdk-csharp)

### NuGet Packages
- [Anthropic (official SDK)](https://www.nuget.org/packages/Anthropic/)
- [Microsoft.Extensions.AI.Abstractions 10.2.0](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions/)
- [Microsoft.Extensions.AI 10.2.0](https://www.nuget.org/packages/Microsoft.Extensions.AI/)

### Community Resources
- [Semantic Kernel Anthropic Discussion](https://github.com/microsoft/semantic-kernel/discussions/10335)
- [Unofficial Anthropic.SDK by tghamm](https://github.com/tghamm/Anthropic.SDK)

---

## Next Steps

1. ✅ **PLAN stage complete** - Research findings documented
2. ⏳ **Awaiting user approval** to move to BUILD & ASSESS
3. 📋 **Implementation order** (when approved):
   - Phase 1: Configuration (lowest risk)
   - Phase 2: Client factory (core infrastructure)
   - Phase 3: Agent integration (quick wins)
   - Phase 4: Testing (validation)
   - Phase 5: Documentation (finalization)

---

**Planning Completed**: 2026-02-15
**Status**: Ready for user review and transition to BUILD & ASSESS stage
