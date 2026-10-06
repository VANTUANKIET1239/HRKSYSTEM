using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Core.TransactionalMessaging.Persistence;

internal static partial class SqlIdentifier
{
    public static string FromEntity<TEntity>(DbContext db)
    {
        var entity = db.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException(
                $"Entity '{typeof(TEntity).Name}' is not mapped in {db.GetType().Name}.");
        var table = entity.GetTableName()
            ?? throw new InvalidOperationException(
                $"Entity '{typeof(TEntity).Name}' is not mapped to a table.");

        return Qualify(entity.GetSchema() ?? "dbo", table);
    }

    public static string Qualify(string schema, string table)
    {
        Validate(schema, nameof(schema));
        Validate(table, nameof(table));
        return $"[{schema}].[{table}]";
    }

    private static void Validate(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || !ValidIdentifier().IsMatch(value))
        {
            throw new InvalidOperationException(
                $"Transactional messaging {parameterName} '{value}' is not a valid SQL identifier.");
        }
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidIdentifier();
}
