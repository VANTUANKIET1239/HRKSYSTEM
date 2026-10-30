using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AUTH.Application.Interfaces
{
    public class IdentityUserDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
    }
}
