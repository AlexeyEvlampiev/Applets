using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Applets.Common
{
    public abstract class AppletChannel : IAppletChannel
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _disposed = 0;


        public Applet Applet { get; }
        public IAppContract AppContract { get; }

        [DebuggerStepThrough]
        protected AppletChannel(AppletId appletId) 
            : this(appletId, new AppContractNullObject())
        {
            
        }

        protected AppletChannel(AppletId appletId, IAppContract appAppContract)
        {
            AppContract = appAppContract ?? throw new ArgumentNullException(nameof(appAppContract));
            Applet = AppContract.GetApplet(appletId ?? throw new ArgumentNullException(nameof(appletId)));
        }


        protected abstract IObservable<IReplyArgs> GetResponses(MessageIntentId intentId, object data, TimeSpan conversationTtl);
        protected abstract Task EmitEventAsync(MessageIntentId messageIntentId, object data, TimeSpan? timeToLive = null, TimeSpan? enqueueDelay = null, CancellationToken cancellation = default);
        protected abstract Task ProcessEventLogAsync(Func<IEventArgs, Task> callback, CancellationToken cancellation = default);


        [DebuggerNonUserCode]
        protected bool IsDisposed => (Thread.VolatileRead(ref _disposed) > 0);

        [DebuggerNonUserCode]
        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException($"{Applet.Name} applet channel");
            }
        }

        public bool CanSendRequest(MessageIntentId requestIntentId, Type dtoType)
        {
            if (requestIntentId == null) return false;
            if (dtoType == null) return false;
            return AppContract.CanBroadcastRequest(Applet, requestIntentId, dtoType);
        }

        public bool CanAcceptReply(MessageIntentId replyIntentId, Type dtoType)
        {
            if (replyIntentId == null) return false;
            if (dtoType == null) return false;
            return AppContract.CanAcceptReply(Applet, replyIntentId, dtoType);
        }

        public bool CanProcessEvent(MessageIntentId eventIntentId, Type dtoType)
        {
            if (eventIntentId == null) return false;
            if (dtoType == null) return false;
            return AppContract.HasSubscription(Applet, eventIntentId, dtoType);
        }

        [DebuggerStepThrough]
        public bool CanProcessEvent(IDeliveryArgs args)
        {
            if (args is null) return false;
            return CanProcessEvent(args.MessageIntent, args.Data.GetType());
        }

        public bool CanEmitEvent(MessageIntentId eventIntentId, Type dtoType)
        {
            if (eventIntentId == null) return false;
            if (dtoType == null) return false;
            return AppContract.CanEmitEvent(Applet, eventIntentId, dtoType);
        }

        IEnumerable<EventKey> IAppletChannel.EventSubscriptionKeys => throw new NotImplementedException();

        //[DebuggerStepThrough]
        IObservable<IReplyArgs> IAppletOutboundChannel.GetResponses(MessageIntentId intentId, object data, TimeSpan conversationTtl)
        {
            ThrowIfDisposed();
            if (intentId == null) throw new ArgumentNullException(nameof(intentId));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (conversationTtl.Ticks < 1) throw new ArgumentOutOfRangeException(nameof(conversationTtl));
            if (AppContract.CanBroadcastRequest(Applet.Id, intentId, data.GetType()))
            {
                return this
                    .GetResponses(intentId, data, conversationTtl)
                    .Take(conversationTtl);
            }

            throw new AppContractViolationException($"Request broadcast is forbidden. Applet: {Applet}, intent: {AppContract.GetIntent(intentId)}, requestType: {data.GetType()}");
        }

        [DebuggerStepThrough]
        Task IAppletOutboundChannel.EmitEventAsync(MessageIntentId messageIntentId, object data, TimeSpan? timeToLive, TimeSpan? enqueueDelay, CancellationToken cancellation)
        {
            ThrowIfDisposed();
            if (messageIntentId == null) throw new ArgumentNullException(nameof(messageIntentId));
            if (data == null) throw new ArgumentNullException(nameof(data));
            cancellation.ThrowIfCancellationRequested();
            if (AppContract.CanEmitEvent(Applet.Id, messageIntentId, data.GetType()))
            {
                return this.EmitEventAsync(messageIntentId, data, timeToLive, enqueueDelay, cancellation);
            }

            throw new AppContractViolationException();
        }

        //[DebuggerStepThrough]
        Task IAppletChannel.ListenAsync(Func<IEventArgs, Task> callback, CancellationToken cancellation)
        {
            ThrowIfDisposed();
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            cancellation.ThrowIfCancellationRequested();
            if (AppContract.IsEventSubscriber(Applet.Id))
            {
                return this.ProcessEventLogAsync(callback, cancellation);
            }

            throw new AppContractViolationException();
        }


        Task IAppletChannel.ListenAsync(IDeliveryCallback callbacksMap, CancellationToken cancellation)
        {
            if (callbacksMap == null) throw new ArgumentNullException(nameof(callbacksMap));
            cancellation.ThrowIfCancellationRequested();
            var self = (IAppletChannel)this;

            var handlersByEventKey = new Dictionary<EventKey, DDeliveryCallback>();
            var requiredEventKeys = AppContract.GetEventKeys(Applet.Id)?.ToHashSet() 
                                    ?? throw new NullReferenceException($"{nameof(AppContract)}.{nameof(AppContract.GetEventKeys)}");
            foreach (var key in callbacksMap.Keys)
            {
                if (self.CanProcessEvent(key.EventIntentId, key.DtoType))
                {
                    var handler = callbacksMap.GetHandler(key);
                    handlersByEventKey.Add(key, handler);
                }
                else
                {
                    var intent = AppContract.GetIntent(key.EventIntentId);
                    throw new AppContractViolationException(
                        new StringBuilder("Redundant event handler.")
                            .Append($"Applet: {Applet}. Event intent: {intent}. Event DTO type: {key.DtoType}")
                        .ToString());
                }
            }

            var missingHandlers = requiredEventKeys
                .Except(callbacksMap.Keys)
                .Select(key => new
                {
                    Intent = AppContract.GetIntent(key.EventIntentId),
                    DtoType = key.DtoType
                })
                .Select(item=> $"event intent: {item.Intent}, DTO type: {item.DtoType}")
                .ToList();

            if (missingHandlers.Any() && false == (AppContract is AppContractNullObject))
            {
                throw new AppContractViolationException(
                    new StringBuilder("Missing event handlers.")
                        .Append($"Applet: {Applet}. Missing handlers: {string.Join(";", missingHandlers)}")
                        .ToString());
            }

            return self.ListenAsync(args =>
            {
                var key = new EventKey(args.MessageIntent, args.Data.GetType());
                if (handlersByEventKey.TryGetValue(key, out var handler))
                {
                    return handler.Invoke(args, this, cancellation);
                }

                return Task.CompletedTask;
            }, cancellation);
        }



        [DebuggerStepThrough]
        protected bool CanProcessReply(IDeliveryArgs args)
        {
            return args != null &&
                   AppContract.CanAcceptReply(Applet, args);
        }



        [DebuggerStepThrough]
        protected bool SentByAnotherApplet(IDeliveryArgs args) => (args.SenderApplet.Id != this.Applet.Id);

        public override string ToString() => Applet.ToString();

        public override int GetHashCode() => Applet.GetHashCode();

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {

            }

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AppletChannel()
        {
            Dispose(false);
        }
    }
}
