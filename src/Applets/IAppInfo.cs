using System;
using System.Collections.Generic;
using Applets.Common;

namespace Applets
{
    public interface IAppInfo
    {
        string GetIntentName(Guid intentCode);
        Guid ApplicationId { get; }
        string ApplicationName { get; }
        IEnumerable<AppletInfo> Applets { get; }
        TimeSpan HeartbeatInterval { get; }
        bool RequiresPublicInboxQueue(Guid appletId);


        bool IsExpectedReply(Guid appletId, Guid requestIntent, Guid replyIntent);

        object Deserialize(DeliveryArgs args);
        DispatchArgs ToDispatchArgs(object dto);
        string GetAppletName(Guid applet);
        bool IsAppletId(Guid id);

        void Assert();
        IEnumerable<IntentInfo> GetAppletIncomingIntents(Guid appletId);
        bool CanReceiveEventNotification(Guid appletId, Guid publicEventIntentId);
        bool CanSend(Guid appletId, Guid intent);
    }
}
