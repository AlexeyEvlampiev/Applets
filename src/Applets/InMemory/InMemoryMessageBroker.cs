using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using Applets.Common;

namespace Applets.InMemory
{
    public sealed class InMemoryMessageBroker
    {
        private readonly IDataContractSerializer _serializer;
        private readonly IAppContract _constraints;
        private readonly Subject<InMemoryDeliveryArgs> _topic = new Subject<InMemoryDeliveryArgs>();

        [DebuggerStepThrough]
        public InMemoryMessageBroker() 
            : this(new AppContractNullObject(), new InMemoryDefaultDataContractSerializer())
        {
            
        }

        [DebuggerStepThrough]
        public InMemoryMessageBroker(IAppContract constraints)
            : this(constraints ?? throw new ArgumentNullException(nameof(constraints)), new InMemoryDefaultDataContractSerializer())
        {
            
        }

        [DebuggerStepThrough]
        public InMemoryMessageBroker(IDataContractSerializer serializer)
            : this(new AppContractNullObject(), serializer ?? throw new ArgumentNullException(nameof(serializer)))
        {

        }

        public InMemoryMessageBroker(IAppContract constraints, IDataContractSerializer serializer)
        {
            _constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }



        public IAppletChannel CreateAppletChannel(AppletId appletId)
        {
            if (appletId == null) throw new ArgumentNullException(nameof(appletId));
            return new InMemoryAppletChannel(appletId, _topic, _constraints, _serializer);
        }
    }
}
