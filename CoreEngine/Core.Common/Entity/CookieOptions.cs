using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common.Entity
{
    public class HRKCookieOptions
    {
        public bool HttpOnly { get; set; } = true;  

        public bool Secure { get; set; } = true;

        public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;
        public bool IsEssential { get; set; } = false;

      
    }
}
