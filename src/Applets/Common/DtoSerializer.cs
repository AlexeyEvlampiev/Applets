using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Applets.InMemory;

namespace Applets.Common
{
    public abstract class DtoSerializer : IDtoSerializer
    {
        protected interface IConstructionCallback
        {
            Guid GetDataContractId(Type dtoType);
            bool CanSerialize(Type dtoType);
        }


        private readonly Lazy<JsonSerializerOptions> _defaultJsonSerializerOptions;
        private readonly Lazy<Encoding> _defaultEncoding;
        private readonly Dictionary<Guid, Type> _dtoTypesByDataContractId;



        [DebuggerStepThrough]
        protected DtoSerializer() 
            : this(Enumerable.Empty<Assembly>())
        {
        }

        [DebuggerStepThrough]
        protected DtoSerializer(IConstructionCallback ctorCallback)
            : this(Enumerable.Empty<Assembly>(), ctorCallback ?? throw new ArgumentNullException(nameof(ctorCallback)))
        {
        }

        [DebuggerStepThrough]
        protected DtoSerializer(IEnumerable<Assembly> dtoAssemblies) 
            : this(dtoAssemblies?.ToHashSet())
        {
        }

        [DebuggerStepThrough]
        protected DtoSerializer(IEnumerable<Assembly> dtoAssemblies, IConstructionCallback ctorCallback)
            : this(dtoAssemblies?.ToHashSet(), ctorCallback ?? throw new ArgumentNullException(nameof(ctorCallback)))
        {
        }

        private DtoSerializer(
            HashSet<Assembly> assemblies, 
            IConstructionCallback ctorCallback = null)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));
            ctorCallback ??= new DefaultConstructionCallback();
            _defaultJsonSerializerOptions = new Lazy<JsonSerializerOptions>(GetDefaultJsonSerializerOptions);
            _defaultEncoding = new Lazy<Encoding>(()=> GetDefaultEncoding() ?? Encoding.UTF8);

            assemblies.Add(GetType().Assembly);
            _dtoTypesByDataContractId = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(type => type.IsAssignableTo(typeof(IDto)))
                .Where(type => type.IsAbstract == false)
                .Where(ctorCallback.CanSerialize)
                .ToDictionary(type => ctorCallback.GetDataContractId(type));
        }

        [DebuggerStepThrough]
        public static DtoSerializer CreateDefaultSerializer(IEnumerable<Assembly> dtoAssemblies)
            => CreateDefaultSerializer(dtoAssemblies?.ToArray() ?? throw new ArgumentNullException(nameof(dtoAssemblies)));

        [DebuggerStepThrough]
        public static DtoSerializer CreateDefaultSerializer(Assembly assembly)
            => CreateDefaultSerializer(new Assembly[]{ assembly ?? throw new ArgumentNullException(nameof(assembly)) } );


        [DebuggerStepThrough]
        public static DtoSerializer CreateDefaultSerializer(params Assembly[] dtoAssemblies)
        {
            if (dtoAssemblies == null) throw new ArgumentNullException(nameof(dtoAssemblies));
            if(dtoAssemblies.Length == 0) throw new ArgumentException($"DTO assemblies collection may not be empty.", nameof(dtoAssemblies));
            Array.ForEach(dtoAssemblies, assembly =>
            {
                if (assembly is null) throw new ArgumentException("Collection contains null references");
            });
            return new InMemoryDtoSerializer(dtoAssemblies);
        }

        sealed class DefaultConstructionCallback : IConstructionCallback
        {
            Guid IConstructionCallback.GetDataContractId(Type dtoType) => dtoType.GUID;

            bool IConstructionCallback.CanSerialize(Type dtoType)
            {
                var ctor = dtoType.GetConstructor(Array.Empty<Type>());
                return (ctor?.IsPublic == true);
            }
        }
        
        sealed class PersistedStoreDto : IDto
        {
            private static readonly Guid DataContractId = Guid.Parse("a4edfc93-9dad-4576-b146-dba0aecc983c");
            public Guid PersistedBlobId { get; }
            public Guid PersistedDataContractId { get; }

            public PersistedStoreDto(Guid persistedBlobId, Guid persistedDataContractId)
            {
                PersistedBlobId = persistedBlobId;
                PersistedDataContractId = persistedDataContractId;
            }

            private async Task<byte[]> SerializeAsync()
            {
                await using var stream = new MemoryStream();
                await using var writer = new BinaryWriter(stream);
                writer.Write(PersistedBlobId.ToByteArray());
                writer.Write(PersistedDataContractId.ToByteArray());
                writer.Flush();
                await stream.FlushAsync();
                return stream.ToArray();
            }


            private static PersistedStoreDto FromBytes(byte[] bytes)
            {
                if (bytes == null) throw new ArgumentNullException(nameof(bytes));
                using var stream = new MemoryStream(bytes);
                using var reader = new BinaryReader(stream);
                reader.ReadBytes(16);
                var blobId = new Guid(reader.ReadBytes(16));
                var contractId = new Guid(reader.ReadBytes(16));
                return new PersistedStoreDto(blobId, contractId);
            }

            public async Task<DtoPackage> PackAsync()
            {
                var content = await SerializeAsync();
                return new DtoPackage(DataContractId, content);
            }

            Guid IDto.DataContractId => DataContractId;

            public static bool TryUnpack(DtoPackage package, out PersistedStoreDto dto)
            {
                if (package.DataContractId == DataContractId)
                {
                    dto = FromBytes(package.Content);
                    return true;
                }

                dto = null;
                return false;
            }
        }

        protected virtual JsonSerializerOptions GetDefaultJsonSerializerOptions() => new();
        protected virtual Encoding GetDefaultEncoding() => Encoding.UTF8;

        protected virtual Task<byte[]> SerializeAsync(IDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            var json = JsonSerializer.Serialize(dto, _defaultJsonSerializerOptions.Value);
            var bytes = (_defaultEncoding.Value ?? Encoding.UTF8).GetBytes(json);
            return Task.FromResult(bytes);
        }


        protected virtual Task<IDto> DeserializeAsync(byte[] content, Type returnType)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var json = (_defaultEncoding.Value ?? Encoding.UTF8).GetString(content);
            var dto = (IDto)JsonSerializer.Deserialize(json, returnType, _defaultJsonSerializerOptions.Value);
            return Task.FromResult(dto);
        }

        


        protected abstract Task<IDto> UploadToPersistedBlobStoreAsync(Guid dataContractId, byte[] bytes);
        protected abstract Task<byte[]> DownloadFromPersistedBlobStoreAsync(Guid persistedBlobId);

        protected abstract int MaxMessageBodyBytes { get; }


        [DebuggerStepThrough]
        async Task<DtoPackage> IDtoSerializer.PackAsync(IDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            var bytes = await SerializeAsync(dto) ?? throw new NullReferenceException();
            if (bytes.Length < MaxMessageBodyBytes)
            {
                return new DtoPackage(dto.DataContractId, bytes);
            }

            var persistedBlobId = Guid.NewGuid();
            await UploadToPersistedBlobStoreAsync(persistedBlobId, bytes);
            var blobStorageDto = new PersistedStoreDto(persistedBlobId, dto.DataContractId);
            return await blobStorageDto.PackAsync();
        }

        public async Task<IDto> UnpackAsync(DtoPackage dtoPackage)
        {
            if (PersistedStoreDto.TryUnpack(dtoPackage, out var persistedStoreDto))
            {
                dtoPackage = new DtoPackage(
                    persistedStoreDto.PersistedDataContractId,
                    await DownloadFromPersistedBlobStoreAsync(persistedStoreDto.PersistedBlobId));
            }

            if (_dtoTypesByDataContractId.TryGetValue(dtoPackage.DataContractId, out var type))
            {
                var dto = await this.DeserializeAsync(dtoPackage.Content, type);
                return dto;
            }

            throw new InvalidOperationException();
        }
    }
}
