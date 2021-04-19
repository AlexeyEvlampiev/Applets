using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Applets.Common;

namespace Applets
{
    /// <summary>
    /// Applet specific connection to the application message broker.
    /// </summary>
    public interface IAppletChannel : IAppletOutboundChannel, IDisposable
    {

        /// <summary>
        /// Processes application events received on this connection.
        /// </summary>
        /// <param name="callback">The message handling callback.</param>
        /// <param name="cancellation">The cancellation.</param>
        Task ListenAsync(Func<IEventArgs, Task> callback, CancellationToken cancellation = default);


        public Task ListenAsync(IDeliveryCallback callback, CancellationToken cancellation = default)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            cancellation.ThrowIfCancellationRequested();
            return ListenAsync(args => callback.InvokeAsync(args, this, cancellation), cancellation);
        }

        [DebuggerStepThrough]
        public IDisposable Subscribe(
            Func<IEventArgs, Task> callback,
            CancellationToken cancellation = default)
        {
            return this
                .ListenAsync(callback, cancellation)
                .ToObservable()
                .Subscribe();
        }


        [DebuggerStepThrough]
        public IDisposable Subscribe(
            IDeliveryCallback callback,
            CancellationToken cancellation = default)
        {
            return this
                .ListenAsync(callback, cancellation)
                .ToObservable()
                .Subscribe();
        }


        bool CanSendRequest(MessageIntentId requestIntentId, Type dtoType);
        bool CanAcceptReply(MessageIntentId replyIntentId, Type dtoType);
        bool CanProcessEvent(MessageIntentId eventIntentId, Type dtoType);
        bool CanEmitEvent(MessageIntentId eventIntentId, Type dtoType);

        protected IEnumerable<EventKey> EventSubscriptionKeys { get; }

        public IAppletOutboundChannel AsOutboundChannel() => RelayAppletOutboundChannel.Wrap(this);
    }
}
