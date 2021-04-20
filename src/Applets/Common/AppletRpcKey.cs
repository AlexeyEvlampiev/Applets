using System;

namespace Applets.Common
{
    record AppletRpcKey(
        AppletId AppletId,
        MessageIntentId RequestIntentId,
        Type RequestType,
        MessageIntentId ResponseIntentId,
        Type ResponseType) : IBroadcastKey
    {
        MessageIntentId IBroadcastKey.MessageIntentId => RequestIntentId;
        Type IBroadcastKey.DtoType => RequestType;
    }
}
