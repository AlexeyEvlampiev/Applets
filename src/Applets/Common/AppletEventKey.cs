using System;

namespace Applets.Common
{
    record AppletEventKey(AppletId SenderAppletId, MessageIntentId MessageIntentId, Type DtoType) : IBroadcastKey;
}
