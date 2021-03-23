using Applets.Common;

namespace Applets.InMemory
{
    sealed class InMemoryDeliveryArgs : DeliveryArgs
    {
        public InMemoryDeliveryArgs(DispatchArgs dispatchArgs, IAppletChannel channel) 
            : base(dispatchArgs, channel)
        {
            
        }

    }
}
