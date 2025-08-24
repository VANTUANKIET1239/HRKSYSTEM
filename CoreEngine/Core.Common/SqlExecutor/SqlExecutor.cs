
namespace Core.Common.SqlExecutor
{
    public interface ISqlExecutor
    {
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null);
        Task<int> ExecuteAsync(string sql, object parameters = null);
    }
    public class SqlExecutor : ISqlExecutor
    {
        private readonly IDbConnection _connection;

        public SqlExecutor(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null)
        {
            return await _connection.QueryAsync<T>(sql, parameters);
        }

        public async Task<int> ExecuteAsync(string sql, object parameters = null)
        {
            return await _connection.ExecuteAsync(sql, parameters);
        }

        public async Task<IEnumerable<T>> QueryStoredProcAsync<T>(string procName, object parameters = null)
        {
            return await _connection.QueryAsync<T>(
                procName,
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }   
    }
}
