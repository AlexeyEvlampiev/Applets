using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Applets.ComponentModel;

namespace Applets.Common
{
    public abstract class AppletDeliveryProcessor : IAppletDeliveryProcessor
    {
        private readonly DEventNotificationHandler _generalizedMessageHandler;

        protected AppletDeliveryProcessor(IAppletChannel channel)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _generalizedMessageHandler = CreateGeneralizedMessageHandler(channel);
        }


        public IAppletChannel Channel { get; }

        [DebuggerStepThrough]
        public Task ProcessOneAsync(IDeliveryArgs args, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            return _generalizedMessageHandler.Invoke(args, cancellation);
        }

        public async Task ProcessOneAsync(Func<IDeliveryArgs> deferredArgs, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var args = deferredArgs.Invoke();
            await _generalizedMessageHandler.Invoke(args, cancellation);
            Channel.Pulse();
        }

        public async Task ProcessAsync(CancellationToken cancellation)
        {
            await Channel
                .CreateProcessedEventNotificationsObservable(_generalizedMessageHandler)
                .TakeUntil(cancellation.ToObservable());

        }

        private DEventNotificationHandler CreateGeneralizedMessageHandler(IAppletChannel channel)
        {
            var handlerResolverByIntent = 
                GetMethodInfosByIntentLookup()
                    .ToDictionary(
                        item=> item.Key, 
                        item=> new MessageHandlerFactory(
                            this, 
                            item.ToArray(), 
                            ProcessUnmappedMessageAsync));

            Task RoutedProcessOneAsync(IDeliveryArgs args, CancellationToken cancellation)
            {
                if (handlerResolverByIntent.TryGetValue(args.IntentId, out var router))
                {
                    var handler = router.GetHandler(args.Dto) 
                                  ?? throw new NullReferenceException(
                                      $"{nameof(router)}.{nameof(router.GetHandler)} returned null");
                    cancellation.ThrowIfCancellationRequested();
                    return handler.Invoke(args, cancellation);
                }

                return ProcessUnmappedMessageAsync(args, cancellation);
            }

            return RoutedProcessOneAsync;
        }


        ILookup<Guid, MethodInfo> GetMethodInfosByIntentLookup()
        {
            var instanceMethods = GetType()
                    .GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic).ToList();

            var processUnmappedMessageAsyncMethodInfo =
                (from mi in instanceMethods
                where mi.Name == nameof(this.ProcessUnmappedMessageAsync)
                      let parameters = mi.GetParameters()
                where parameters.Length == 2 &&
                      parameters[0].ParameterType == typeof(IDeliveryArgs) &&
                      parameters[1].ParameterType == typeof(CancellationToken) &&
                      mi.ReturnType == typeof(Task)
                select mi).Single();

            var candidateMethods =
                (from mi in instanceMethods.Where(mi => mi != processUnmappedMessageAsyncMethodInfo)
                 let parameters = mi.GetParameters()
                 where parameters.Length >= 1
                 where parameters.Count(p => p.ParameterType == typeof(IDeliveryArgs)) ==1
                 let intentAtt = mi.GetCustomAttribute<IntentAttribute>()
                 select new
                 {
                     MethodInfo = mi,
                     Parameters = parameters,
                     IntentAttribure = intentAtt
                 }).ToList();

            foreach (var candidateMethod in candidateMethods)
            {
                var (mi, parameters, intentAtt) = (candidateMethod.MethodInfo, candidateMethod.Parameters, candidateMethod.IntentAttribure);
                if (mi.ReturnType != typeof(void) && false == typeof(Task).IsAssignableFrom(mi.ReturnType))
                {
                    if (intentAtt is null) continue;
                    throw new InvalidOperationException(
                        new StringBuilder("Invalid message handler return type.")
                            .Append($" {GetType()}.{mi.Name} is expected to return either {typeof(void)} or a {typeof(Type)} result type.")
                            .Append($" Actual result type is {mi.ReturnType}.")
                            .ToString());
                }

                var customParameters = parameters
                    .Where(p => p.ParameterType != typeof(IDeliveryArgs) &&
                                false == (typeof(CancellationToken) == p.ParameterType)).ToList();
                var dynamicBindingParameters = customParameters
                    .Where(cp => false == typeof(IAppletChannel).IsAssignableFrom(cp.ParameterType))
                    .ToList();
                if (dynamicBindingParameters.Count > 1)
                {
                    var csv = String.Join(", ", dynamicBindingParameters.Select(p => p.Name));
                    throw new InvalidOperationException(
                        new StringBuilder("Detected multiple dto- parameters.")
                            .Append($" {GetType()}.{mi.Name} accepts multipe dto- parameters.")
                            .Append($" See parameters {csv}.")
                            .ToString());
                }
            }

            return candidateMethods
                .ToLookup(
                    item => item.IntentAttribure?.Intent ?? Guid.Empty,
                    item => item.MethodInfo);
        }



        protected virtual Task ProcessUnmappedMessageAsync(IDeliveryArgs args, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var logMessage = new StringBuilder($"No appropriate handler found for the incoming message.")
                .Append($" Applet: {Channel.AppletName} ({Channel.AppletId})")
                .Append($" Intent: {args.IntentName} ({args.IntentId})")
                .ToString();
            Debug.Fail(logMessage);
            Trace.TraceError(logMessage);
            return Task.CompletedTask;
        }


    }
}
