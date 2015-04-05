using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BinarySerialization.Utils;

namespace BinarySerialization.Readers
{
    public class UnsafeBinaryReader : IBinaryReader
    {
        private byte[] _temporaryBuffer;

        public byte[] TemporaryBuffer
        {
            get { return _temporaryBuffer ?? (_temporaryBuffer = new byte[sizeof(decimal)]); }
        }

        public object ReadObject(Type type, Stream stream)
        {
            Contract.Requires<ArgumentNullException>(stream != null);

            Trace.WriteLine("*** BEGIN READING ***");
            try
            {
                return ReadObjectInternal(type, stream);
            }
            finally
            {
                Trace.WriteLine("*** END READING ***");
            }
        }

        private object ReadObjectInternal(Type type, Stream stream)
        {
            if(type != typeof(object))
            {
                object resultObject;

                var objectType = TypeUtils.DetermineObjectType(type);
                if(objectType == ObjectType.Class || objectType == ObjectType.Struct)
                {
                    resultObject = ReadCompositeObject(type, stream);
                }
                else
                {
                    TraceUtils.WriteLineFormatted("Reading plain object of type: {0}", type.FullName);
                    resultObject = ReadPlainObject(type, stream);
                }

                return resultObject;
            }

            TraceUtils.WriteLineFormatted("Unable to read object of type \"{0}\": Unsupported", type.FullName);
            return null;
        }

        private object ReadCompositeObject(Type type, Stream stream)
        {
            TraceUtils.WriteLineFormatted("Reading composite object of type: {0}", type.FullName);
            var resultingObject = Activator.CreateInstance(type);

            var props = resultingObject.GetProperties();

            if(props.Any())
            {
                foreach(var prop in props)
                {
                    TraceUtils.WriteLineFormatted("Reading sub-object of property {0} with type: {1}", prop.Name, prop.PropertyType.FullName);
                    var value = ReadPlainObject(prop.PropertyType, stream);
                    prop.SetValue(resultingObject, value);
                }
            }
            else
            {
                Trace.WriteLine("Composite object hasn't any read\\write properties");
            }

            return resultingObject;
        }

        private object ReadPlainObject(Type type, Stream stream)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            Contract.Requires<ArgumentNullException>(stream != null);

            object result = null;

            var objectType = TypeUtils.DetermineObjectType(type);
            switch(objectType)
            {
                case ObjectType.Primitive:
                    result = ReadPrimitive(type, stream);
                    break;
                case ObjectType.Nullable:
                    result = ReadNullable(type, stream);
                    break;
                case ObjectType.String:
                    result = ReadString(stream);
                    break;
                case ObjectType.Class:
                    result = ReadClass(type, stream);
                    break;
                case ObjectType.Struct:
                    result = ReadStruct(type, stream);
                    break;
                case ObjectType.Enumerable:
                    result = ReadEnumerable(type, stream);
                    break;
                case ObjectType.Enum:
                    result = ReadEnum(type, stream);
                    break;
                case ObjectType.Unsupported:
                    TraceUtils.WriteLineFormatted("Unsupported value of type: \"{0}\"", type.FullName);
                    break;
            }

            return result;
        }

        private object ReadPrimitive(Type type, Stream stream)
        {
            var bytesToRead = Marshal.SizeOf(type);
            ReadStream(stream, TemporaryBuffer, bytesToRead);

            return ConvertionUtils.GetValue(type, TemporaryBuffer);
        }

        private object ReadNullable(Type type, Stream stream)
        {
            var isNull = ReadNullFlag(stream);
            if(!isNull)
            {
                return ReadPlainObject(Nullable.GetUnderlyingType(type), stream);
            }

            return null;
        }

        private object ReadString(Stream stream)
        {
            var isNull = ReadNullFlag(stream);
            if(!isNull)
            {
                var length = (int)ReadPrimitive(typeof(int), stream);
                if(length > 0)
                {
                    var bytes = new byte[length];
                    ReadStream(stream, bytes, length);
                    return ConvertionUtils.GetString(bytes);
                }

                return String.Empty;
            }

            return null;
        }

        private object ReadEnumerable(Type type, Stream stream)
        {
            var elementType = TypeUtils.GetEnumerableItemType(type);

            var supportedElementType = true;
            if(elementType != null)
            {
                supportedElementType = TypeUtils.IsSupportedElementType(elementType);
            }

            if(elementType != null && supportedElementType)
            {
                var isNull = ReadNullFlag(stream);

                if(!isNull)
                {
                    var result = TypeUtils.CreateList(elementType);

                    var count = (int)ReadPrimitive(typeof(int), stream);

                    if(count > 0)
                    {
                        Trace.WriteLine("Begin reading enumerable");

                        for(var i = 0; i < count; i++)
                        {
                            var item = ReadPlainObject(elementType, stream);
                            result.Add(item);
                        }

                        Trace.WriteLine("End reading enumerable");
                    }
                    else
                    {
                        Trace.WriteLine("Enumerable is empty");
                    }

                    if(!type.IsArray)
                    {
                        return result;
                    }

                    return ConvertionUtils.ConvertListToArray(result);
                }
            }
            else if(elementType != null)
            {
                TraceUtils.WriteLineFormatted("Unable to read Enumerable of type \"{0}\": Unsupported element type \"{1}\"", type, elementType);
            }
            else
            {
                TraceUtils.WriteLineFormatted("Unable to read Enumerable of type \"{0}\": Unsupported", type);
            }


            return null;
        }

        private object ReadClass(Type type, Stream stream)
        {
            var isNull = ReadNullFlag(stream);
            if(!isNull)
            {
                return ReadObjectInternal(type, stream);
            }

            return null;
        }

        private object ReadStruct(Type type, Stream stream)
        {
            return ReadObjectInternal(type, stream);
        }

        private object ReadEnum(Type type, Stream stream)
        {
            return ReadPrimitive(Enum.GetUnderlyingType(type), stream);
        }

        private bool ReadNullFlag(Stream stream)
        {
            return stream.ReadByte() == 0;
        }

        private void ReadStream(Stream stream, byte[] dst, int length)
        {
            stream.Read(dst, 0, length);
        }
    }
}