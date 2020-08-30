using System;

namespace Applets.ComponentModel
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IntentAttribute : Attribute
    {
        public IntentAttribute(string guid)
        {
            Intent = Guid.Parse(guid);
            if(Intent == Guid.Empty)
                throw new ArgumentException($"Intent code is required.");
        }

        public Guid Intent { get; }
    }
}
