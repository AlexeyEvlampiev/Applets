using System.Collections;
using System.Diagnostics;
using Applets.Common;

namespace Applets
{
    public abstract class MessageIntentId : EntityId
    {
        [DebuggerStepThrough]
        protected MessageIntentId(object reference, IEqualityComparer comparer)
            : base(reference, comparer)
        {
            
        }

        [DebuggerStepThrough]
        public static GenericMessageIntentId<T> Create<T>(T identifier) =>
            new GenericMessageIntentId<T>(identifier);

        public static implicit operator MessageIntentId(MessageIntent intent) => intent?.Id;
    }

}
