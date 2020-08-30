using System;

namespace Applets.ComponentModel
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class DtoSerializerAttribute : Attribute
    {
        public Type SerializerType { get; }

        public DtoSerializerAttribute(Type serializerType)
        {
            SerializerType = serializerType ?? throw new ArgumentNullException(nameof(serializerType));
        }
    }
}
