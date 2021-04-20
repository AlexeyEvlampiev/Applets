using System;

namespace Applets.Common
{
    record AppletRpcKey(
        AppletId AppletId,
        MessageIntentId RequestIntentId,
        Type RequestType,
        MessageIntentId ResponseIntentId,
        Type ResponseType) : ITriggerKey
    {
        MessageIntentId ITriggerKey.MessageIntentId => ResponseIntentId;
        Type ITriggerKey.DtoType => RequestType;
    }
}
