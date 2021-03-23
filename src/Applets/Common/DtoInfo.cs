using System;

namespace Applets.Common
{
    struct DtoInfo
    {
        public Type DtoType { get; }

        public Guid Contract => DtoType.GUID;

        public DtoSerializerOld Serializer { get; }

        public DtoInfo(Type dtoType, DtoSerializerOld serializer) : this()
        {
            DtoType = dtoType ?? throw new ArgumentNullException(nameof(dtoType));
            Serializer = serializer ?? DtoSerializerOld.Default;

        }
    }
}
