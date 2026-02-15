using FeatureAssessment.Core.Models;
using System.Text.Json;

namespace FeatureAssessment.Core.Tests.Models;

[TestClass]
public class AssessmentStateTests
{
    [TestMethod]
    public void FromFeatureLookupResult_WithSuccessfulResult_CreatesStateCorrectly()
    {
        // Arrange
        var lookupResult = new FeatureLookupResult
        {
            FeatureId = "feature1",
            FeatureKey = "PLAT-1523",
            TargetEnvironment = "Production",
            IsSuccess = true
        };

        // Act
        var state = AssessmentState.FromFeatureLookupResult(lookupResult);

        // Assert
        Assert.AreEqual("feature1", state.FeatureId);
        Assert.AreEqual("PLAT-1523", state.FeatureKey);
        Assert.AreEqual("Production", state.TargetEnvironment);
        Assert.AreEqual("feature_lookup_completed", state.CurrentStage);
        Assert.IsTrue(state.IsFeatureIdentified);
        Assert.IsNull(state.ErrorMessage);
    }

    [TestMethod]
    public void FromFeatureLookupResult_WithFailedResult_SetsErrorState()
    {
        // Arrange
        var lookupResult = new FeatureLookupResult
        {
            IsSuccess = false,
            ErrorMessage = "Feature not found"
        };

        // Act
        var state = AssessmentState.FromFeatureLookupResult(lookupResult);

        // Assert
        Assert.IsNull(state.FeatureId);
        Assert.IsNull(state.FeatureKey);
        Assert.AreEqual("error", state.CurrentStage);
        Assert.IsFalse(state.IsFeatureIdentified);
        Assert.AreEqual("Feature not found", state.ErrorMessage);
    }

    [TestMethod]
    public void FromFeatureLookupResult_WithPartialResult_HandlesGracefully()
    {
        // Arrange
        var lookupResult = new FeatureLookupResult
        {
            FeatureKey = "PLAT-1523",
            FeatureId = null,  // Only JIRA key found, not feature ID
            TargetEnvironment = "UAT",
            IsSuccess = true
        };

        // Act
        var state = AssessmentState.FromFeatureLookupResult(lookupResult);

        // Assert
        Assert.IsNull(state.FeatureId);
        Assert.AreEqual("PLAT-1523", state.FeatureKey);
        Assert.AreEqual("UAT", state.TargetEnvironment);
        Assert.IsTrue(state.IsFeatureIdentified);
    }

    [TestMethod]
    public void WithStage_UpdatesStageCorrectly()
    {
        // Arrange
        var state = new AssessmentState
        {
            FeatureId = "feature1",
            CurrentStage = "feature_lookup_completed"
        };

        // Act
        var updatedState = state.WithStage("coordinator");

        // Assert
        Assert.AreEqual("coordinator", updatedState.CurrentStage);
        Assert.AreEqual("feature1", updatedState.FeatureId);
        // Verify immutability - original unchanged
        Assert.AreEqual("feature_lookup_completed", state.CurrentStage);
    }

    [TestMethod]
    public void WithMetadata_AddsNewMetadataEntry()
    {
        // Arrange
        var state = new AssessmentState
        {
            FeatureId = "feature1"
        };

        // Act
        var updatedState = state.WithMetadata("start_time", DateTime.UtcNow);

        // Assert
        Assert.HasCount(1, updatedState.Metadata);
        Assert.IsTrue(updatedState.Metadata.ContainsKey("start_time"));
        // Verify immutability - original unchanged
        Assert.IsEmpty(state.Metadata);
    }

    [TestMethod]
    public void WithMetadata_UpdatesExistingMetadataEntry()
    {
        // Arrange
        var state = new AssessmentState
        {
            FeatureId = "feature1",
            Metadata = new Dictionary<string, object>
            {
                ["counter"] = 1
            }
        };

        // Act
        var updatedState = state.WithMetadata("counter", 2);

        // Assert
        Assert.HasCount(1, updatedState.Metadata);
        Assert.AreEqual(2, updatedState.Metadata["counter"]);
        // Verify immutability - original unchanged
        Assert.AreEqual(1, state.Metadata["counter"]);
    }

    [TestMethod]
    public void WithMetadata_PreservesOtherMetadata()
    {
        // Arrange
        var state = new AssessmentState
        {
            FeatureId = "feature1",
            Metadata = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };

        // Act
        var updatedState = state.WithMetadata("key3", "value3");

        // Assert
        Assert.HasCount(3, updatedState.Metadata);
        Assert.AreEqual("value1", updatedState.Metadata["key1"]);
        Assert.AreEqual("value2", updatedState.Metadata["key2"]);
        Assert.AreEqual("value3", updatedState.Metadata["key3"]);
    }

    [TestMethod]
    public void Record_SupportsWithSyntax()
    {
        // Arrange
        var state = new AssessmentState
        {
            FeatureId = "feature1",
            FeatureKey = "PLAT-1523",
            CurrentStage = "feature_lookup_completed"
        };

        // Act
        var updatedState = state with
        {
            CurrentStage = "coordinator",
            IsFeatureIdentified = true
        };

        // Assert
        Assert.AreEqual("coordinator", updatedState.CurrentStage);
        Assert.IsTrue(updatedState.IsFeatureIdentified);
        Assert.AreEqual("feature1", updatedState.FeatureId);
        Assert.AreEqual("PLAT-1523", updatedState.FeatureKey);
        // Verify immutability - original unchanged
        Assert.AreEqual("feature_lookup_completed", state.CurrentStage);
        Assert.IsFalse(state.IsFeatureIdentified);
    }

    [TestMethod]
    public void State_CanBeSerializedToJson()
    {
        // Arrange
        var state = new AssessmentState
        {
            FeatureId = "feature1",
            FeatureKey = "PLAT-1523",
            CurrentStage = "coordinator",
            TargetEnvironment = "Production",
            IsFeatureIdentified = true,
            Metadata = new Dictionary<string, object>
            {
                ["start_time"] = "2026-02-14T10:00:00Z"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(state);
        var deserialized = JsonSerializer.Deserialize<AssessmentState>(json);

        // Assert
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(state.FeatureId, deserialized.FeatureId);
        Assert.AreEqual(state.FeatureKey, deserialized.FeatureKey);
        Assert.AreEqual(state.CurrentStage, deserialized.CurrentStage);
        Assert.AreEqual(state.TargetEnvironment, deserialized.TargetEnvironment);
        Assert.AreEqual(state.IsFeatureIdentified, deserialized.IsFeatureIdentified);
    }

    [TestMethod]
    public void State_DefaultMetadataIsEmptyDictionary()
    {
        // Arrange & Act
        var state = new AssessmentState();

        // Assert
        Assert.IsNotNull(state.Metadata);
        Assert.IsEmpty(state.Metadata);
    }

    [TestMethod]
    public void State_AllowsNullableFields()
    {
        // Arrange & Act
        var state = new AssessmentState
        {
            FeatureId = null,
            FeatureKey = null,
            CurrentStage = null,
            TargetEnvironment = null,
            ErrorMessage = null
        };

        // Assert
        Assert.IsNull(state.FeatureId);
        Assert.IsNull(state.FeatureKey);
        Assert.IsNull(state.CurrentStage);
        Assert.IsNull(state.TargetEnvironment);
        Assert.IsNull(state.ErrorMessage);
        Assert.IsFalse(state.IsFeatureIdentified);
    }
}
