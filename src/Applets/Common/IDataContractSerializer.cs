using System;

namespace Applets.Common
{
    public interface IDataContractSerializer
    {
        byte[] Serialize(object data);
        object Deserialize(byte[] bytes, Type dataContractType);
    }
}
