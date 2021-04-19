using System.Threading;
using System.Threading.Tasks;

namespace Applets
{
    public delegate Task DDeliveryCallback(IEventArgs args, IAppletOutboundChannel channel, CancellationToken cancellation);
}
