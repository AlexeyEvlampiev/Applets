using System;
using System.Collections.Generic;
using System.Linq;

namespace Applets.Common
{
    sealed class AppContract : IAppContract
    {
        record AppletRpcReplyKey(AppletId AppletId, MessageIntentId MessageIntentId, Type ReplyType);

        private readonly HashSet<AppletRpcKey> _appletRpcKeys;
        private readonly HashSet<AppletRpcReplyKey> _appletRpcReplyKeys;
        private readonly HashSet<AppletTriggerKey> _appletSubscriptionKeys;
        private readonly Dictionary<MessageIntentId, MessageIntent> _messageIntentsById;
        private readonly Dictionary<AppletId, Applet> _appletsById;
        private readonly HashSet<AppletId> _subscriberApplets;
        private readonly HashSet<AppletEventKey> _appletStreamRequestKeys;
        private readonly HashSet<AppletEventKey> _appletBroadcastKeys;

        public AppContract(
            IEnumerable<MessageIntent> messageIntents,
            IEnumerable<Applet> applets,
            IEnumerable<AppletEventKey> appletBroadcastKeys,
            IEnumerable<AppletTriggerKey> appletSubscriptionKeys,
            IEnumerable<AppletRpcKey> appletRpcKeys)
        {
            if (messageIntents == null) throw new ArgumentNullException(nameof(messageIntents));
            if (applets == null) throw new ArgumentNullException(nameof(applets));
            _appletRpcKeys = appletRpcKeys?.ToHashSet() ?? throw new ArgumentNullException(nameof(appletRpcKeys));
            _appletStreamRequestKeys = _appletRpcKeys.Select(key => new AppletEventKey(key.AppletId, key.RequestIntentId, key.RequestType)).ToHashSet();
            _appletBroadcastKeys = appletBroadcastKeys?.ToHashSet() ?? throw new ArgumentNullException(nameof(appletBroadcastKeys));
            _appletSubscriptionKeys = (appletSubscriptionKeys ?? throw new ArgumentNullException(nameof(appletSubscriptionKeys))).ToHashSet();
            _messageIntentsById = messageIntents.ToDictionary(intent => intent.Id);
            _appletsById = applets.ToDictionary(applet => applet.Id);
            _subscriberApplets = _appletSubscriptionKeys.Select(key => key.AppletId).ToHashSet();
            _appletRpcReplyKeys = _appletRpcKeys
                .Select(key => new AppletRpcReplyKey(key.AppletId, key.ResponseIntentId, key.ResponseType)).ToHashSet();
        }


        public bool IsValidEvent(AppletId senderId, MessageIntentId eventIntentId, Type dtoType)
        {
            if (senderId == null) throw new ArgumentNullException(nameof(senderId));
            if (eventIntentId == null) throw new ArgumentNullException(nameof(eventIntentId));
            if (dtoType == null) throw new ArgumentNullException(nameof(dtoType));
            return _appletBroadcastKeys.Contains(new AppletEventKey(senderId, eventIntentId, dtoType));
        }

        public bool IsValidRequest(AppletId senderId, MessageIntentId requestIntentId, Type dtoType)
        {
            if (senderId == null) throw new ArgumentNullException(nameof(senderId));
            if (requestIntentId == null) throw new ArgumentNullException(nameof(requestIntentId));
            if (dtoType == null) throw new ArgumentNullException(nameof(dtoType));
            var key = new AppletEventKey(senderId, requestIntentId, dtoType);
            return _appletStreamRequestKeys.Contains(key);
        }

        public bool IsEventListener(AppletId receiverId)
        {
            if (receiverId == null) throw new ArgumentNullException(nameof(receiverId));
            return _subscriberApplets.Contains(receiverId);
        }

        public bool IsValidSubscription(AppletId receiverId, MessageIntentId eventIntentId, Type dtoType)
        {
            if (receiverId == null) throw new ArgumentNullException(nameof(receiverId));
            if (eventIntentId == null) throw new ArgumentNullException(nameof(eventIntentId));
            if (dtoType == null) throw new ArgumentNullException(nameof(dtoType));
            return _appletSubscriptionKeys.Contains(new AppletTriggerKey(receiverId, eventIntentId, dtoType));
        }

        public bool IsValidResponse(AppletId receiverId, MessageIntentId responseIntentId, Type dtoType)
        {
            if (receiverId == null) throw new ArgumentNullException(nameof(receiverId));
            if (responseIntentId == null) throw new ArgumentNullException(nameof(responseIntentId));
            if (dtoType == null) throw new ArgumentNullException(nameof(dtoType));
            return _appletRpcReplyKeys.Contains(new AppletRpcReplyKey(receiverId, responseIntentId, dtoType));
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
