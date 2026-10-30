using System.Data;
using Core.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Core.Modules.Tests;
public partial class SqlExecutorIntegrationTests
{
    public sealed class FunctionRow
    {
        public int Value { get; set; }
        public string? Label { get; set; }
    }
}
