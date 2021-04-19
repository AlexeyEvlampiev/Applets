using System;
using System.Collections.Generic;
using System.Linq;

namespace Applets.Common
{
    sealed class AppContractNullObject : IAppContract
    {
        public bool CanEmitEvent(AppletId senderId, MessageIntentId eventIntentId, Type dtoType) => true;

        public bool CanBroadcastRequest(AppletId senderId, MessageIntentId requestIntentId, Type dtoType) => true;

        public bool IsEventSubscriber(AppletId appletId) => true;

        public bool HasSubscription(AppletId appletId, MessageIntentId messageIntentId, Type eventType) => true;
        public bool CanAcceptReply(AppletId appletId, MessageIntentId argsMessageIntent, Type replyType) => true;

        public bool CanAcceptReply(AppletId appletId, MessageIntent argsMessageIntent, Type replyType) => true;

        public MessageIntent GetIntent(MessageIntentId intentId) => new MessageIntent(intentId, intentId.ToString());
        public Applet GetApplet(AppletId appletId) => new Applet(appletId, appletId.ToString());
        public IEnumerable<EventKey> GetEventKeys(AppletId appletId) => Enumerable.Empty<EventKey>();
    }
}
