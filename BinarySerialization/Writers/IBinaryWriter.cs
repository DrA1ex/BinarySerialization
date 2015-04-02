using System.IO;

namespace BinarySerialization.Writers
{
    public interface IBinaryWriter
    {
        void WriteObject(object obj, Stream stream);
    }
}
