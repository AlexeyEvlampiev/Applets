using System;

namespace Applets.Common
{
    record AppletEventKey(AppletId AppletId, MessageIntentId MessageIntentId, Type DtoType)
        : IBroadcastKey;
}
