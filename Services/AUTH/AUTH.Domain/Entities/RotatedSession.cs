using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AUTH.Domain.Entities
{
    public  class RotatedSession
    {
       public  string UserId { get; set; }
       public Guid SessionId { get; set; }
       public  string NewRefreshToken { get; set; }

        public RotatedSession(string userId, Guid sessionId, string newRefreshToken)
        {
            this.UserId = userId;
            this.SessionId = sessionId;
            this.NewRefreshToken = newRefreshToken;
        }
    }


   
}
