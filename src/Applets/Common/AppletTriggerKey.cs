using System;

namespace Applets.Common
{
    record AppletTriggerKey(AppletId AppletId, MessageIntentId MessageIntentId, Type DtoType) : IBroadcastKey;
}
