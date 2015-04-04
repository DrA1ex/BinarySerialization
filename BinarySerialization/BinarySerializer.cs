using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

            byte[] typeFullNameBytes;
            using(var md5 = MD5.Create())
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                typeFullNameBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(typeFullName));
            }

            Debug.WriteLine("Write object type...");
            _writer.WriteObject(typeFullNameBytes, stream);
            Debug.WriteLine("Write object...");
            _writer.WriteObject(obj, stream);
        }

        public T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(typeof(T), stream);
        }

        public object Deserialize(Type objectType, Stream stream)
        {
            var targetTypeFullName = objectType.AssemblyQualifiedName;
            Debug.WriteLine("Read object type...");
            var typeFullNameBytes = (byte[])_reader.ReadObject(typeof(byte[]), stream);

            byte[] targetTypeFullNameBytes;
            using(var md5 = MD5.Create())
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                targetTypeFullNameBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(targetTypeFullName));
            }

            if(targetTypeFullNameBytes.SequenceEqual(typeFullNameBytes))
            {
                Debug.WriteLine("Read object...");
                return _reader.ReadObject(objectType, stream);
            }

            var targetTypeBinaryString = String.Join("", targetTypeFullNameBytes.Select(x => Convert.ToString(x, 16).PadLeft(2, '0')));
            var typeBinaryString = String.Join("", typeFullNameBytes.Select(x => Convert.ToString(x, 16).PadLeft(2, '0')));

            throw new ArgumentException(String.Format("Unable to deserialize object: Wrong type hash \"{0}\" expected \"{1}\"", typeBinaryString, targetTypeBinaryString));
        }
    }
}