using System;

namespace Applets.Common
{
    record AppletRpcKey(
        AppletId AppletId,
        MessageIntentId RequestIntentId,
        Type RequestDtoType,
        MessageIntentId ResponseIntentId,
        Type ResponseDtoType) : IBroadcastKey, ITriggerKey
    {
        MessageIntentId IBroadcastKey.MessageIntentId => RequestIntentId;
        Type IBroadcastKey.DtoType => RequestDtoType;

        MessageIntentId ITriggerKey.MessageIntentId => ResponseIntentId;
        Type ITriggerKey.DtoType => ResponseDtoType;
    }
}
