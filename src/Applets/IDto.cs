using System;
using System.Threading.Tasks;

namespace Applets
{
    public interface IDto
    {
        Guid DataContractId { get; }
    }
}
