using System;
using System.Threading;
using System.Threading.Tasks;

namespace Applets
{
    public interface IAppletDeliveryProcessor 
    {
        Task ProcessOneAsync(IDeliveryArgs args, CancellationToken cancellation = default);

        Task ProcessOneAsync(Func<IDeliveryArgs> deferredArgs, CancellationToken cancellation = default);

        Task ProcessAsync(CancellationToken cancellation = default);
    }
}
