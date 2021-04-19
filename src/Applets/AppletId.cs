using System.Collections;
using System.Diagnostics;
using Applets.Common;

namespace Applets
{
    public abstract class AppletId : EntityId
    {
        [DebuggerStepThrough]
        protected AppletId(object reference, IEqualityComparer comparer) : base(reference, comparer)
        {
        }

        public static implicit operator AppletId(Applet applet) => applet?.Id;

        [DebuggerStepThrough]
        public static GenericAppletId<T> Create<T>(T reference) =>
            new(reference);
    }
}
