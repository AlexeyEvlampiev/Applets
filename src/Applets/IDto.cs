using System;

namespace Applets
{
    public interface IDto
    {
        public Guid DataContractId => GetType().GUID;
    }
}
