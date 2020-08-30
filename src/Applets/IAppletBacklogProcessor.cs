using System;
using System.Threading;
using System.Threading.Tasks;
using Applets.Common;

namespace Applets
{
    public interface IAppletBacklogProcessor 
    {
        Task ProcessOneAsync(DeliveryArgs args, CancellationToken cancellation);

        Task ProcessOneAsync(Func<DeliveryArgs> deferredArgs, CancellationToken cancellation);

        Task ProcessAsync(CancellationToken cancellation);
    }
}
