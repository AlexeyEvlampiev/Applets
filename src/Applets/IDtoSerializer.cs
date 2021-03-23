using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Applets
{
    public interface IDtoSerializer
    {
        Task<DtoPackage> PackAsync(IDto dto);

        Task<IDto> UnpackAsync(DtoPackage dtoPackage);

        [DebuggerStepThrough]
        public Task<IDto> UnpackAsync(Guid dataContractId, byte[] content) => UnpackAsync(new DtoPackage(dataContractId, content));

        [DebuggerStepThrough]
        public static IDtoSerializer CreateDefaultSerializer(IEnumerable<Assembly> dtoAssemblies)
            => Common.DtoSerializer.CreateDefaultSerializer(dtoAssemblies);

        [DebuggerStepThrough]
        public static IDtoSerializer CreateDefaultSerializer(Assembly assembly)
            => Common.DtoSerializer.CreateDefaultSerializer(assembly);

        [DebuggerStepThrough]
        public static IDtoSerializer CreateDefaultSerializer(params Assembly[] dtoAssemblies)
            => Common.DtoSerializer.CreateDefaultSerializer(dtoAssemblies);
    }
}
