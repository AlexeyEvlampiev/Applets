using System;
using System.Collections;
using System.Diagnostics;

namespace Applets
{
    public abstract class EntityId
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private readonly object _innerId;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IEqualityComparer _comparer;

        protected EntityId(object reference, IEqualityComparer comparer)
        {
            _innerId = reference ?? throw new ArgumentNullException(nameof(reference));
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            if (reference is EntityId)
            {
                throw new ArgumentException($"Objects of the {reference.GetType()} type cannot be used as the entry primary identifier.");
            }
        }

        [DebuggerStepThrough]
        public bool Is(object reference) => Equals(reference);


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected object InnerId => _innerId;

        public virtual bool IsCompatibleWith(EntityId other) => (GetType() == other?.GetType());

        public sealed override int GetHashCode() => _comparer.GetHashCode(_innerId);

        [DebuggerStepThrough]
        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(_innerId, obj)) return true;
            if (obj is EntityId other)
            {
                if (GetType() == other.GetType() || IsCompatibleWith(other))
                {
                    return ReferenceEquals(_comparer, other._comparer)
                        ? _comparer.Equals(_innerId, other._innerId)
                        : _comparer.Equals(_innerId, other._innerId) && other._comparer.Equals(_innerId, other._innerId);
                }

                return false;
            }
                
            return _comparer.Equals(_innerId, obj);
        }

        [DebuggerStepThrough]
        public sealed override string ToString() => _innerId.ToString();

        [DebuggerStepThrough]
        public static bool operator ==(EntityId left, EntityId right) => Equals(left, right);

        [DebuggerStepThrough]
        public static bool operator !=(EntityId left, EntityId right) => !Equals(left, right);
    }
}
