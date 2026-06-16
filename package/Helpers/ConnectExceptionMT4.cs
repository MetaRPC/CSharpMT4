
using System;

namespace mt4_term_api
{
    /// <summary>
    /// Exception thrown when there is a connection issue with the MT4 terminal.
    /// </summary>
    /// <remarks>
    /// This exception is used to indicate that the MT4 terminal could not be connected to.
    /// </remarks>
    [Serializable]
    public class ConnectExceptionMT4 : Exception
    {
        public ConnectExceptionMT4()
        {
        }

        public ConnectExceptionMT4(string message) : base(message)
        {
        }

        public ConnectExceptionMT4(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}