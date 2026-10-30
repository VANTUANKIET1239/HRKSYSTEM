using System.Text.Json;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class LeaderboardGatewayTests
{
    [Theory]
    [InlineData("Ocelot.json")]
    [InlineData("Ocelot.docker.json")]
    public void BothEnvironmentsForwardTheListAndPublicInspectionPaths(string filename)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "ApiGateway", "ApiGateway"))) dir = dir.Parent;
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "ApiGateway", "ApiGateway", filename)));
        foreach (var suffix in new[] { "", "/{everything}" })
        {
            var route = Assert.Single(json.RootElement.GetProperty("Routes").EnumerateArray(),
                x => x.GetProperty("UpstreamPathTemplate").GetString() == "/gateway/dcs-game/leaderboards" + suffix);
            Assert.Equal("/api/leaderboards" + suffix, route.GetProperty("DownstreamPathTemplate").GetString());
            Assert.Equal("Get", Assert.Single(route.GetProperty("UpstreamHttpMethod").EnumerateArray()).GetString());
            Assert.Equal("Bearer", route.GetProperty("AuthenticationOptions").GetProperty("AuthenticationProviderKey").GetString());
        }
    }
}
