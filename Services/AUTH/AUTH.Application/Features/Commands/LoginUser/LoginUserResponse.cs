using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AUTH.Application.Features.Commands.LoginUser
{

    public class LoginUserResponse
    {
        public string UserId { get; set; }  
        public string UserName { get; set; }    
        public string UserEmail { get; set; }   
        public string? AccessToken { get; set; }
        public DateTime? AccessTokenExpiresAt { get; set; }
    }

}
