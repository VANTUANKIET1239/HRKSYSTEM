using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Web.Cookies
{
    using Core.Web.Cookies;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System;

    public enum Expiration {
        Minute = 1,
        Hour = 2,
        Day = 3
    }   

}
