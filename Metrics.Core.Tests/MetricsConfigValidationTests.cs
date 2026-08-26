using Metrics.Core.Config;

namespace Metrics.Core.Tests;

public sealed class MetricsConfigValidationTests
{
    [Fact]
    public void TryValidate_WithConfiguredDefaults_ReturnsTrue()
    {
        var config = CreateValidConfig();

        var valid = MetricsConfigValidation.TryValidate(config, out var error);

        Assert.True(valid, error);
    }

    [Fact]
    public void TryValidate_WhenEstimatedBatchExceedsFluteLimit_ReturnsFalse()
    {
        var config = CreateValidConfig();
        config.BatchSize = 100;
        config.MaxEventBytes = 16_384;

        var valid = MetricsConfigValidation.TryValidate(config, out var error);

        Assert.False(valid);
        Assert.Contains("1 MiB", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryBuildIngestionUri_PreservesBasePath()
    {
        var valid = MetricsConfigValidation.TryBuildIngestionUri(
            "https://example.test/flute",
            out var uri
        );

        Assert.True(valid);
        Assert.Equal("https://example.test/flute/api/metrics/v1/events", uri.AbsoluteUri);
    }

    private static MetricsConfig CreateValidConfig()
    {
        return new MetricsConfig
        {
            Enabled = true,
            ApiSecret = "emx_1_example_secret_for_unit_tests_only_1234567890"
        };
    }
}
