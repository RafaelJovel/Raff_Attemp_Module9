using Microsoft.VisualStudio.TestTools.UnitTesting;

// Quick test to see what Assert methods are available
public class TestAssertAPI
{
    public void TestMethod()
    {
        var dict = new Dictionary<string, object>();
        
        // Try these potential signatures based on MSTEST0037 suggestions:
        // Assert.HasCount(dict, 1);
        // Assert.IsEmpty(dict);
        // Assert.Contains("substring", "string");
    }
}
