using System;
using System.Threading;
using System.Threading.Tasks;

namespace Applets
{
    public interface IDeliveryArgs
    {
        byte[] Body { get; }
        Guid IntentId { get; }
        string IntentName { get; }
        Guid CorrelationId { get; }
        Guid From { get; }
        Guid DataContractId { get; }
        Guid AppletId { get; }
        string AppletName { get; }
        object Dto { get; }
        Task ReplyWithAsync(object dto, CancellationToken cancellation);
        Task ReplyWithAsync(DispatchArgs reply, CancellationToken cancellation);

        bool HasCorrelationId { get; }
    }
}