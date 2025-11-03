using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common.Constants.Common
{
    public static partial class Constants
    {
        public class CORE_CONSTANTS
        {
            public const string DefaultConnection = "DefaultConnection";
            public const string JWT = "JwtSettings";
        }
        public class HRK_API
        {
            public const string API_KEY = "HRK-API-KEY";
        }
        public class JSON_WEB_TOKEN
        {
            public const string SESSIONID = "HRK-SESSION-ID";
            public const string USERID = "HRK-USER-ID";
            public const string JWT = "HRK-JWT-TOKEN";
            public const string REFRESHTOKEN = "HRK-JWT-REFRESHTOKEN";
        }

        public class RABBITMQ
        {
            public const string RABBITMQ_OPTIONS = "RabbitMq";
        }

        public class Cookie
        {
            public const string COOKIE_OPTONS = "CookieOptions";
        }

    }
}
