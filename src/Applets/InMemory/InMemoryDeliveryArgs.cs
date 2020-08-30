using System;
using Applets.Common;

namespace Applets.InMemory
{
    sealed class InMemoryDeliveryArgs : DeliveryArgs
    {
        public InMemoryDeliveryArgs(DispatchArgs dispatchArgs, IAppletChannel channel) 
            : base(dispatchArgs.Body, channel)
        {
            if (dispatchArgs == null) throw new ArgumentNullException(nameof(dispatchArgs));
            Intent = dispatchArgs.Intent;
            Correlation = dispatchArgs.Correlation;
            From = dispatchArgs.From;
            Contract = dispatchArgs.Contract;
            Applet = dispatchArgs.Applet;
        }
    }
}
