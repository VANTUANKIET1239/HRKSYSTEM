using Core.Persistence.Database.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Persistence.Database.Interfaces
{
    public class DatabaseFactory
    {
        public static IDatabaseHRKProvider Create(string provider)
        {
            return provider switch
            {
                Core.Common.Constants.Common.Constants.Database.SQL_SERVER => new SqlServerProvider(),
                Core.Common.Constants.Common.Constants.Database.POSTGRE_SQL => new PostgreSqlProvider(),
                _ => throw new Exception("Database not supported")
            };
        }
    }
}
