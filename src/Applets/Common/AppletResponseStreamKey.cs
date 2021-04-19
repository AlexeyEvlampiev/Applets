using System;

namespace Applets.Common
{
    record AppletResponseStreamKey(
        AppletId AppletId, 
        MessageIntentId RequestIntentId, 
        Type RequestType,
        MessageIntentId ResponseIntentId,
        Type ResponseType);
}
