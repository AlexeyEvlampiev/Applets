using System;

namespace Applets
{
    public sealed class AppContractViolationException : InvalidOperationException
    {
        public AppContractViolationException()
        {
            
        }

        public AppContractViolationException(string message) : base(message)
        {
            
        }
    }
}
