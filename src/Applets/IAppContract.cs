using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Applets
{
    public interface IAppContract
    {
        bool IsValidEvent(AppletId senderId, MessageIntentId eventIntentId, Type dtoType);
        bool IsValidRequest(AppletId senderId, MessageIntentId requestIntentId, Type dtoType);
        bool IsEventListener(AppletId appletId);
        bool IsValidSubscription(AppletId receiverId, MessageIntentId eventIntentId, Type dtoType);

        bool IsValidResponse(AppletId receiverId, MessageIntentId responseIntentId, Type dtoType);


        [DebuggerStepThrough]
        public bool IsValidResponse(AppletId receiverId, IDeliveryArgs args)
        {
            if (receiverId is null || args is null) return false;
            return this.IsValidResponse(receiverId, args.MessageIntent, args.Data.GetType());
        }

        MessageIntent GetIntent(MessageIntentId intentId);
        Applet GetApplet(AppletId appletId);

        IEnumerable<EventKey> GetEventKeys(AppletId appletId);
    }
}
