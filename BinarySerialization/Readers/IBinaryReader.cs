using System;
using System.IO;

namespace BinarySerialization.Readers
{
    public interface IBinaryReader
    {
        object ReadObject(Type type, Stream stream);
    }
}
