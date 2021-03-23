using System;

namespace Applets
{
    public readonly struct DtoPackage
    {
        public Guid DataContractId { get; }
        public byte[] Content { get; }

        public DtoPackage(Guid dataContractId, byte[] content)
        {
            DataContractId = dataContractId;
            Content = content;
        }
    }
}
