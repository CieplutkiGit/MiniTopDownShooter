using System;

namespace Application.Economy
{
    public sealed class EconomyOperationResult
    {
        public bool IsSuccess { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }

        private EconomyOperationResult(bool isSuccess, string errorCode, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public static EconomyOperationResult Success()
        {
            return new EconomyOperationResult(true, null, null);
        }

        public static EconomyOperationResult Failure(string errorCode, string errorMessage)
        {
            return new EconomyOperationResult(false, errorCode, errorMessage);
        }
    }
}
