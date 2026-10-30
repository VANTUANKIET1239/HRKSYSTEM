using Core.Persistence.DependencyInjection;
using Core.Persistence.Repositories;
using Core.Persistence.SqlExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Core.Modules.Tests;
public partial class SqlExecutorTests
{
    public sealed class ProbeContext(DbContextOptions<ProbeContext> options) : DbContext(options);
}
