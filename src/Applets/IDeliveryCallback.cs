using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Applets
{
    public interface IDeliveryCallback
    {
        IEnumerable<EventKey> Keys { get; }

        DDeliveryCallback GetHandler(EventKey key);

        [DebuggerStepThrough]
        public Task InvokeAsync(IEventArgs args, IAppletChannel channel, CancellationToken cancellation = default)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            var (intent, data) = (args.MessageIntent, args.Data);
            var key = new EventKey(
                intent ?? throw new NullReferenceException($"{nameof(args)}.{nameof(args.MessageIntent)} is null."), 
                data?.GetType() ?? throw new NullReferenceException($"{nameof(args)}.{nameof(args.MessageIntent)} is null."));
            var handler = GetHandler(key);
            return handler.Invoke(args, channel, cancellation);
        }
    }
}
