using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Core.Persistence.Repositories;
using GAME.Infrastructure.Data;
using GAME.Application.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;
public partial class UnitOfWorkTransactionTests
{
    private sealed class Probe
    {
        public int Id { get; set; }
    }
}
