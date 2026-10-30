using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Persistence.Repositories;
using GAME.Infrastructure.Data;
using Xunit;

namespace GAME.Domain.Tests;
public partial class GameAssetPathTests
{
    public sealed class AssetRow
    {
        public int Id { get; set; }
        public string? Avatar { get; set; }
        public string? ImagePath { get; set; }
        public string? BackgroundPath { get; set; }
        public string? Icon { get; set; }
        public string? BattleJson { get; set; }
        public byte[]? ImageData { get; set; }
    }
}
