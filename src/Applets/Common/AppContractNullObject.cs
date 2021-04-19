using System;
using System.Collections.Generic;
using System.Linq;

namespace Applets.Common
{
    sealed class AppContractNullObject : IAppContract
    {
        public bool IsValidEvent(AppletId senderId, MessageIntentId eventIntentId, Type dtoType) => true;

        public bool IsValidRequest(AppletId senderId, MessageIntentId requestIntentId, Type dtoType) => true;

        public bool IsEventListener(AppletId receiverId) => true;

        public bool IsValidSubscription(AppletId receiverId, MessageIntentId eventIntentId, Type dtoType) => true;
        public bool IsValidResponse(AppletId receiverId, MessageIntentId responseIntentId, Type dtoType) => true;

        public bool CanAcceptReply(AppletId appletId, MessageIntent argsMessageIntent, Type replyType) => true;

        public MessageIntent GetIntent(MessageIntentId intentId) => new MessageIntent(intentId, intentId.ToString());
        public Applet GetApplet(AppletId appletId) => new Applet(appletId, appletId.ToString());
        public IEnumerable<EventKey> GetEventKeys(AppletId appletId) => Enumerable.Empty<EventKey>();
    }
}
