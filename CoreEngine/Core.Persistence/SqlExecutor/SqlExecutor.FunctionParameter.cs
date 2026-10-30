using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;

namespace Core.Persistence.SqlExecutor;
public sealed partial class SqlExecutor
{
    private sealed class FunctionParameter
    {
        public string? ParameterName { get; set; }
    }
}
