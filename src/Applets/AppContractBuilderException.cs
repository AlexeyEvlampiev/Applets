using System;
using System.Text;

namespace Applets
{
    public sealed class AppContractBuilderException : Exception
    {
        internal AppContractBuilderException(string message) : base(message)
        {
            
        }

        internal AppContractBuilderException(StringBuilder message) : base(message.ToString())
        {
            
        }
    }
}
