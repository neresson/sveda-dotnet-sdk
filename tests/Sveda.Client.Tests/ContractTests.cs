using System.Text.Json;
using Xunit;

namespace Sveda.Client.Tests;

public sealed class ContractTests
{
    [Fact]
    public void LocksSidecarContractSurface()
    {
        var path = FindSidecarContract();

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        Assert.Equal("1.0", root.GetProperty("version").GetString());
        Assert.Equal("/sveda", root.GetProperty("prefix").GetString());
        Assert.Equal(
            "application/vnd.sveda.stream+json",
            root.GetProperty("accept").GetProperty("svedaStream").GetString()
        );
    }

    private static string FindSidecarContract()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            foreach (var relative in new[]
            {
                Path.Combine("contracts", "sidecar.v1.json"),
                Path.Combine("packages", "protocol", "contracts", "sidecar.v1.json"),
                Path.Combine("sveda", "packages", "protocol", "contracts", "sidecar.v1.json"),
            })
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("sidecar.v1.json");
    }
}
