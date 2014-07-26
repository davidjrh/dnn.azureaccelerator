using System;

namespace DNNShared.Exceptions
{
    class CompressOperationException
        : Exception
    {
        public CompressOperationException(string message) : base(message) { }
        public CompressOperationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
