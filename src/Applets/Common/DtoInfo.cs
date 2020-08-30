using System;

namespace Applets.Common
{
    struct DtoInfo
    {
        public Type DtoType { get; }

        public Guid Contract => DtoType.GUID;

        public DtoSerializer Serializer { get; }

        public DtoInfo(Type dtoType, DtoSerializer serializer) : this()
        {
            DtoType = dtoType ?? throw new ArgumentNullException(nameof(dtoType));
            Serializer = serializer ?? DtoSerializer.Default;

        }
    }
}
