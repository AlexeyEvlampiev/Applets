using System;

namespace Applets
{
    public class DispatchArgs
    {
        public DispatchArgs(byte[] body)
        {
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }


        public byte[] Body { get; }

        public Guid CorrelationId { get; set; }

        public Guid DataContractId { get; set; }

        public Guid IntentId { get; set; }

        public Guid AppletId { get; internal set; }

        public Guid To { get; set; }

        public Guid From { get; internal set; }

        public string ContentType { get; internal set; }

        public bool HasCorrelationId => (CorrelationId != Guid.Empty);
    }
}
