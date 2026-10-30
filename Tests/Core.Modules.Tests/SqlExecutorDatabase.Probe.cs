using System.Data;
using Core.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Core.Modules.Tests;
public sealed partial class SqlExecutorDatabase
{
    public sealed class Probe
    {
        public Guid Id { get; set; }
        public string? Label { get; set; }
        public decimal Amount { get; set; }
    }
}
