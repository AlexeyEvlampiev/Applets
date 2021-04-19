using System;

namespace Applets.Common
{
    record AppletBroadcastKey(AppletId SenderAppletId, MessageIntentId MessageIntentId, Type DtoType);
}
