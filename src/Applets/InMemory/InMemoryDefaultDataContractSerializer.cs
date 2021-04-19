using System;
using System.Text;
using System.Text.Json;
using Applets.Common;

namespace Applets.InMemory
{
    sealed class InMemoryDefaultDataContractSerializer : IDataContractSerializer
    {
        private readonly Encoding _encoding = Encoding.UTF8;
        public byte[] Serialize(object data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var json = JsonSerializer.Serialize(data);
            return _encoding.GetBytes(json);
        }

        public object Deserialize(byte[] bytes, Type dataContractType)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (dataContractType == null) throw new ArgumentNullException(nameof(dataContractType));
            var json = _encoding.GetString(bytes);
            return JsonSerializer.Deserialize(json, dataContractType);
        }
    }
}
