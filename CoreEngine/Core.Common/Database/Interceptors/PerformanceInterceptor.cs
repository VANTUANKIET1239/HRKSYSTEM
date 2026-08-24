using Core.Common.Database.Options;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data.Common;

public class PerformanceInterceptor : DbCommandInterceptor
{
    private readonly ILogger<PerformanceInterceptor> _logger;
    private readonly DatabaseOptions _options;

    public PerformanceInterceptor(
        ILogger<PerformanceInterceptor> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }
    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        Log(command, eventData);

        return base.ReaderExecuted(command, eventData, result);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        Log(command, eventData);

        return base.ScalarExecuted(command, eventData, result);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        Log(command, eventData);

        return base.NonQueryExecuted(command, eventData, result);
    }

    private void Log(
        DbCommand command,
        CommandExecutedEventData eventData)
    {
        var elapsed = eventData.Duration.TotalMilliseconds;

        if (elapsed > _options.SlowQueryMilliseconds)
        {
            _logger.LogWarning(
                """
                Slow Query Detected

                Duration : {Duration} ms

                SQL :
                {Sql}
                """,
                elapsed,
                command.CommandText);
        }
    }
}