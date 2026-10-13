using System;
using Core.Common.Errors;

namespace GAME.Domain.Exceptions
{
    /// <summary>
    /// Ngoại lệ đại diện cho các vi phạm quy tắc nghiệp vụ hoặc bất biến trong Domain Model.
    /// </summary>
    public class DomainException : BusinessException
    {
        public string? ReasonCode { get; }

        public DomainException(string message) : base("DOMAIN_RULE_VIOLATION", message)
        {
        }

        public DomainException(string reasonCode, string message) : base(reasonCode, message)
        {
            ReasonCode = reasonCode;
        }
    }
}
