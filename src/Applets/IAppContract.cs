using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Applets
{
    public interface IAppContract
    {
        bool CanEmitEvent(AppletId senderId, MessageIntentId eventIntentId, Type dtoType);
        bool CanBroadcastRequest(AppletId senderId, MessageIntentId requestIntentId, Type dtoType);
        bool IsEventSubscriber(AppletId appletId);
        bool HasSubscription(AppletId appletId, MessageIntentId messageIntentId, Type eventType);

        bool CanAcceptReply(AppletId appletId, MessageIntentId argsMessageIntent, Type replyType);


        [DebuggerStepThrough]
        public bool CanAcceptReply(AppletId appletId, IDeliveryArgs args)
        {
            if (appletId is null || args is null) return false;
            return this.CanAcceptReply(appletId, args.MessageIntent, args.Data.GetType());
        }

        MessageIntent GetIntent(MessageIntentId intentId);
        Applet GetApplet(AppletId appletId);

        IEnumerable<EventKey> GetEventKeys(AppletId appletId);
    }
}
