using System;
using System.Collections.Generic;

namespace Applets.Common
{
    public abstract class DeliveryCallback : IDeliveryCallback
    {
        private readonly Dictionary<EventKey, DDeliveryCallback> _map = new();
        public IEnumerable<EventKey> Keys => _map.Keys;

        public DDeliveryCallback GetHandler(EventKey key) => _map[key];

        protected void AddRoute(MessageIntentId eventIntentId, Type dtoType, DDeliveryCallback callback)
        {
            _map.Add(new EventKey(eventIntentId, dtoType), callback);
        }
    }
}
