using FeatureAssessment.Core.Tools;

var baseDir = AppContext.BaseDirectory;
var dataDirectory = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "data"));
Console.WriteLine($"Data directory: {dataDirectory}");
Console.WriteLine();

var tools = new FeatureLookupTools(dataDirectory);

Console.WriteLine("Testing PLAT-1687 lookup (should now work)...");
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
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    return 1;
}

Console.WriteLine();
Console.WriteLine("Testing PLAT-1523 lookup (should still work)...");
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

Console.WriteLine();
Console.WriteLine("✓ All tests passed!");
return 0;
