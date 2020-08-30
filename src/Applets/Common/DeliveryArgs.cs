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

        public byte[] Body { get; }

        public IAppletChannel Channel { get; }

        public Guid Intent { get; protected set; }

        public string IntentName => _appInfo.GetIntentName(Intent);

        public Guid Correlation { get; protected set; }

        public Guid From { get; protected set; }

        public Guid Contract { get; protected set; }

        public Guid Applet { get; protected set; }

        public string AppletName => _appInfo.GetAppletName(Applet);

        [DebuggerStepThrough]
        public Task ReplyWithAsync(object dto, CancellationToken cancellation)
        {
            var reply = _appInfo.ToDispatchArgs(dto);
            return ReplyWithAsync(reply, cancellation);
        }

        public Task ReplyWithAsync(DispatchArgs reply, CancellationToken cancellation)
        {
            if (reply == null) throw new ArgumentNullException(nameof(reply));
            if (reply.Correlation == Guid.Empty)
                reply.Correlation = this.Correlation;
            if (reply.To == Guid.Empty)
                reply.To = this.From;
            if (reply.Intent == Guid.Empty)
                reply.Intent = reply.Contract;
            return Channel.SendAsync(reply, cancellation);
        }

        public bool HasCorrelationId => Correlation != Guid.Empty;


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
