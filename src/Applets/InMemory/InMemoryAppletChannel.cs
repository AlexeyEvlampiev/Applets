using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Applets.Common;

namespace Applets.InMemory
{
    class InMemoryAppletChannel : AppletChannel
    {
        private readonly Subject<InMemoryDeliveryArgs> _topic;

        public InMemoryAppletChannel(Guid appletId, IAppInfo appInfo, Subject<InMemoryDeliveryArgs> topic) 
            : base(appletId, appInfo)
        {
            _topic = topic ?? throw new ArgumentNullException(nameof(topic));
        }

        protected override IObservable<IDeliveryArgs> CreatePrivateResponsesObservable()
        {
            return _topic
                .Where(args => args.HasCorrelationId && args.From != this.Instance)
                .ObserveOn(TaskPoolScheduler.Default);
        }


        protected override IObservable<IDeliveryArgs> RegisterEventNotificationsHandler(DEventNotificationHandler processOneAsync)
        {
            if (processOneAsync == null) throw new ArgumentNullException(nameof(processOneAsync));
            var cancellation = new CancellationTokenSource();
            return _topic
                .Do(args => { }, cancellation.Cancel)
                .Where(CanReceiveEventNotification)
                .SelectMany(async args =>
                {
                    await processOneAsync(args, cancellation.Token);
                    return args;
                });

        }

        protected override Task BroadcastAsync(DispatchArgs args, CancellationToken cancellation)
        {
            _topic.OnNext(new InMemoryDeliveryArgs(args, this));
            return Task.CompletedTask;
        }
    }
}
