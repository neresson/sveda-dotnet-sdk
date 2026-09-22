using System.Text.Json;
using Xunit;

namespace Sveda.Client.Tests;

public sealed class ContractTests
{
    [Fact]
    public void LocksSidecarContractSurface()
    {
        var path = Path.Combine("contracts", "sidecar.v1.json");
        if (!File.Exists(path))
        {
            path = Path.Combine("..", "..", "..", "..", "sveda", "packages", "protocol", "contracts", "sidecar.v1.json");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        Assert.Equal("1.0", root.GetProperty("version").GetString());
        Assert.Equal("/sveda", root.GetProperty("prefix").GetString());
        Assert.Equal(
            "application/vnd.sveda.stream+json",
            root.GetProperty("accept").GetProperty("svedaStream").GetString()
        );
    }
}
