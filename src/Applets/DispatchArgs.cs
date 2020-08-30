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

        public Guid Correlation { get; set; }

        public Guid Contract { get; set; }

        public Guid Intent { get; set; }

        public Guid Applet { get; internal set; }

        public Guid To { get; set; }

        public Guid From { get; internal set; }

        public string ContentType { get; internal set; }

        public bool HasCorrelationId => (Correlation != Guid.Empty);
    }
}
