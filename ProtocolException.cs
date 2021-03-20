using System;

namespace Proxycord
{
    public class ProtocolException : Exception
    {
        public ProtocolException()
        {
        }

        public ProtocolException(string? message) : base(message)
        {
        }

        public ProtocolException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
