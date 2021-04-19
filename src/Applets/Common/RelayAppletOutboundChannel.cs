using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Applets.Common
{
    sealed class RelayAppletOutboundChannel : IAppletOutboundChannel
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private readonly IAppletOutboundChannel _other;

        [DebuggerStepThrough]
        private RelayAppletOutboundChannel(IAppletOutboundChannel other)
        {
            _other = other ?? throw new ArgumentNullException(nameof(other));
        }

        [DebuggerStepThrough]
        public IObservable<IReplyArgs> GetResponses(MessageIntentId intentId, object data, TimeSpan conversationTtl)
        {
            return _other.GetResponses(intentId, data, conversationTtl);
        }

        [DebuggerStepThrough]
        public Task EmitEventAsync(MessageIntentId messageIntentId, object data, TimeSpan? timeToLive = null,
            TimeSpan? enqueueDelay = null, CancellationToken cancellation = default)
        {
            return _other.EmitEventAsync(messageIntentId, data, timeToLive, enqueueDelay, cancellation);
        }

        [DebuggerStepThrough]
        public static IAppletOutboundChannel Wrap(IAppletOutboundChannel other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            return other is RelayAppletOutboundChannel
                ? other
                : new RelayAppletOutboundChannel(other);
        }
    }
}
