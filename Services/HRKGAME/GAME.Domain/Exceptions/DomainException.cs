using System;

namespace GAME.Domain.Exceptions
{
    /// <summary>
    /// Ngoại lệ đại diện cho các vi phạm quy tắc nghiệp vụ hoặc bất biến trong Domain Model.
    /// </summary>
    public class DomainException : Exception
    {
        public string? ReasonCode { get; }

        public DomainException(string message) : base(message)
        {
        }

        public DomainException(string reasonCode, string message) : base(message)
        {
            ReasonCode = reasonCode;
        }
    }
}
