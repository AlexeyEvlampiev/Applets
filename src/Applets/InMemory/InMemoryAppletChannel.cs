using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Applets.Common;

namespace Applets.InMemory
{
    sealed class InMemoryAppletChannel : AppletChannel
    {
        private readonly IObserver<InMemoryDeliveryArgs> _publicTopic;
        private readonly IObservable<InMemoryDeliveryArgs> _privateQueue;
        private readonly IDataContractSerializer _serializer;
        private readonly IScheduler _privateScheduler = new EventLoopScheduler();


        [DebuggerStepThrough]
        internal InMemoryAppletChannel(
            AppletId appletId, 
            ISubject<InMemoryDeliveryArgs> subject, 
            IAppContract appContract,
            IDataContractSerializer serializer) : base(appletId, appContract)
        {
            _publicTopic = (subject ?? throw new ArgumentNullException(nameof(subject))).AsObserver();
            _privateQueue = subject
                .Where(SentByAnotherApplet)
                .Where(CanProcess)
                .Where(IsAlive)
                .Do(_=>{}, ex=> Dispose(), Dispose)
                .ObserveOn(_privateScheduler);
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            subject.Subscribe(_ => { }, ex=> Dispose(), Dispose);
            if (IsDisposed)
            {
                throw new ArgumentException("The subject is already completed", nameof(subject));
            }
        }

        private bool IsAlive(InMemoryDeliveryArgs arg)
        {
            if (arg.ExpirationUtc < DateTime.UtcNow) return false;
            return true;
        }

        private bool CanProcess(InMemoryDeliveryArgs arg)
        {
            if (CanProcessEvent(arg)) return true;
            return arg.SessionId != Guid.Empty && CanProcessReply(arg);
        }

        [DebuggerStepThrough]
        internal InMemoryAppletChannel(AppletId appletId, IAppContract appAppContract, ISubject<InMemoryDeliveryArgs> topic, IDataContractSerializer serializer)
            : base(appletId, appAppContract)
        {
            _publicTopic = topic ?? throw new ArgumentNullException(nameof(topic));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _publicTopic.OnCompleted();
            }

            base.Dispose(disposing);
        }


        protected override IObservable<IReplyArgs> GetResponses(MessageIntentId intentId, object data, TimeSpan conversationTtl)
        {
            var intent = AppContract.GetIntent(intentId);
            var request = new InMemoryDeliveryArgs(Applet, intent, data, _serializer, AppContract)
            {
                SessionId = Guid.NewGuid(),
                TimeToLive = conversationTtl
            };

            return Observable.Create<IReplyArgs>(observer =>
            {
                var subscription = _privateQueue
                    .Take(conversationTtl)
                    .Where(response =>
                        response.SessionId == request.SessionId)
                    .ObserveOn(_privateScheduler)
                    .Subscribe(observer);
                _publicTopic.OnNext(request);
                return subscription;
            });
        }

        protected override Task EmitEventAsync(
            MessageIntentId messageIntentId, 
            object data, 
            TimeSpan? timeToLive = null, 
            TimeSpan? enqueueDelay = null, 
            CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            var intent = AppContract.GetIntent(messageIntentId);
            var args = new InMemoryDeliveryArgs(Applet, intent, data, _serializer, AppContract)
            {
                TimeToLive = timeToLive
            };
            if (enqueueDelay.HasValue)
            {
                Observable
                    .Timer(enqueueDelay.Value)
                    .TakeWhile(_=> false == IsDisposed)
                    .Subscribe(_ => _publicTopic.OnNext(args));
            }
            else
            {
                _publicTopic.OnNext(args);
            }
            return Task.CompletedTask;
        }

        protected override async Task ProcessEventLogAsync(Func<IEventArgs, Task> callback, CancellationToken cancellation = default)
        {
            await _privateQueue
                .Select(receivedArgs =>
                {
                    var clone = receivedArgs.Clone();
                    clone.ReplyMessageFactory = (intent, data) =>
                        new InMemoryDeliveryArgs(Applet, intent, data, _serializer, AppContract)
                        {
                            SessionId = receivedArgs.SessionId
                        };
                    clone.ReplyHandler = replyArgs =>
                    {
                        replyArgs.SessionId = receivedArgs.SessionId;
                        _publicTopic.OnNext(replyArgs);
                    };
                    return clone;
                })
                .SelectMany(args => callback(args).ToObservable())
                .ToTask(cancellation);
        }
    }
}
