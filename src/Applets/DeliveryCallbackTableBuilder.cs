using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Applets
{
    public sealed class DeliveryCallbackTableBuilder
    {
        private readonly Dictionary<EventKey, DDeliveryCallback> _callbacksByEventKey = new();

        sealed class Table : IDeliveryCallback
        {
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            private readonly Dictionary<EventKey, DDeliveryCallback> _callbacksByEventKey;

            public Table(Dictionary<EventKey, DDeliveryCallback> callbacksByEventKey)
            {
                _callbacksByEventKey = callbacksByEventKey ?? throw new ArgumentNullException(nameof(callbacksByEventKey));
            }

            public IEnumerable<EventKey> Keys => _callbacksByEventKey.Keys;

            public DDeliveryCallback GetHandler(EventKey key) => _callbacksByEventKey[key];
        }

        public IDeliveryCallback Build() => new Table(_callbacksByEventKey);
    }
}