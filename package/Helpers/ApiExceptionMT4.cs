using mt4_term_api;
using Mt4TermApi;
using System;

namespace mt4_term_api
{

    public class ApiExceptionMT4 : Exception
    {
        public Error Error { get; }

        public string? ErrorCode => Error?.ErrorCode;
        public string? ErrorMessageText => Error?.ErrorMessage;
        public ErrorType ErrorType => Error?.Type ?? default;
        public MqlErrorCode MqlErrorCode => Error?.MqlErrorCode ?? default;
        public int MqlErrorIntCode => Error?.MqlErrorIntCode ?? 0;
        public string? MqlErrorDescription => Error?.MqlErrorDescription;
        //public MqlErrorTradeCode MqlErrorTradeCode => Error?.MqlErrorTradeCode ?? default;
        //public int MqlErrorTradeIntCode => Error?.MqlErrorTradeIntCode ?? 0;
        //public string? MqlErrorTradeDescription => Error?.MqlErrorTradeDescription;
        public string? RemoteStackTrace => Error?.StackTrace;
        public string? CommandTypeName => Error?.CommandTypeName;
        public long CommandId => Error?.CommandId ?? 0;

        public ApiExceptionMT4(Error error)
            : base(error?.ErrorMessage)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }

        public override string ToString()
        {
            return $"API Exception: {ErrorCode} - {ErrorMessageText}\n" +
                   $"MQL: {MqlErrorCode} ({MqlErrorIntCode}) - {MqlErrorDescription}\n" +
                   //$"Trade: {MqlErrorTradeCode} ({MqlErrorTradeIntCode}) - {MqlErrorTradeDescription}\n" +
                   $"Command: {CommandTypeName} (ID: {CommandId})\n" +
                   $"Stack: {RemoteStackTrace}";
        }
    }
}