using Core.Common.Database.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common.Database.Interfaces
{
    public class DatabaseFactory
    {
        public static IDatabaseHRKProvider Create(string provider)
        {
            return provider switch
            {
                Constants.Common.Constants.Database.SQL_SERVER => new SqlServerProvider(),
                Constants.Common.Constants.Database.POSTGRE_SQL => new PostgreSqlProvider(),
                _ => throw new Exception("Database not supported")
            };
        }
    }
}
