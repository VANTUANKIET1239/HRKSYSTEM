

namespace Core.Common.Entity
{
    namespace MyCompany.Shared.Responses
    {
        public class BaseResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
            public List<string> Errors { get; set; }

            public bool HasErrors => Errors != null && Errors.Count > 0;    

            public static BaseResponse<T> SuccessResponse(T data, string message = null)
            {
                return new BaseResponse<T>
                {
                    Success = true,
                    Message = message ?? "Success.",
                    Data = data,
                    Errors = null
                };
            }

            public static BaseResponse<T> FailResponse(List<string> errors, string message = null)
            {
                return new BaseResponse<T>
                {
                    Success = false,
                    Message = message ?? "Failed.",
                    Data = default,
                    Errors = errors
                };
            }

            public static BaseResponse<T> FailResponse(string error, string message = null)
            {
                return FailResponse(new List<string> { error }, message);
            }
        }
    }

}
