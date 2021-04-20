﻿using System;


namespace Applets.Common
{
    interface IBroadcastKey
    {
        MessageIntentId MessageIntentId { get; }
        Type DtoType { get; }

        public bool IsMatch(ITriggerKey trigger)
        {
            if (trigger is null) return false;
            if (MessageIntentId is null) return false;
            if (DtoType is null) return false;
            return MessageIntentId == trigger.MessageIntentId && DtoType == trigger.DtoType;
        }
    };
}
