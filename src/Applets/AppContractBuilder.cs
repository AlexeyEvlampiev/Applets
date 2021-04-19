using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly HashSet<AppletSubscriptionKey> _appletSubscriptionKeys = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly HashSet<AppletResponseStreamKey> _appletResponseStreamKeys = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly HashSet<AppletBroadcastKey> _appletBroadcastKeys = new();

        #endregion

        public sealed class ResponseStreamKeyBuilder
        {
            private readonly AppletId _appletId;
            private readonly MessageIntentId _requestIntentId;
            private readonly Type _requestType;
            private readonly HashSet<AppletResponseStreamKey> _keys = new HashSet<AppletResponseStreamKey>();

            internal ResponseStreamKeyBuilder(AppletId appletId, MessageIntentId requestIntentId, Type requestType)
            {
                _appletId = appletId ?? throw new ArgumentNullException(nameof(appletId));
                _requestIntentId = requestIntentId ?? throw new ArgumentNullException(nameof(requestIntentId));
                _requestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
            }

            public void Add(MessageIntentId replyIntentId, Type replyType)
            {
                if (replyIntentId == null) throw new ArgumentNullException(nameof(replyIntentId));
                if (replyType == null) throw new ArgumentNullException(nameof(replyType));
                _keys.Add(new AppletResponseStreamKey(
                    _appletId,
                    _requestIntentId,
                    _requestType,
                    replyIntentId,
                    replyType));
            }

            internal HashSet<AppletResponseStreamKey> Keys => _keys;
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

        public void EnableSubscription(AppletId subscriberId, MessageIntentId eventIntentId, Type dtoType)
        {
            if (subscriberId == null) throw new ArgumentNullException(nameof(subscriberId));
            if (eventIntentId == null) throw new ArgumentNullException(nameof(eventIntentId));
            if (dtoType == null) throw new ArgumentNullException(nameof(dtoType));
            ThrowIfNotExists(subscriberId);
            ThrowIfNotExists(eventIntentId);
            _appletSubscriptionKeys.Add(new AppletSubscriptionKey(subscriberId, eventIntentId, dtoType));
        }

        public void EnableBroadcast(AppletId senderAppletId, MessageIntentId eventIntentId, Type dtoType)
        {
            if (senderAppletId == null) throw new ArgumentNullException(nameof(senderAppletId));
            if (eventIntentId == null) throw new ArgumentNullException(nameof(eventIntentId));
            if (dtoType == null) throw new ArgumentNullException(nameof(dtoType));
            _appletBroadcastKeys.Add(new AppletBroadcastKey(senderAppletId, eventIntentId, dtoType));
        }

        public void EnableResponseStream(AppletId appletId,
            MessageIntentId requestIntentId,
            Type requestType,
            MessageIntentId responseIntentId,
            Type responseType)
        {
            _appletResponseStreamKeys.Add(new AppletResponseStreamKey(appletId, requestIntentId, requestType,
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

        public void EnableResponseStream(
            AppletId appletId, 
            MessageIntentId requestIntentId, 
            Type requestType,
            Action<ResponseStreamKeyBuilder> responses)
        {
            var responseStreamKeyBuilder = new ResponseStreamKeyBuilder(appletId, requestIntentId, requestType);
            responses.Invoke(responseStreamKeyBuilder);
            foreach (var key in responseStreamKeyBuilder.Keys)
            {
                _appletResponseStreamKeys.Add(key);
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
                _appletBroadcastKeys,
                _appletSubscriptionKeys,
                _appletResponseStreamKeys);
        }



        private void Assert()
        {
            if (_messageIntentsById.Count == 0)
            {
                throw new AppContractBuilderException(
                    new StringBuilder("Message intent registrations are missing.")
                        .Append($" Use {nameof(AddMessageIntent)} method to register the required message intents."));
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
