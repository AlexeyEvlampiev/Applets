using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Applets.Common
{
    public abstract class AppletChannel : IAppletChannel
    {
        private readonly IAppInfo _appInfo;
        private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        private readonly EventLoopScheduler _privateEventLoopScheduler = new EventLoopScheduler();
        private readonly IConnectableObservable<IDeliveryArgs> _privateResponses;
        readonly Dictionary<Guid, IObserver<IDeliveryArgs>> _privateResponseObserversByCorrelationId = new Dictionary<Guid, IObserver<IDeliveryArgs>>();
        private int _privateResponsesConnected;
        private readonly Subject<IDeliveryArgs> _pulse = new Subject<IDeliveryArgs>();


        protected AppletChannel(Guid appletId, IAppInfo appInfo)
        {
            _appInfo = appInfo ?? throw new ArgumentNullException(nameof(appInfo));
            if(false == appInfo.IsAppletId(appletId))
                throw new ArgumentException($"Invalid applet ID. Application: {appInfo.ApplicationName}, Applet ID: {appletId}");
            AppletId = appletId;
            Instance = Guid.NewGuid();

            var disposing = _cancellationSource.Token.ToObservable();

            _privateResponses = Observable
                .Defer(CreatePrivateResponsesObservable)
                .TakeUntil(disposing)
                .ObserveOn(_privateEventLoopScheduler)
                .Do(InspectPrivateResponse)
                .Do(_pulse)
                .Where(IsValidResponse)
                .Publish();

            _privateResponses
                .SubscribeOn(_privateEventLoopScheduler)
                .Where(response => _privateResponseObserversByCorrelationId.ContainsKey(response.Correlation))
                .Subscribe(
                    OnPrivateResponse, 
                    OnPrivateResponsesError, 
                    OnPrivateResponsesCompleted);

            Observable
                .Interval(_appInfo.HeartbeatInterval)
                .TakeUntil(disposing)
                .SkipUntil(_pulse)
                .Select(CreateHeartbeatDto)
                .Select(ToDispatchArgs)
                .Subscribe(args =>
                {
                    args.Intent = AppInfo.HeartbeatIntent;
                    SendAsync(args);
                });
        }

        protected abstract IObservable<IDeliveryArgs> CreatePrivateResponsesObservable();

        protected virtual object CreateHeartbeatDto(long sequenceId) => new { sequenceId };


        private void OnPrivateResponse(IDeliveryArgs response)
        {
            if (_privateResponseObserversByCorrelationId.TryGetValue(response.Correlation, out var targetObserver))
            {
                targetObserver.OnNext(response);
            }
        }


        private void OnPrivateResponsesError(Exception ex)
        {
            foreach (var observer in _privateResponseObserversByCorrelationId.Values)
            {
                observer.OnError(ex);
            }
        }

        private void OnPrivateResponsesCompleted()
        {
            foreach (var observer in _privateResponseObserversByCorrelationId.Values)
            {
                observer.OnCompleted();
            }

            _privateResponseObserversByCorrelationId.Clear();
        }

        private bool IsValidResponse(IDeliveryArgs response)
        {
            if (response.From == this.Instance) return false;
            return true;
        }

        private void InspectPrivateResponse(IDeliveryArgs obj)
        {
            
        }

        

        
        public string AppletName => _appInfo.GetAppletName(ApplicationId);


        public Task SendAsync(DispatchArgs args, CancellationToken cancellation = default)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            cancellation.ThrowIfCancellationRequested();
            if (CanSend(args.Intent))
            {
                args.From = this.Instance;
                args.Applet = this.AppletId;
                return this.BroadcastAsync(args, cancellation);
            }

            var intentName = _appInfo.GetIntentName(args.Intent);
            throw new InvalidOperationException(
                new StringBuilder($"The specified outgoing message intent is forbidden for this applet.")
                    .Append($" Sender applet: {AppletName} ({AppletId}).")
                    .Append($" Message intent: {intentName} ({args.Intent}).")
                    .Append($" See {_appInfo.GetType()} type constructor.")
                    .ToString()
            );
        }

        [DebuggerStepThrough]
        public IObservable<IDeliveryArgs> GetResponses(object request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return GetResponses(request, request.GetType().GUID);
        }

        public IObservable<IDeliveryArgs> GetResponses(object request, Guid intent)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var args = ToDispatchArgs(request);
            args.Intent = intent;
            args.Correlation = Guid.NewGuid();
            return GetResponses(args);
        }

        public IObservable<IDeliveryArgs> CreateProcessedEventNotificationsObservable(
            DEventNotificationHandler processOneAsync)
        {
            return Observable
                .Defer(() => RegisterEventNotificationsHandler(processOneAsync))
                .Do(_pulse);
        }

        protected abstract IObservable<IDeliveryArgs> RegisterEventNotificationsHandler(
            DEventNotificationHandler processOneAsync);

        public IDisposable ProcessEventNotifications(DEventNotificationHandler processOneAsync)
        {
            if (processOneAsync == null) throw new ArgumentNullException(nameof(processOneAsync));
            return CreateProcessedEventNotificationsObservable(processOneAsync)
                .Subscribe();
        }



        public IObservable<IDeliveryArgs> GetResponses(DispatchArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (false == args.HasCorrelationId)
                throw new ArgumentException($"{nameof(args)}.{nameof(args.Correlation)} is missing.");

            if (0 == Interlocked.CompareExchange(ref _privateResponsesConnected, 1, 0))
            {
                _privateResponses.Connect();
            }

            return Observable
                .Create<IDeliveryArgs>(Subscribe)
                .SubscribeOn(_privateEventLoopScheduler)
                .Do(InspectAndLog)
                .Where(IsIntentPermitted)
                .ObserveOn(_privateEventLoopScheduler);
                

            IDisposable Subscribe(IObserver<IDeliveryArgs> observer)
            {
                observer = observer.NotifyOn(TaskPoolScheduler.Default);
                var subscription = Disposable.Create(() =>
                    _privateResponseObserversByCorrelationId.Remove(args.Correlation));
                try
                {
                    _privateResponseObserversByCorrelationId.TryAdd(args.Correlation, observer);
                    SendAsync(args);
                    return subscription;
                }
                catch
                {
                    subscription.Dispose();
                    throw;
                }

            }

            bool IsIntentPermitted(IDeliveryArgs reply) =>
                _appInfo.IsExpectedReply(AppletId, args.Intent, reply.Intent);

            void InspectAndLog(IDeliveryArgs reply)
            {
                if (false == IsIntentPermitted(reply))
                {
                    Trace.TraceError(new StringBuilder("Unexpected Fan-Out response.")
                        .Append($" Initiating applet: {AppletName}.")
                        .Append($" Fan-Out request: {_appInfo.GetIntentName(args.Intent)}.")
                        .Append($" Fan-Out response: {reply.IntentName}.")
                        .ToString()
                    );
                }
            }
        }

        public IAppInfo GetAppInfo() => _appInfo;

        protected abstract Task BroadcastAsync(DispatchArgs args, CancellationToken cancellation);

        public Guid AppletId { get; }

        public Guid Instance { get; }

        public Task SendErrorAsync(object data, CancellationToken cancellation = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var args = ToDispatchArgs(data);
            args.Intent = AppInfo.ErrorIntent;
            return SendAsync(args, cancellation);
        }

        public Task SendErrorAsync(string message, CancellationToken cancellation = default)
        {
            if (String.IsNullOrWhiteSpace(message)) throw new ArgumentException($"Message text is required", nameof(message));
            var args = ToDispatchArgs(message);
            args.Intent = AppInfo.ErrorIntent;
            return SendAsync(args, cancellation);
        }

        public Task SendWarningAsync(object data, CancellationToken cancellation = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var args = ToDispatchArgs(data);
            args.Intent = AppInfo.WarningIntent;
            return SendAsync(args, cancellation);
        }

        public Task SendWarningAsync(string message, CancellationToken cancellation = default)
        {
            if (String.IsNullOrWhiteSpace(message)) throw new ArgumentException($"Message text is required", nameof(message));
            var args = ToDispatchArgs(message);
            args.Intent = AppInfo.WarningIntent;
            return SendAsync(args, cancellation);
        }

        public Task SendInfoAsync(object data, CancellationToken cancellation = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var args = ToDispatchArgs(data);
            args.Intent = AppInfo.InfoIntent;
            return SendAsync(args, cancellation);
        }

        public Task SendInfoAsync(string message, CancellationToken cancellation = default)
        {
            if (String.IsNullOrWhiteSpace(message)) throw new ArgumentException($"Message text is required", nameof(message));
            var args = ToDispatchArgs(message);
            args.Intent = AppInfo.InfoIntent;
            return SendAsync(args, cancellation);
        }

        public bool CanSend(Guid intent)
        {
            return _appInfo.CanSend(AppletId, intent);
        }

        public bool CanReceiveEventNotification(Guid intent)
        {
            return _appInfo.CanReceiveEventNotification(AppletId, intent);
        }

        public bool CanSend(DispatchArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            return CanSend(args.Intent);
        }

        public bool CanReceiveEventNotification(IDeliveryArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            return CanReceiveEventNotification(args.Intent) && args.From != this.Instance;
        }


        public Guid ApplicationId => _appInfo.ApplicationId;

        public string ApplicationName => _appInfo.ApplicationName;


        public DispatchArgs ToDispatchArgs(object dto)
        {
            var args = _appInfo.ToDispatchArgs(dto);
            args.Applet = this.AppletId;
            args.From = this.Instance;
            return args;
        }

        protected bool IsSubscribedFor(DeliveryArgs args)
        {
            return _appInfo.CanReceiveEventNotification(AppletId, args.Intent);
        }

        protected virtual void Dispose(bool disposing)
        {
            _cancellationSource.Cancel();
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
