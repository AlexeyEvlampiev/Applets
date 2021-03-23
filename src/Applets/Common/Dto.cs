using System;
using System.Threading.Tasks;

namespace Applets.Common
{
    public abstract class Dto : IDto
    {
        protected virtual Guid ContractId => GetType().GUID;

        Guid IDto.DataContractId => this.ContractId;
    }
}
