using System.Collections.Generic;
using System.Diagnostics;

namespace Applets.Common
{
    public sealed class GenericAppletId<T> : AppletId
    {
        [DebuggerStepThrough]
        public GenericAppletId(T reference) : base(reference, EqualityComparer<T>.Default)
        {
        }
    }
}
