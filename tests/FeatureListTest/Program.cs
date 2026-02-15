using FeatureAssessment.Core.Tools;

var dataDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data"));
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

if (features.Count < 4)
{
    Console.WriteLine("\nExpected 4 features! Checking directory structure...");
    var incomingDir = Path.Combine(dataDirectory, "incoming");
    if (Directory.Exists(incomingDir))
    {
        var dirs = Directory.GetDirectories(incomingDir, "feature*");
        Console.WriteLine($"Found {dirs.Length} feature directories:");
        foreach (var dir in dirs)
        {
            var dirName = Path.GetFileName(dir);
            Console.WriteLine($"  - {dirName}");
            var jiraFile = Path.Combine(dir, "jira", "feature_issue.json");
            Console.WriteLine($"    JIRA file exists: {File.Exists(jiraFile)}");

            if (File.Exists(jiraFile))
            {
                try
                {
                    var content = await File.ReadAllTextAsync(jiraFile);
                    var hasKey = content.Contains("\"key\"");
                    var hasFields = content.Contains("\"fields\"");
                    Console.WriteLine($"    Has 'key': {hasKey}, Has 'fields': {hasFields}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Error reading file: {ex.Message}");
                }
            }
        }
    }
}
