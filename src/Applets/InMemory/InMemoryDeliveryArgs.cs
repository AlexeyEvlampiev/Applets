using System;
using System.Text;
using System.Threading.Tasks;
using Applets.Common;

namespace Applets.InMemory
{
    sealed class InMemoryDeliveryArgs : IReplyArgs, IEventArgs
    {
        private readonly IDataContractSerializer _serializer;
        private readonly IAppContract _appContract;
        private readonly byte[] _body;
        private readonly Type _dtoType;

        public InMemoryDeliveryArgs(
            Applet sender, 
            MessageIntent messageIntent, 
            object data, 
            IDataContractSerializer serializer,
            IAppContract appContract)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _appContract = appContract ?? throw new ArgumentNullException(nameof(appContract));
            MessageIntent = messageIntent ?? throw new ArgumentNullException(nameof(messageIntent));
            SenderApplet = sender ?? throw new ArgumentNullException(nameof(sender));
            SentUtc = DateTime.UtcNow;
            _body = _serializer.Serialize(data);
            _dtoType = data.GetType();
        }

        public InMemoryDeliveryArgs Clone()
        {
            return new InMemoryDeliveryArgs(SenderApplet, MessageIntent, Data, _serializer, _appContract)
            {
                SessionId = SessionId,
                SentUtc = SentUtc,
                TimeToLive = TimeToLive
            };
        }

        public MessageIntent MessageIntent { get; set; }


        public DateTime SentUtc { get; set; }

        public TimeSpan? TimeToLive { get; set; }

        public object Data => _serializer.Deserialize(_body, _dtoType);

        public Applet SenderApplet { get; set; }

        public Guid SessionId { get; set; }

        public Action<InMemoryDeliveryArgs> ReplyHandler { get; set; }

        public Func<MessageIntent, object, InMemoryDeliveryArgs> ReplyMessageFactory { get; set; }

        public DateTime? ExpirationUtc { get; set; }

        public async Task ReactAsync(MessageIntentId reactionIntentId, object data)
        {
            if (reactionIntentId == null) throw new ArgumentNullException(nameof(reactionIntentId));
            if (data == null) throw new ArgumentNullException(nameof(data));
            var intent = _appContract.GetIntent(reactionIntentId);
            var reply = ReplyMessageFactory(intent, data);
            ReplyHandler.Invoke(reply);
            await Task.CompletedTask;
        }

        public override string ToString()
        {
            return new StringBuilder($"Intent: {this.MessageIntent.Name}.")
                .Append($" Sent by {SenderApplet.Name}.")
                .ToString();
        }
    }
}
