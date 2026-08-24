using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common.Database.Options
{
    public class DatabaseOptions
    {
        public string Provider { get; set; } = "";
        public string ConnectionString { get; set; } = "";

        public bool EnableSqlLogging { get; set; }

        public bool EnableSensitiveDataLogging { get; set; }

        public int SlowQueryMilliseconds { get; set; } = 500;

        public int CommandTimeout { get; set; } = 30;

        public int RetryCount { get; set; } = 5;

        public int MaxRetryDelay { get; set; } = 30;
    }
}
