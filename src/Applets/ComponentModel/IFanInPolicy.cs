using System;

namespace Applets.ComponentModel
{
    public interface IFanInPolicy<T>
    {
        bool TryCompleteWith(IDeliveryArgs reply);
        T Result { get; }
        TimeSpan Timeout { get; }
    }
}
