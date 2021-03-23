using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Applets.Common
{
    public abstract class DtoSerializer : IDtoSerializer
    {
        private const string PersistedBlobStorageDataContractGuid = "a4edfc93-9dad-4576-b146-dba0aecc983c";
        private static readonly Guid PersistedBlobStorageDataContractId = Guid.Parse(PersistedBlobStorageDataContractGuid);

        protected delegate Task<byte[]> SerializationHandler(IDto dto);
        protected delegate Task<IDto> DeserializationHandler(DtoPackage package);

        private SerializationHandler _serializationHandler;
        private DeserializationHandler _deserializationHandler;


        [DebuggerStepThrough]
        protected DtoSerializer() 
            : this(Enumerable.Empty<Assembly>())
        {
        }

        [DebuggerStepThrough]
        protected DtoSerializer(IEnumerable<Assembly> dtoAssemblies) 
            : this(dtoAssemblies?.ToHashSet())
        {
        }

        private DtoSerializer(HashSet<Assembly> assemblies)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));
            assemblies.Add(GetType().Assembly);
            var dtoTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(type => type.IsAssignableTo(typeof(IDto)))
                .Where(type => type.IsAbstract == false)
                .ToHashSet();


            var serializationHandlersByType = new Lazy<Dictionary<Type, SerializationHandler>>(
                () => BuildSerializationHandlersByTypeDictionary(dtoTypes));
            var deserializationHandlersByDataContractId = new Lazy<Dictionary<Guid, DeserializationHandler>>(
                ()=> BuildDeserializationHandlersByDataContractDictionary(dtoTypes));

            _serializationHandler = (dto) =>
            {
                if (serializationHandlersByType
                    .Value
                    .TryGetValue(dto.GetType(), out var handler))
                {
                    return handler.Invoke(dto);
                }

                throw new InvalidOperationException();
            };

            _deserializationHandler = (package) =>
            {
                if (deserializationHandlersByDataContractId
                    .Value
                    .TryGetValue(package.DataContractId, out var handler))
                {
                    return handler.Invoke(package);
                }

                throw new InvalidOperationException();
            };
        }

        
        [Guid(PersistedBlobStorageDataContractGuid)]
        sealed class PersistedStoreDto : Dto
        {
            public Guid PersistedBlobId { get; }
            public Guid PersistedDataContractId { get; }


            public PersistedStoreDto(Guid persistedBlobId, Guid persistedDataContractId)
            {
                PersistedBlobId = persistedBlobId;
                PersistedDataContractId = persistedDataContractId;
            }

            public async Task<byte[]> SerializeAsync()
            {
                await using var stream = new MemoryStream();
                await using var writer = new BinaryWriter(stream);
                writer.Write(PersistedBlobId.ToByteArray());
                writer.Write(PersistedDataContractId.ToByteArray());
                writer.Flush();
                await stream.FlushAsync();
                return stream.ToArray();
            }


            public static PersistedStoreDto FromBytes(byte[] bytes)
            {
                if (bytes == null) throw new ArgumentNullException(nameof(bytes));
                using var stream = new MemoryStream(bytes);
                using var reader = new BinaryReader(stream);
                reader.ReadBytes(16);
                var blobId = new Guid(reader.ReadBytes(16));
                var contractId = new Guid(reader.ReadBytes(16));
                return new PersistedStoreDto(blobId, contractId);
            }
        }


        protected virtual bool CanSerialize(Type dtoType)
        {
            var ctor = dtoType.GetConstructor(Array.Empty<Type>());
            return (ctor?.IsPublic == true);
        }

        protected virtual SerializationHandler GetSerializationHandler(Type dtoType)
        {
            return dto =>
            {
                var json = JsonSerializer.Serialize(dto);
                var bytes = Encoding.UTF8.GetBytes(json);
                return Task.FromResult(bytes);
            };
        }

        protected virtual DeserializationHandler GetDeserializationHandler(Type dtoType)
        {
            return package =>
            {
                var json = Encoding.UTF8.GetString(package.Content);
                var dto = (IDto)JsonSerializer.Deserialize(json, dtoType);
                return Task.FromResult(dto);
            };
        }

        protected abstract Task<IDto> UploadToPersistedBlobStoreAsync(Guid dataContractId, byte[] bytes);
        protected abstract Task<byte[]> DownloadFromPersistedBlobStoreAsync(Guid persistedBlobId);

        protected abstract int MaxMessageBodyBytes { get; }


        [DebuggerStepThrough]
        async Task<DtoPackage> IDtoSerializer.PackAsync(IDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            var bytes = await _serializationHandler.Invoke(dto) ?? throw new NullReferenceException();
            if (bytes.Length < MaxMessageBodyBytes)
            {
                return new DtoPackage(dto.DataContractId, bytes);
            }

            var persistedBlobId = Guid.NewGuid();
            await UploadToPersistedBlobStoreAsync(persistedBlobId, bytes);
            var blobStorageDto = new PersistedStoreDto(persistedBlobId, dto.DataContractId);
            return new DtoPackage(PersistedBlobStorageDataContractId, await blobStorageDto.SerializeAsync());
        }

        public async Task<IDto> UnpackAsync(DtoPackage dtoPackage)
        {
            if (dtoPackage.DataContractId == PersistedBlobStorageDataContractId)
            {
                var persistedStoreDto = PersistedStoreDto.FromBytes(dtoPackage.Content);
                dtoPackage = new DtoPackage(
                    persistedStoreDto.PersistedDataContractId,
                    await DownloadFromPersistedBlobStoreAsync(persistedStoreDto.PersistedBlobId));
            }

            return await _deserializationHandler.Invoke(dtoPackage);
        }


        private Dictionary<Type, SerializationHandler> BuildSerializationHandlersByTypeDictionary(IEnumerable<Type> dtoTypes)
        {
            if (dtoTypes == null) throw new ArgumentNullException(nameof(dtoTypes));
            var map = dtoTypes
                .Where(CanSerialize)
                .Select(type => KeyValuePair.Create(type, GetSerializationHandler(type)))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            map.Add(typeof(PersistedStoreDto), dto =>
            {
                var persistedStoreDto = (PersistedStoreDto) dto;
                return persistedStoreDto.SerializeAsync();
            });
            return map;
        }

        private Dictionary<Guid, DeserializationHandler> BuildDeserializationHandlersByDataContractDictionary(IEnumerable<Type> dtoTypes)
        {
            if (dtoTypes == null) throw new ArgumentNullException(nameof(dtoTypes));
            var map = dtoTypes
                .Where(CanSerialize)
                .Select(type => KeyValuePair.Create(type, GetDeserializationHandler(type)))
                .ToDictionary(pair => pair.Key.GUID, pair => pair.Value);
            map.Add(typeof(PersistedStoreDto).GUID, package =>
            {
                var persistedStoreDto = (IDto)PersistedStoreDto.FromBytes(package.Content);
                return Task.FromResult(persistedStoreDto);
            });
            return map;
        }
    }
}
