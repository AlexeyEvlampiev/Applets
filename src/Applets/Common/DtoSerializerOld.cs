using System;
using System.Text.Json;

namespace Applets.Common
{
    public abstract class DtoSerializerOld
    {
        public static readonly DtoSerializerOld Default = new JsonDtoSerializer();

        public abstract byte[] Serialize(object dto, out string contentType);
        public abstract object Deserialize(byte[] bytes, Type returnType);

        sealed class JsonDtoSerializer : DtoSerializerOld
        {
            static readonly JsonSerializerOptions _options = new JsonSerializerOptions();

            public override byte[] Serialize(object dto, out string contentType)
            {
                contentType = "application/json";
                return JsonSerializer.SerializeToUtf8Bytes(dto, _options);
            }

            public override object Deserialize(byte[] bytes, Type returnType) => JsonSerializer.Deserialize(bytes, returnType);
        }

       
    }
}
