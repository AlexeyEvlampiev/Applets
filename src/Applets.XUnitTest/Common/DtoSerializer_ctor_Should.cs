using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Applets.InMemory;
using Xunit;

namespace Applets.Common
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class DtoSerializer_ctor_Should
    {
        public sealed class TargetDto : IDto
        {
        }


        sealed class TargetSerializer : InMemoryDtoSerializer
        {
        }

        [Fact]
        public async Task AutoDiscoverCurrentAssemblyDtoClasses()
        {
            IDtoSerializer target = new TargetSerializer();
            IDto dto = new TargetDto();
            var package = await target.PackAsync(new TargetDto());
            Assert.Equal(dto.DataContractId, package.DataContractId);
        }
    }
}
