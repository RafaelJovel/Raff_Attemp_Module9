namespace FeatureAssessment.TestHarness;

/// <summary>
/// Pre-defined test scenarios for demonstrating Feature Lookup Agent behavior.
/// </summary>
public static class TestScenarios
{
    /// <summary>
    /// All test scenarios organized by category.
    /// </summary>
    public static readonly Dictionary<string, List<TestScenario>> Scenarios = new()
    {
        ["Happy Path - Basic Feature Identification"] = new()
        {
            new TestScenario(
                "Query with JIRA key and Production target",
                "Is PLAT-1523 ready for production?",
                "Should identify feature by JIRA key and extract 'Production' environment"
            ),
            new TestScenario(
                "Query with feature name and UAT target",
                "Check maintenance scheduling for UAT",
                "Should match by name 'maintenance scheduling' and extract 'UAT' environment"
            ),
            new TestScenario(
                "Query without explicit environment",
                "Tell me about the QR code check-in feature",
                "Should match by name 'QR code' and default to 'UAT' environment"
            ),
        },

        ["Environment Extraction"] = new()
        {
            new TestScenario(
                "Production target extraction",
                "Can we deploy PLAT-1687 to production?",
                "Should extract 'Production' from query"
            ),
            new TestScenario(
                "UAT target extraction",
                "Is the reservation system ready for UAT testing?",
                "Should extract 'UAT' from query"
            ),
            new TestScenario(
                "Default environment (no mention)",
                "What's the status of PLAT-1677?",
                "Should default to 'UAT' when no environment mentioned"
            ),
        },

        ["Error Handling"] = new()
        {
            new TestScenario(
                "Non-existent feature (JIRA key)",
                "Is PLAT-9999 ready for production?",
                "Should return IsSuccess=false with helpful error message"
            ),
            new TestScenario(
                "Non-existent feature (name)",
                "Check the status of the flying car feature",
                "Should return IsSuccess=false indicating feature not found"
            ),
            new TestScenario(
                "Ambiguous query",
                "What about the feature?",
                "Should either ask for clarification or return error"
            ),
        },

        ["Tool Calling Visibility"] = new()
        {
            new TestScenario(
                "Should call list_all_features first",
                "Is there a feature about contributions?",
                "Agent should list features first to find matches"
            ),
            new TestScenario(
                "Should call get_feature_metadata",
                "Get details for PLAT-1523",
                "Agent should retrieve full metadata after identifying feature"
            ),
        },

        ["Edge Cases"] = new()
        {
            new TestScenario(
                "Partial feature name",
                "What about the QR feature?",
                "Should match 'QR Code Check-in' from partial name"
            ),
            new TestScenario(
                "Case insensitive matching",
                "Is plat-1523 ready?",
                "Should match JIRA key case-insensitively"
            ),
            new TestScenario(
                "Multiple features mentioned",
                "Compare PLAT-1523 and PLAT-1687",
                "May pick first mentioned or ask for clarification"
            ),
        },

        ["Documentation Assessment - Agent Delegation"] = new()
        {
            new TestScenario(
                "Coordinator calls Documentation Specialist",
                "Is PLAT-1523 ready for production? Check the documentation first.",
                "Should show: Coordinator → invokes Documentation Specialist → lists planning docs → reads docs → returns assessment to Coordinator"
            ),
            new TestScenario(
                "Documentation Specialist lists available docs",
                "What planning documents exist for feature1?",
                "Should show: Lists all .md files in feature1/planning/ directory"
            ),
            new TestScenario(
                "Documentation Specialist assesses completeness",
                "Is the USER_STORY complete for feature1?",
                "Should show: Reads USER_STORY.md → reports sections present/missing → no judgment calls"
            ),
        },

        ["Documentation Assessment - Happy Path"] = new()
        {
            new TestScenario(
                "Feature with complete documentation",
                "Check documentation status for PLAT-1523",
                "Should report on USER_STORY.md, DESIGN_DOC.md, DEPLOYMENT_PLAN.md completeness"
            ),
            new TestScenario(
                "Multiple documents assessment",
                "Verify all planning documents for feature2",
                "Should list found docs + report which documents are missing"
            ),
            new TestScenario(
                "Specific document query",
                "What's in the ARCHITECTURE document for feature1?",
                "Should read and summarize ARCHITECTURE.md content"
            ),
        },

        ["Documentation Assessment - Error Handling"] = new()
        {
            new TestScenario(
                "Feature with no documentation",
                "Check planning docs for feature4",
                "Should report 'No planning documents found' gracefully (no crash)"
            ),
            new TestScenario(
                "Missing specific document",
                "Does feature3 have a DEPLOYMENT_PLAN?",
                "Should report document as missing without throwing exception"
            ),
            new TestScenario(
                "Invalid feature ID",
                "Get documentation for nonexistent-feature-xyz",
                "Should report feature not found in planning directory"
            ),
        },
    };

    /// <summary>
    /// Gets all scenarios flattened into a single list.
    /// </summary>
    public static List<(string Category, TestScenario Scenario)> GetAllScenarios()
    {
        return Scenarios
            .SelectMany(kvp => kvp.Value.Select(s => (Category: kvp.Key, Scenario: s)))
            .ToList();
    }

    /// <summary>
    /// Gets scenarios by category name.
    /// </summary>
    public static List<TestScenario> GetScenariosByCategory(string category)
    {
        return Scenarios.TryGetValue(category, out var scenarios) ? scenarios : new List<TestScenario>();
    }
}

/// <summary>
/// Represents a single test scenario.
/// </summary>
/// <param name="Name">Display name for the scenario</param>
/// <param name="Query">The query to send to the agent</param>
/// <param name="ExpectedBehavior">Description of expected behavior</param>
public record TestScenario(
    string Name,
    string Query,
    string ExpectedBehavior
);
