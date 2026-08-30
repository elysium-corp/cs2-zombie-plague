using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Menu.Core.Storage;
using Menu.Core.Validation;

namespace Menu.Core.Tests;

public sealed class MenuJsonTests
{
    [Fact]
    public void Canonicalize_SortsObjectPropertiesRecursively()
    {
        using var first = JsonDocument.Parse("""
            {"z":2,"nested":{"b":2,"a":1},"a":1}
            """);
        using var second = JsonDocument.Parse("""
            {"a":1,"nested":{"a":1,"b":2},"z":2}
            """);

        var canonical = MenuJson.Canonicalize(first.RootElement);

        Assert.Equal(MenuJson.Canonicalize(second.RootElement), canonical);
        Assert.Equal(
            "{\"a\":1,\"nested\":{\"a\":1,\"b\":2},\"z\":2}",
            Encoding.UTF8.GetString(canonical));
    }

    [Fact]
    public void ComputeChecksum_IsIndependentOfDictionaryInsertionOrderAndChecksumField()
    {
        var first = TestReleaseFactory.Release() with
        {
            Metadata = new Dictionary<string, JsonElement>
            {
                ["zeta"] = TestReleaseFactory.Json(2),
                ["alpha"] = TestReleaseFactory.Json(1)
            }
        };
        var second = first with
        {
            Checksum = new string('f', 64),
            Metadata = new Dictionary<string, JsonElement>
            {
                ["alpha"] = TestReleaseFactory.Json(1),
                ["zeta"] = TestReleaseFactory.Json(2)
            }
        };

        Assert.Equal(MenuJson.ComputeChecksum(first), MenuJson.ComputeChecksum(second));
    }

    [Fact]
    public void ComputeChecksum_PreservesSemanticArrayOrder()
    {
        var first = TestReleaseFactory.Release(
            menus: [TestReleaseFactory.Menu("first"), TestReleaseFactory.Menu("second")]);
        var reordered = first with { Menus = [first.Menus[1], first.Menus[0]] };

        Assert.NotEqual(MenuJson.ComputeChecksum(first), MenuJson.ComputeChecksum(reordered));
    }

    [Fact]
    public void Validator_RejectsPayloadTamperedAfterChecksum()
    {
        var valid = TestReleaseFactory.Release();
        var tampered = valid with
        {
            Menus =
            [
                valid.Menus[0] with
                {
                    Title = TestReleaseFactory.Text("Tampered")
                }
            ]
        };

        var result = new MenuReleaseValidator().Validate(tampered, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "release.checksum_mismatch");
    }

    [Fact]
    public void DeserializeRelease_RejectsDuplicateJsonProperties()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "menuCoreApiVersion": 1,
              "releaseId": 1,
              "releaseId": 2,
              "generatedAt": "2026-08-30T12:00:00Z",
              "checksum": null,
              "menus": [],
              "commands": [],
              "metadata": {}
            }
            """;

        Assert.Throws<JsonException>(() => MenuJson.DeserializeRelease(json));
    }

    [Fact]
    public async Task Fixture_DeserializesAndValidatesAfterChecksumIsAttached()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "minimal-release.json");
        var json = await File.ReadAllTextAsync(path);
        var fixture = MenuJson.DeserializeRelease(json);

        Assert.NotNull(fixture);
        var checksummed = TestReleaseFactory.WithChecksum(fixture!);
        var result = new MenuReleaseValidator().Validate(checksummed, TestReleaseFactory.Context());

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors.Select(issue => issue.Code)));
        Assert.Equal("fixture-main", checksummed.Menus.Single().MenuKey);
    }

    [Fact]
    public async Task FluteFixture_ValidatesWithItsOriginalChecksum()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "flute-minimal-release.json");
        var release = MenuJson.DeserializeRelease(await File.ReadAllTextAsync(path));

        Assert.NotNull(release);
        Assert.Equal("fd3190be9193c733fd937c86a64e5306a0d82171d78cd0eba87531141e2e239f", release!.Checksum);
        var result = new MenuReleaseValidator().Validate(release, TestReleaseFactory.Context());

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors.Select(issue => issue.Code)));
        Assert.Equal(release.Checksum, MenuJson.ComputeChecksum(release));
    }

    [Fact]
    public async Task FluteCanonicalizationFixture_MatchesBytesAndHash()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "flute-canonicalization.json");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var root = document.RootElement;
        var canonical = MenuJson.Canonicalize(root.GetProperty("input"), omitRootChecksum: true);

        Assert.Equal(root.GetProperty("canonical").GetString(), Encoding.UTF8.GetString(canonical));
        Assert.Equal(
            root.GetProperty("checksum").GetString(),
            Convert.ToHexStringLower(SHA256.HashData(canonical)));
    }
}
