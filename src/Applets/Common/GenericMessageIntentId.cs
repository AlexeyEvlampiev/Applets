using System.Collections.Generic;
using System.Diagnostics;

namespace Applets.Common
{
    public sealed class GenericMessageIntentId<T> : MessageIntentId
    {
        [DebuggerStepThrough]
        public GenericMessageIntentId(T identifier) : base(identifier, EqualityComparer<T>.Default)
        {

        }
    }
}
