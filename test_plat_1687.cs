using FeatureAssessment.Core.Tools;

var dataDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data"));
Console.WriteLine($"Data directory: {dataDirectory}");

var tools = new FeatureLookupTools(dataDirectory);

Console.WriteLine("\nTesting PLAT-1687 lookup (should now work)...");
try
{
    var feature = await tools.GetFeatureMetadataAsync("PLAT-1687");
    Console.WriteLine($"✓ SUCCESS: Found {feature.Key} - {feature.Fields.Summary}");
    Console.WriteLine($"  Assignee: {feature.Fields.Assignee?.DisplayName} (Active: {feature.Fields.Assignee?.Active})");
    Console.WriteLine($"  Reporter: {feature.Fields.Reporter?.DisplayName} (Active: {feature.Fields.Reporter?.Active})");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ FAILED: {ex.Message}");
    return 1;
}

Console.WriteLine("\nTesting PLAT-1523 lookup (should still work)...");
try
{
    var feature = await tools.GetFeatureMetadataAsync("PLAT-1523");
    Console.WriteLine($"✓ SUCCESS: Found {feature.Key} - {feature.Fields.Summary}");
    Console.WriteLine($"  Assignee: {feature.Fields.Assignee?.DisplayName} (Active: {feature.Fields.Assignee?.Active})");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ FAILED: {ex.Message}");
    return 1;
}

Console.WriteLine("\n✓ All tests passed!");
return 0;
