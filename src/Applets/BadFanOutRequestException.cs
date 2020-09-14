using System;

namespace Applets
{
    public sealed class BadFanOutRequestException : Exception
    {
        public IDeliveryArgs DeliveryArgs { get; }

        internal BadFanOutRequestException(IDeliveryArgs deliveryArgs)
        {
            DeliveryArgs = deliveryArgs ?? throw new ArgumentNullException(nameof(deliveryArgs));
            if(false == deliveryArgs.IsError)
                throw new ArgumentException($"Given delivery args is not an error reply");
        }
    }
}
