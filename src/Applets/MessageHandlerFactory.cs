using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Applets.Common;

namespace Applets
{
    sealed class MessageHandlerFactory
    {
        readonly MethodInfo[] _methods;
        private readonly DEventNotificationHandler _fallback;
        readonly ConcurrentDictionary<Type, DEventNotificationHandler> 
            _handlersCache = new ConcurrentDictionary<Type, DEventNotificationHandler>();

        private readonly AppletDeliveryProcessor _processor;

        delegate object DBuildInvocationParameter(IDeliveryArgs args, CancellationToken cancellation);

        public MessageHandlerFactory(AppletDeliveryProcessor processor, MethodInfo[] methods, DEventNotificationHandler fallback)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            if (methods == null) throw new ArgumentNullException(nameof(methods));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _methods = methods ?? throw new ArgumentNullException(nameof(methods));
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        public DEventNotificationHandler GetHandler(object dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (_handlersCache.TryGetValue(dto.GetType(), out var handler)){ return handler; }
            handler = CreateHandler(dto.GetType());
            _handlersCache.GetOrAdd(dto.GetType(), handler);
            return handler;
        }

        private DEventNotificationHandler CreateHandler(Type dtoType)
        {
            foreach (var customHandler in _methods)
            {
                var parameterBuilders = BuildParameterFactories(customHandler);
                Task StronglyTypedMessageHandler(IDeliveryArgs args, CancellationToken cancellation)
                {
                    var parameters = new object[parameterBuilders.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var parametersBuilder = parameterBuilders[i];
                        parameters[i] = parametersBuilder.Invoke(args, cancellation);
                    }
                    var result = (Task)customHandler.Invoke(_processor, parameters);
                    return result ?? Task.CompletedTask;
                }

                return new DEventNotificationHandler(StronglyTypedMessageHandler);
            }

            return _fallback;
        }

        DBuildInvocationParameter[] BuildParameterFactories(MethodInfo method)
        {
            var parameterInfos = method.GetParameters();

            var parameterBuildersList =
                new List<DBuildInvocationParameter>();

            foreach (var parameterInfo in parameterInfos)
            {
                if (parameterInfo.ParameterType == typeof(IDeliveryArgs))
                {
                    parameterBuildersList.Add((args, token) => args);
                }
                else if (parameterInfo.ParameterType == typeof(IAppInfo))
                {
                    parameterBuildersList.Add((args, token) => _processor.Channel.GetAppInfo());
                }
                else if (parameterInfo.ParameterType == typeof(CancellationToken))
                {
                    parameterBuildersList.Add((args, token) => token);
                }
                else 
                {
                    parameterBuildersList.Add((args, token) => args.Dto);
                }
            }

            return parameterBuildersList.ToArray();
        }
    }
}
