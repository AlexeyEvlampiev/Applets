using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Applets.Common
{
    public abstract class DeliveryArgs : IDeliveryArgs
    {
        private object _dto;
        private readonly IAppInfo _appInfo;

        protected DeliveryArgs(byte[] body, IAppletChannel channel)
        {
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _appInfo = Channel.GetAppInfo() ?? 
                       throw new NullReferenceException($"{nameof(channel)}.{nameof(Channel.GetAppInfo)} returned null");
        }

        protected DeliveryArgs(DispatchArgs dispatchArgs, IAppletChannel channel)
        {
            if (dispatchArgs == null) throw new ArgumentNullException(nameof(dispatchArgs));
            Body = dispatchArgs.Body ?? throw new NullReferenceException(nameof(dispatchArgs.Body));
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _appInfo = Channel.GetAppInfo() ??
                       throw new NullReferenceException($"{nameof(channel)}.{nameof(Channel.GetAppInfo)} returned null");
            IntentId = dispatchArgs.IntentId;
            CorrelationId = dispatchArgs.CorrelationId;
            From = dispatchArgs.From;
            DataContractId = dispatchArgs.DataContractId;
            AppletId = dispatchArgs.AppletId;
        }

        public byte[] Body { get; }

        public IAppletChannel Channel { get; }

        public Guid IntentId { get; protected set; }

        public string IntentName => _appInfo.GetIntentName(IntentId);

        public Guid CorrelationId { get; protected set; }

        public Guid From { get; protected set; }

        public Guid DataContractId { get; protected set; }

        public Guid AppletId { get; protected set; }

        public string AppletName => _appInfo.GetAppletName(AppletId);

        [DebuggerStepThrough]
        public Task ReplyWithAsync(object dto, CancellationToken cancellation)
        {
            var reply = _appInfo.ToDispatchArgs(dto);
            return ReplyWithAsync(reply, cancellation);
        }

        public Task ReplyWithAsync(DispatchArgs reply, CancellationToken cancellation)
        {
            if (reply == null) throw new ArgumentNullException(nameof(reply));
            if (reply.CorrelationId == Guid.Empty)
                reply.CorrelationId = this.CorrelationId;
            if (reply.To == Guid.Empty)
                reply.To = this.From;
            if (reply.IntentId == Guid.Empty)
                reply.IntentId = reply.DataContractId;
            return Channel.SendAsync(reply, cancellation);
        }

        public bool HasCorrelationId => CorrelationId != Guid.Empty;


        public object Dto
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _dto, () => _appInfo.Deserialize(this));
                return _dto;
            }
        }


    }
}
