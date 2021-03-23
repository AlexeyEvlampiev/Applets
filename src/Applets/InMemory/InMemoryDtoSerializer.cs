using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Applets.Common;

namespace Applets.InMemory
{
    public class InMemoryDtoSerializer : DtoSerializer
    {
        [DebuggerStepThrough]
        public InMemoryDtoSerializer(IEnumerable<Assembly> dtoAssemblies)
            : base(dtoAssemblies)
        {
        }

        [DebuggerStepThrough]
        protected InMemoryDtoSerializer()
        {
        }

        [DebuggerStepThrough]
        protected InMemoryDtoSerializer(IEnumerable<Assembly> dtoAssemblies, IConstructionCallback ctorCallback)
            : base(dtoAssemblies, ctorCallback)
        {
        }


        protected sealed override Task<IDto> UploadToPersistedBlobStoreAsync(Guid dataContractId, byte[] bytes)
        {
            throw new NotSupportedException();
        }

        protected sealed override Task<byte[]> DownloadFromPersistedBlobStoreAsync(Guid persistedBlobId)
        {
            throw new NotSupportedException();
        }

        protected sealed override int MaxMessageBodyBytes =>int.MaxValue;
    }
}
