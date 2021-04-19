using System;

namespace Applets.Common
{
    record AppletSubscriptionKey(AppletId AppletId, MessageIntentId MessageIntentId, Type DtoType);
}
