using System.Threading;
using System.Threading.Tasks;

namespace Applets
{
    public delegate Task DEventNotificationHandler(IDeliveryArgs args, CancellationToken cancellation);
}
