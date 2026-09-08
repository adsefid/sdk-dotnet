using System.Security.Cryptography;
using Adsefid.Sdk.Tests.Infrastructure;
using Xunit;

namespace Adsefid.Sdk.Tests;

/// <summary>
/// The golden fixtures are byte-identical copies of the same tree in the sibling SDK repositories.
/// One that drifts here silently weakens every test that reads it, so verify the manifest.
/// </summary>
public sealed class FixturesIntegrityTests
{
    public static TheoryData<string, string> ManifestEntries()
    {
        var data = new TheoryData<string, string>();
        foreach (var (sha256, name) in Fixtures.Manifest())
        {
            data.Add(name, sha256);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ManifestEntries))]
    public void FixtureMatchesItsChecksum(string name, string expectedSha256)
    {
        var actual = Convert.ToHexString(SHA256.HashData(Fixtures.Bytes(name))).ToLowerInvariant();

        Assert.Equal(expectedSha256, actual);
    }

    [Fact]
    public void NoFixtureIsMissingFromTheManifest()
    {
        var listed = Fixtures.Manifest().Select(entry => entry.Name).OrderBy(name => name, StringComparer.Ordinal);

        var onDisk = Directory
            .EnumerateFiles(Fixtures.Directory, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(Fixtures.Directory, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(name => name != "CHECKSUMS.txt")
            .OrderBy(name => name, StringComparer.Ordinal);

        Assert.Equal(listed, onDisk);
    }
}
