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
            IntentId = dispatchArgs.IntentId;
            CorrelationId = dispatchArgs.CorrelationId;
            From = dispatchArgs.From;
            DataContractId = dispatchArgs.DataContractId;
            AppletId = dispatchArgs.AppletId;
        }
    }
}
