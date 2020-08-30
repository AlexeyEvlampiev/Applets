using System;
using System.Threading;
using System.Threading.Tasks;

namespace Applets
{
    public interface IDeliveryArgs
    {
        byte[] Body { get; }
        Guid Intent { get; }
        string IntentName { get; }
        Guid Correlation { get; }
        Guid From { get; }
        Guid Contract { get; }
        Guid Applet { get; }
        string AppletName { get; }
        object Dto { get; }
        Task ReplyWithAsync(object dto, CancellationToken cancellation);
        Task ReplyWithAsync(DispatchArgs reply, CancellationToken cancellation);

        bool HasCorrelationId { get; }
    }
}