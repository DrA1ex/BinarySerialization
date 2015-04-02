using System;
using System.Diagnostics;
using System.IO;
using BinarySerialization.Readers;
using BinarySerialization.Writers;

namespace BinarySerialization
{
    public enum BinarySerializationMethod
    {
        /// <summary>
        ///     Обеспечивает наименьший рамер сериализуемых данных
        ///     и обеспечивает наилучшую производительность,
        ///     но отсутствуют проверки на соотвествие модели.
        ///     Могут возникать непредвиденные ошибки при изменении порядка
        ///     определения членов.
        ///     Сериализации подлежат все открытые свойства объекта
        /// </summary>
        UnsafeSerialization
    }

    public class BinarySerializer
    {
        private readonly IBinaryReader _reader;
        private readonly IBinaryWriter _writer;

        public BinarySerializer(BinarySerializationMethod serializationMethod)
        {
            switch(serializationMethod)
            {
                case BinarySerializationMethod.UnsafeSerialization:
                    _writer = new UnsafeBinaryWriter();
                    _reader = new UnsafeBinaryReader();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("serializationMethod");
            }
        }

        public MemoryStream Serialize(object obj)
        {
            var strem = new MemoryStream();
            Serialize(obj, strem);

            return strem;
        }

        public void Serialize(object obj, Stream stream)
        {
            var typeFullName = obj.GetType().AssemblyQualifiedName;

            Trace.WriteLine("Write object type...");
            _writer.WriteObject(typeFullName, stream);
            Trace.WriteLine("Write object...");
            _writer.WriteObject(obj, stream);
        }

        public T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(typeof(T), stream);
        }

        public object Deserialize(Type objectType, Stream stream)
        {
            var targetTypeFullName = objectType.AssemblyQualifiedName;
            Trace.WriteLine("Read object type...");
            var typeFullName = (string)_reader.ReadObject(typeof(string), stream);

            if(targetTypeFullName == typeFullName)
            {
                Trace.WriteLine("Read object...");
                return _reader.ReadObject(objectType, stream);
            }

            throw new ArgumentException(String.Format("Unable to deserialize object: Expected type \"{0}\" but found \"{1}\"", targetTypeFullName, typeFullName));
        }
    }
}