using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Applets.Common;

namespace Applets
{
    /// <summary>
    /// <seealso cref="IAppContract"/> application contract builder.
    /// </summary>
    public class AppContractBuilder
    {
        #region Private Fields

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<MessageIntentId, MessageIntent> _messageIntentsById = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<AppletId, Applet> _appletsById = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly HashSet<AppletEventKey> _appletEventKeys = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly HashSet<AppletTriggerKey> _appletTriggerKeys = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly HashSet<AppletRpcKey> _appletRpcKeys = new();

        

        #endregion

        public sealed class ResponseStreamKeyBuilder
        {
            private readonly AppletId _appletId;
            private readonly MessageIntentId _requestIntentId;
            private readonly Type _requestType;
            private readonly HashSet<AppletRpcKey> _keys = new HashSet<AppletRpcKey>();

            internal ResponseStreamKeyBuilder(AppletId appletId, MessageIntentId requestIntentId, Type requestType)
            {
                _appletId = appletId ?? throw new ArgumentNullException(nameof(appletId));
                _requestIntentId = requestIntentId ?? throw new ArgumentNullException(nameof(requestIntentId));
                _requestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
            }

            public ResponseStreamKeyBuilder WithResponse(MessageIntentId replyIntentId, Type replyType)
            {
                if (replyIntentId == null) throw new ArgumentNullException(nameof(replyIntentId));
                if (replyType == null) throw new ArgumentNullException(nameof(replyType));
                _keys.Add(new AppletRpcKey(
                    _appletId,
                    _requestIntentId,
                    _requestType,
                    replyIntentId,
                    replyType));
                return this;
            }

            internal HashSet<AppletRpcKey> Keys => _keys;
        }


        public MessageIntent AddMessageIntent(MessageIntentId id, string name = null)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            var intent = new MessageIntent(id, name ?? id.ToString());
            _messageIntentsById.Add(intent.Id, intent);
            return intent;
        }

        public MessageIntent AddMessageIntent<T>(T identifier, string name = null)
        {
            if (identifier == null) throw new ArgumentNullException(nameof(identifier));
            if (identifier is MessageIntentId id) return this.AddMessageIntent(id, name);
            id = CreateMessageIntentId(identifier);
            var intent = new MessageIntent(id, name ?? id.ToString());
            _messageIntentsById.Add(intent.Id, intent);
            return intent;
        }

        public Applet AddApplet(AppletId id, string name = null)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            var applet = new Applet(id, name ?? id.ToString());
            _appletsById.Add(applet.Id, applet);
            return applet;
        }

        public void EnableAppletTrigger(AppletId appletId, MessageIntentId triggerIntentId, Type dtoType)
        {
            if (appletId == null) throw new ArgumentNullException(nameof(appletId));
            if (triggerIntentId == null) throw new ArgumentNullException(nameof(triggerIntentId));
            if (dtoType == null) throw new ArgumentNullException(nameof(dtoType));
            ThrowIfNotExists(appletId);
            ThrowIfNotExists(triggerIntentId);
            _appletTriggerKeys.Add(new AppletTriggerKey(appletId, triggerIntentId, dtoType));
        }

        public void EnableAppletEvent(AppletId senderAppletId, MessageIntentId eventIntentId, Type dtoType)
        {
            if (senderAppletId == null) throw new ArgumentNullException(nameof(senderAppletId));
            if (eventIntentId == null) throw new ArgumentNullException(nameof(eventIntentId));
            if (dtoType == null) throw new ArgumentNullException(nameof(dtoType));
            ThrowIfNotExists(senderAppletId);
            ThrowIfNotExists(eventIntentId);
            _appletEventKeys.Add(new AppletEventKey(senderAppletId, eventIntentId, dtoType));
        }

        public void EnableAppletRpc(
            AppletId appletId,
            MessageIntentId requestIntentId,
            Type requestDtoType,
            MessageIntentId responseIntentId,
            Type responseType)
        {
            if (appletId == null) throw new ArgumentNullException(nameof(appletId));
            if (requestIntentId == null) throw new ArgumentNullException(nameof(requestIntentId));
            if (requestDtoType == null) throw new ArgumentNullException(nameof(requestDtoType));
            if (responseIntentId == null) throw new ArgumentNullException(nameof(responseIntentId));
            if (responseType == null) throw new ArgumentNullException(nameof(responseType));
            ThrowIfNotExists(appletId);
            ThrowIfNotExists(requestIntentId);
            ThrowIfNotExists(responseIntentId);
            _appletRpcKeys.Add(new AppletRpcKey(appletId, requestIntentId, requestDtoType,
                responseIntentId, responseType));
        }

        public Applet AddApplet(object id, string name = null)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            var appletId = CreateAppletId(id);
            var applet = new Applet(appletId, name ?? id.ToString());
            _appletsById.Add(applet.Id, applet);
            return applet;
        }

        public void EnableAppletRpc(
            AppletId appletId, 
            MessageIntentId requestIntentId, 
            Type requestDtoType,
            Action<ResponseStreamKeyBuilder> rpcConfig)
        {
            if (appletId == null) throw new ArgumentNullException(nameof(appletId));
            if (requestIntentId == null) throw new ArgumentNullException(nameof(requestIntentId));
            if (requestDtoType == null) throw new ArgumentNullException(nameof(requestDtoType));
            if (rpcConfig == null) throw new ArgumentNullException(nameof(rpcConfig));
            ThrowIfNotExists(appletId);
            ThrowIfNotExists(requestIntentId);
            var responseStreamKeyBuilder = new ResponseStreamKeyBuilder(appletId, requestIntentId, requestDtoType);

            rpcConfig.Invoke(responseStreamKeyBuilder);
            foreach (var key in responseStreamKeyBuilder.Keys)
            {
                ThrowIfNotExists(key.ResponseIntentId);
                _appletRpcKeys.Add(key);
            }

        }

        protected virtual MessageIntentId CreateMessageIntentId<T>(T identifier) => MessageIntentId.Create(identifier);
        protected virtual AppletId CreateAppletId(object identifier) => AppletId.Create(identifier);

        /// <summary>
        /// Builds and validates the resulting <see cref="IAppContract"/> object.
        /// </summary>
        /// <returns>Application contract</returns>
        /// <exception cref="AppContractBuilderException"></exception>
        public IAppContract Build()
        {
            Assert();
            return new AppContract(
                _messageIntentsById.Values,
                _appletsById.Values,
                _appletEventKeys,
                _appletTriggerKeys,
                _appletRpcKeys);
        }



        private void Assert()
        {
            if (_messageIntentsById.Count == 0)
            {
                throw new AppContractBuilderException(
                    new StringBuilder("Message intent registrations are missing.")
                        .Append($" Use {nameof(AddMessageIntent)} method to register the required intents."));
            }

            if (_appletsById.Count == 0)
            {
                throw new AppContractBuilderException(
                    new StringBuilder("Applet registrations are missing.")
                        .Append($" Use {nameof(AddApplet)} method to register the required applets."));
            }

            if (_appletTriggerKeys.Count == 0)
            {
                throw new AppContractBuilderException(
                    new StringBuilder("Applet trigger registrations are missing.")
                        .Append($" Use {nameof(EnableAppletTrigger)} method to register the required triggers."));
            }

            var missingBroadcasts = _appletTriggerKeys
                .OfType<IBroadcastKey>()
                .Where(triggerKey => _appletEventKeys.Any(triggerKey.IsMatch) == false &&
                                     _appletRpcKeys.Any(triggerKey.IsMatch) == false)
                .ToList();
            if (missingBroadcasts.Any())
            {
                var missingRegistrationsCsv = string.Join(", ", missingBroadcasts
                    .Select(k=> $"{k.MessageIntentId} ({k.DtoType})"));
                var recipientAppletsCsv = string.Join(", ", missingBroadcasts
                    .OfType<AppletTriggerKey>()
                    .Select(key => $"{key.AppletId}"));
                throw new AppContractBuilderException(
                    new StringBuilder("Missing broadcast registrations.")
                        .Append($" Broadcast(s) to register: {missingRegistrationsCsv}.")
                        .Append($" Broadcast subscribers: {recipientAppletsCsv}.")
                        .Append($" Use {nameof(EnableAppletEvent)} and/or {nameof(EnableAppletRpc)} methods to register these broadcast(s)."));
            }

        }

        private void ThrowIfNotExists(AppletId appletId)
        {
            if (false == _appletsById.ContainsKey(appletId))
                throw new AppContractBuilderException(
                    new StringBuilder($"{appletId} applet is not registered.")
                        .Append($" Use {nameof(AddApplet)} method to register the required applet."));
        }

        private void ThrowIfNotExists(MessageIntentId messageIntentId)
        {
            if (false == _messageIntentsById.ContainsKey(messageIntentId))
                throw new AppContractBuilderException(
                    new StringBuilder($"{messageIntentId} message intent is not registered.")
                        .Append($" Use {nameof(AddMessageIntent)} method to register the required message intent."));
        }


    }
}
