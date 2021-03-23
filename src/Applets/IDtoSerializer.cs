using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Applets
{
    public interface IDtoSerializer
    {
        Task<DtoPackage> PackAsync(IDto dto);

        Task<IDto> UnpackAsync(DtoPackage dtoPackage);

        [DebuggerStepThrough]
        public Task<IDto> UnpackAsync(Guid dataContractId, byte[] content) => UnpackAsync(new DtoPackage(dataContractId, content));
    }
}
