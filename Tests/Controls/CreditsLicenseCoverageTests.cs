using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Grex.Tests.Controls
{
    /// <summary>
    /// Guards the third-party license manifest: every package the GUI resolves must be
    /// documented in Assets/third-party-licenses.json or explicitly excluded, and the
    /// manifest itself must be internally consistent.
    /// </summary>
    public class CreditsLicenseCoverageTests
    {
        // Build/test-only packages that are never shipped in the redistributed GUI artifact.
        private static readonly HashSet<string> KnownBuildOnlyExclusions = new(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.Windows.SDK.BuildTools",
            "Microsoft.Windows.SDK.BuildTools.MSIX",
            "Microsoft.NET.Test.Sdk",
            "xunit",
            "xunit.runner.visualstudio",
            "coverlet.collector",
            "Moq",
            "FluentAssertions",
            "Microsoft.Xaml.Behaviors.Wpf",
        };

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Grex.csproj")) &&
                    File.Exists(Path.Combine(dir.FullName, "Assets", "third-party-licenses.json")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(
                "Could not locate repo root (expected Grex.csproj + Assets/third-party-licenses.json by walking up from the test assembly).");
        }

        private static JsonElement LoadManifest(string repoRoot)
        {
            var path = Path.Combine(repoRoot, "Assets", "third-party-licenses.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.Clone();
        }

        [Fact]
        public void Manifest_IsInternallyValid()
        {
            var root = LoadManifest(FindRepoRoot());

            root.GetProperty("schemaVersion").GetInt32().Should().Be(1, "schemaVersion must be 1");

            var licenses = root.GetProperty("licenses");
            var components = root.GetProperty("components");

            licenses.EnumerateObject().Any().Should().BeTrue("licenses map must not be empty");
            components.GetArrayLength().Should().BeGreaterThan(0, "components array must not be empty");

            foreach (var lic in licenses.EnumerateObject())
            {
                lic.Value.GetString().Should().NotBeNullOrWhiteSpace(
                    $"license text for key '{lic.Name}' must be non-empty");
            }

            foreach (var c in components.EnumerateArray())
            {
                c.GetProperty("name").GetString().Should().NotBeNullOrWhiteSpace();
                c.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
                c.GetProperty("url").GetString().Should().NotBeNullOrWhiteSpace();

                var category = c.GetProperty("category").GetString();
                category.Should().BeOneOf("library", "platform");

                var key = c.GetProperty("license").GetString();
                key.Should().NotBeNullOrWhiteSpace();
                licenses.TryGetProperty(key!, out _).Should().BeTrue(
                    $"component '{c.GetProperty("name").GetString()}' references license key '{key}' which must exist in licenses");
            }
        }

        [Fact]
        public void EveryResolvedPackage_IsDocumentedOrExcluded()
        {
            var repoRoot = FindRepoRoot();
            var assets = Path.Combine(repoRoot, "obj", "project.assets.json");
            Assert.SkipUnless(File.Exists(assets),
                "project.assets.json not found — run dotnet build grex.sln -p:Platform=x64 first.");

            using var doc = JsonDocument.Parse(File.ReadAllText(assets));
            var resolved = doc.RootElement.GetProperty("libraries").EnumerateObject()
                .Where(p => p.Value.TryGetProperty("type", out var t) && t.GetString() == "package")
                .Select(p => p.Name.Split('/')[0])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var root = LoadManifest(repoRoot);
            var documented = root.GetProperty("components").EnumerateArray()
                .Select(c => c.GetProperty("name").GetString()!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var undocumented = resolved
                .Where(id => !documented.Contains(id) && !KnownBuildOnlyExclusions.Contains(id))
                .OrderBy(id => id)
                .ToList();

            undocumented.Should().BeEmpty(
                "every package the GUI build resolves must be documented in Assets/third-party-licenses.json " +
                "or listed in KnownBuildOnlyExclusions. Undocumented: " + string.Join(", ", undocumented) +
                ". Add each to the JSON with verbatim license text, or (if build/test-only and not redistributed) " +
                "to KnownBuildOnlyExclusions with a documented reason.");
        }
    }
}
