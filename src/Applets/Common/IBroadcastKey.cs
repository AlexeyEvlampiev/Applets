using System;


namespace Applets.Common
{
    interface IBroadcastKey
    {
        MessageIntentId MessageIntentId { get; }
        Type DtoType { get; }

        public bool IsMatch(IBroadcastKey other)
        {
            if (other is null) return false;
            if (MessageIntentId is null) return false;
            if (DtoType is null) return false;
            return MessageIntentId == other.MessageIntentId && DtoType == other.DtoType;
        }
    };
}
