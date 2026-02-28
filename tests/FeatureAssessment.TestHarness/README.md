# Feature Lookup Agent - Manual Testing Harness

Interactive console application for manually testing and validating the Feature Lookup Agent behavior.

## Quick Start

```bash
# From repository root
cd tests/FeatureAssessment.TestHarness

# Run the harness
dotnet run
```

## Prerequisites

### Ollama Setup

1. **Install Ollama**
   - Download from: https://ollama.com/download
   - Verify installation: `ollama --version`

2. **Pull Required Model**
   ```bash
   ollama pull llama3.1:8b
   ```

3. **Start Ollama Service**
   ```bash
   ollama serve
   ```
   - Default endpoint: `http://localhost:11434`
   - Verify: `curl http://localhost:11434/api/tags`

### Data Setup

The harness reads from `data/incoming/` directory (repository root). Ensure sample features exist:
- `data/incoming/feature1/` - Maintenance Scheduling (PLAT-1523)
- `data/incoming/feature2/` - QR Code Check-in (PLAT-1524)
- `data/incoming/feature3/` - Reservation System (PLAT-1525)
- `data/incoming/feature4/` - Contribution Tracking (PLAT-1526)

## Features

### 1. Run All Scenarios
Executes all pre-defined test scenarios across all categories:
- Happy Path - Basic Feature Identification (3 scenarios)
- Environment Extraction (3 scenarios)
- Error Handling (3 scenarios)
- Tool Calling Visibility (2 scenarios)
- Edge Cases (3 scenarios)

**Total: 14 scenarios**

### 2. Run Scenarios by Category
Select a specific category and run only those scenarios.

### 3. Run Single Scenario
Choose a category, then select a specific scenario to execute.

### 4. Enter Custom Query
Type your own natural language query to test the agent interactively.

### 5. Show Configuration
Display current Ollama configuration settings.

## Configuration

Edit `appsettings.json` to customize settings:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ModelName": "llama3.1:8b",
    "TimeoutSeconds": 60,
    "MaxRetries": 3
  }
}
```

**Configuration Options:**
- `Endpoint`: Ollama API endpoint (default: `http://localhost:11434`)
- `ModelName`: Model to use (recommended: `llama3.1:8b`)
- `TimeoutSeconds`: LLM call timeout (default: 60 seconds)
- `MaxRetries`: Number of retry attempts on transient failures (default: 3)

## Model Requirements

### Recommended Model: `llama3.1:8b`

**Why llama3.1:8b?**
- ✅ Proven tool calling support
- ✅ Good balance of speed and accuracy
- ✅ Reliable function parameter extraction
- ✅ Consistently generates valid JSON responses

**Download:**
```bash
ollama pull llama3.1:8b
```

### Alternative Models

**Supported:**
- `llama3.2:latest` - Newer version, may have improvements
- `llama3.3:latest` - If available, test first
- `llama3.1:70b` - More capable but requires 40GB+ VRAM

**NOT Recommended:**
- ❌ `qwen2.5` family - Limited tool calling support
- ❌ Models < 7B parameters - May not reliably call tools
- ❌ Non-instruction-tuned models - Poor structured output

**Hardware Requirements:**
- **8B models**: 8GB RAM/VRAM minimum, 16GB recommended
- **70B models**: 40GB+ VRAM required

## Expected Behavior with Local LLMs

### Normal Variability

**This is NORMAL:**
- Results may vary slightly between runs
- `temperature=0` reduces but doesn't eliminate variation
- Response phrasing may differ (semantics should be consistent)
- Occasional JSON parsing issues (LLM returns malformed response)

**What "Working" Looks Like:**
- ✅ Tools are called (you'll see tool invocations in output)
- ✅ Feature is identified (`IsSuccess=true`)
- ✅ Metadata populated (`feature_key`, `feature_id`, `target_environment`)
- ✅ Execution completes within timeout

### Known Limitations

1. **Edge Case Inconsistency**
   - Non-existent features may produce varying error messages
   - Ambiguous queries may be handled differently each run
   - This is expected behavior with local LLMs

2. **JSON Parsing Failures**
   - Occasionally the LLM returns malformed JSON
   - Retry the same query - usually succeeds on 2nd attempt
   - More common with smaller models or complex queries

3. **Tool Calling Reliability**
   - llama3.1:8b has ~95% tool calling success rate
   - If tools are never called, check model compatibility
   - Verify you're using an instruction-tuned model

## Interpreting Results

### Success Response

```
Status: ✓ Success
Feature Key: PLAT-1523
Feature ID: feature1
Target Environment: Production
Execution Time: 3.45s
```

**Indicates:**
- Agent successfully parsed query
- Feature was found in `data/incoming/`
- Environment correctly extracted
- Tool calls executed successfully

### Failure Response

```
✗ Failed

Feature not found: No feature matching 'XYZ-9999' exists

Execution Time: 2.10s
```

**Possible Causes:**
- Feature doesn't exist in `data/incoming/`
- Query was too ambiguous
- JSON parsing error (retry)
- Tool calling failed (check model)

### Trace Information

```
Trace Information:
  Trace ID: a1b2c3d4e5f6...
  Span ID: 1234567890ab...
  Duration: 3452.18ms
  Tags:
    query: Is PLAT-1523 ready for production?
    feature_key: PLAT-1523
    target_environment: Production
    is_success: True
```

**Shows:**
- Complete execution trace
- Span attributes captured
- Duration breakdown
- Diagnostic information

## Sample Session Walkthrough

### Step 1: Start the Harness

```bash
dotnet run --project tests/FeatureAssessment.TestHarness
```

**Expected Output:**
```
  _____ _____    _  _____ _   _ ____  _____   _     ___   ___  _  ___   _ ____
 |  ___| ____|  / \|_   _| | | |  _ \| ____| | |   / _ \ / _ \| |/ / | | |  _ \
 | |_  |  _|   / _ \ | | | | | | |_) |  _|   | |  | | | | | | | ' /| | | | |_) |
 |  _| | |___ / ___ \| | | |_| |  _ <| |___  | |__| |_| | |_| | . \| |_| |  __/
 |_|   |_____/_/   \_\_|  \___/|_| \_\_____| |_____\___/ \___/|_|\_\\___/|_|

Manual Testing Harness for Feature Lookup Agent

┌─────────────────────────────────────────────┐
│        Current Configuration                 │
├───────────────────────┬─────────────────────┤
│      Setting          │        Value        │
├───────────────────────┼─────────────────────┤
│ Ollama Endpoint       │ http://localhost... │
│ Model Name            │ llama3.1:8b         │
│ Timeout (seconds)     │ 60                  │
│ Max Retries           │ 3                   │
└───────────────────────┴─────────────────────┘
```

### Step 2: Choose an Option

```
What would you like to do?
❯ Run all scenarios
  Run scenarios by category
  Run single scenario
  Enter custom query
  Show configuration
  Exit
```

### Step 3: View Results

**Example: Custom Query**

```
───── Custom Query: User Input ─────────────────────────────────

Query: Is PLAT-1523 ready for production?

⠋ Executing query...

┌────────────────────────────────────┐
│ Status          │ ✓ Success        │
│ Feature Key     │ PLAT-1523        │
│ Feature ID      │ feature1         │
│ Target Enviro.. │ Production       │
│ Execution Time  │ 3.42s            │
└────────────────────────────────────┘
```

## Troubleshooting

### Tools Never Called

**Symptoms:**
- Agent completes but `IsSuccess=false`
- No tool invocations visible in output
- Error: "Could not determine feature"

**Solutions:**
1. **Check Ollama is running**
   ```bash
   curl http://localhost:11434/api/tags
   ```
   Expected: JSON response with available models

2. **Verify model is available**
   ```bash
   ollama list
   ```
   Expected: `llama3.1:8b` in the list

3. **Check model supports tool calling**
   - Use `llama3.1:8b` (proven support)
   - Avoid qwen2.5 models (limited support)
   - Ensure instruction-tuned variant

4. **Review logs for errors**
   - Check console output for exceptions
   - Look for connection errors to Ollama
   - Verify endpoint is correct (no `/v1` suffix)

### Parsing Failures

**Symptoms:**
- Error: "Failed to parse agent response"
- JSON deserialization errors
- Intermittent failures

**Solutions:**
1. **Retry the query**
   - LLMs occasionally produce malformed JSON
   - Usually succeeds on 2nd attempt

2. **Switch to llama3.1:8b**
   - Best JSON generation reliability
   - Properly follows tool calling schema

3. **Check temperature setting**
   - Should be 0 for maximum determinism
   - Verify in agent configuration

### Slow Responses

**Symptoms:**
- Queries take >30 seconds
- High CPU usage during execution
- System becomes sluggish

**Solutions:**
1. **Model too large for hardware**
   - 8B models need ~8GB RAM minimum
   - 70B models need 40GB+ VRAM
   - Use smaller model or add more RAM

2. **CPU vs GPU inference**
   - Ollama uses GPU if available (much faster)
   - CPU-only inference is slower but works
   - Check: `nvidia-smi` or `rocm-smi` for GPU usage

3. **Increase timeout**
   - Edit `appsettings.json`: `"TimeoutSeconds": 120`
   - Useful for slower hardware

### Connection Errors

**Symptoms:**
- "Connection refused" errors
- "Ollama endpoint not reachable"
- Timeout before LLM responds

**Solutions:**
1. **Verify endpoint format**
   - Should be: `http://localhost:11434`
   - NO `/v1` suffix for Ollama connector
   - Check `appsettings.json`

2. **Check Ollama service status**
   ```bash
   # Windows
   Get-Service ollama

   # Linux/Mac
   systemctl status ollama
   ```

3. **Review firewall settings**
   - Ensure localhost:11434 is accessible
   - Check for blocking software (antivirus, firewall)

4. **Restart Ollama**
   ```bash
   # Stop
   pkill ollama  # or Stop-Service ollama

   # Start
   ollama serve
   ```

## Advanced Usage

### Custom Scenarios

Edit `TestScenarios.cs` to add your own test scenarios:

```csharp
["My Custom Category"] = new()
{
    new TestScenario(
        "My scenario name",
        "Is my feature ready?",
        "Should identify 'my feature' and extract environment"
    ),
}
```

### Trace Export

To export traces to a file or external system, modify `Program.cs`:

```csharp
// Add OTLP exporter
.WithTracing(builder => builder
    .AddSource(ActivitySources.FeatureLookup.Name)
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    }))
```

### Verbose Logging

Increase log verbosity in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "FeatureAssessment": "Trace"
    }
  }
}
```

## Next Steps

After manual verification:
1. Review results to ensure agent behavior is correct
2. Document any unexpected behavior or edge cases
3. Consider adding new test scenarios for coverage gaps
4. Use trace information to identify performance bottlenecks

## Support

For issues or questions:
- Check this README's Troubleshooting section
- Review `TESTING.md` in repository root
- Check Ollama documentation: https://ollama.com/docs
- Open issue in project repository
