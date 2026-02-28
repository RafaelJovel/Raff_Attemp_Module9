using FeatureAssessment.Core.Tools;

var dataDirectory = Path.GetFullPath("data");
Console.WriteLine($"Data directory: {dataDirectory}");
Console.WriteLine($"Incoming directory: {Path.Combine(dataDirectory, "incoming")}");
Console.WriteLine($"Directory exists: {Directory.Exists(Path.Combine(dataDirectory, "incoming"))}");
Console.WriteLine();

var tools = new FeatureLookupTools(dataDirectory);

Console.WriteLine("Calling ListAllFeaturesAsync...");
var features = await tools.ListAllFeaturesAsync();

Console.WriteLine($"\nFound {features.Count} features:");
foreach (var feature in features)
{
    Console.WriteLine($"  - {feature.FeatureId}: {feature.JiraKey} - {feature.Summary}");
}

if (features.Count == 0)
{
    Console.WriteLine("\nNo features found! Checking directory structure...");
    var incomingDir = Path.Combine(dataDirectory, "incoming");
    if (Directory.Exists(incomingDir))
    {
        var dirs = Directory.GetDirectories(incomingDir, "feature*");
        Console.WriteLine($"Found {dirs.Length} feature directories:");
        foreach (var dir in dirs)
        {
            Console.WriteLine($"  - {dir}");
            var jiraFile = Path.Combine(dir, "jira", "feature_issue.json");
            Console.WriteLine($"    JIRA file exists: {File.Exists(jiraFile)}");
        }
    }
}
