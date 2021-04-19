﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Applets.Common
{
    sealed class AppContract : IAppContract
    {
        record AppletRpcReplyKey(AppletId AppletId, MessageIntentId MessageIntentId, Type ReplyType);

        private readonly HashSet<AppletResponseStreamKey> _appletRpcKeys;
        private readonly HashSet<AppletRpcReplyKey> _appletRpcReplyKeys;
        private readonly HashSet<AppletSubscriptionKey> _appletSubscriptionKeys;
        private readonly Dictionary<MessageIntentId, MessageIntent> _messageIntentsById;
        private readonly Dictionary<AppletId, Applet> _appletsById;
        private readonly HashSet<AppletId> _subscriberApplets;
        private readonly HashSet<AppletBroadcastKey> _appletStreamRequestKeys;
        private readonly HashSet<AppletBroadcastKey> _appletBroadcastKeys;

        public AppContract(
            IEnumerable<MessageIntent> messageIntents,
            IEnumerable<Applet> applets,
            IEnumerable<AppletBroadcastKey> appletBroadcastKeys,
            IEnumerable<AppletSubscriptionKey> appletSubscriptionKeys,
            IEnumerable<AppletResponseStreamKey> appletRpcKeys)
        {
            if (messageIntents == null) throw new ArgumentNullException(nameof(messageIntents));
            if (applets == null) throw new ArgumentNullException(nameof(applets));
            _appletRpcKeys = appletRpcKeys?.ToHashSet() ?? throw new ArgumentNullException(nameof(appletRpcKeys));
            _appletStreamRequestKeys = _appletRpcKeys.Select(key => new AppletBroadcastKey(key.AppletId, key.RequestIntentId, key.RequestType)).ToHashSet();
            _appletBroadcastKeys = appletBroadcastKeys?.ToHashSet() ?? throw new ArgumentNullException(nameof(appletBroadcastKeys));
            _appletSubscriptionKeys = (appletSubscriptionKeys ?? throw new ArgumentNullException(nameof(appletSubscriptionKeys))).ToHashSet();
            _messageIntentsById = messageIntents.ToDictionary(intent => intent.Id);
            _appletsById = applets.ToDictionary(applet => applet.Id);
            _subscriberApplets = _appletSubscriptionKeys.Select(key => key.AppletId).ToHashSet();
            _appletRpcReplyKeys = _appletRpcKeys
                .Select(key => new AppletRpcReplyKey(key.AppletId, key.ResponseIntentId, key.ResponseType)).ToHashSet();
        }


        public bool CanEmitEvent(AppletId senderId, MessageIntentId eventIntentId, Type dtoType)
        {
            if (senderId == null) throw new ArgumentNullException(nameof(senderId));
            if (eventIntentId == null) throw new ArgumentNullException(nameof(eventIntentId));
            if (dtoType == null) throw new ArgumentNullException(nameof(dtoType));
            return _appletBroadcastKeys.Contains(new AppletBroadcastKey(senderId, eventIntentId, dtoType));
        }

        public bool CanBroadcastRequest(AppletId senderId, MessageIntentId requestIntentId, Type dtoType)
        {
            if (senderId == null) throw new ArgumentNullException(nameof(senderId));
            if (requestIntentId == null) throw new ArgumentNullException(nameof(requestIntentId));
            if (dtoType == null) throw new ArgumentNullException(nameof(dtoType));
            var key = new AppletBroadcastKey(senderId, requestIntentId, dtoType);
            return _appletStreamRequestKeys.Contains(key);
        }

        public bool IsEventSubscriber(AppletId appletId)
        {
            if (appletId == null) throw new ArgumentNullException(nameof(appletId));
            return _subscriberApplets.Contains(appletId);
        }

        public bool HasSubscription(AppletId appletId, MessageIntentId messageIntentId, Type eventType)
        {
            if (appletId == null) throw new ArgumentNullException(nameof(appletId));
            if (messageIntentId == null) throw new ArgumentNullException(nameof(messageIntentId));
            if (eventType == null) throw new ArgumentNullException(nameof(eventType));
            return _appletSubscriptionKeys.Contains(new AppletSubscriptionKey(appletId, messageIntentId, eventType));
        }

        public bool CanAcceptReply(AppletId appletId, MessageIntentId messageIntentId, Type replyType)
        {
            if (appletId == null) throw new ArgumentNullException(nameof(appletId));
            if (messageIntentId == null) throw new ArgumentNullException(nameof(messageIntentId));
            if (replyType == null) throw new ArgumentNullException(nameof(replyType));
            return _appletRpcReplyKeys.Contains(new AppletRpcReplyKey(appletId, messageIntentId, replyType));
        }

        public MessageIntent GetIntent(MessageIntentId intentId)
        {
            return _messageIntentsById.TryGetValue(intentId, out var intent)
                ? intent
                : throw new AppContractViolationException($"{intentId} intent does not exist.");
        }

        public Applet GetApplet(AppletId id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (_appletsById.TryGetValue(id, out var applet)) return applet;
            throw new AppContractViolationException($"Applet not found.");
        }

        public IEnumerable<EventKey> GetEventKeys(AppletId appletId)
        {
            if (appletId == null) throw new ArgumentNullException(nameof(appletId));
            return _appletSubscriptionKeys
                .Where(key => key.AppletId == appletId)
                .Select(key => new EventKey(key.MessageIntentId, key.DtoType));
        }
    }
}
