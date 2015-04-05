using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using BinarySerialization.Utils;

namespace BinarySerialization.Writers
{
    public sealed class UnsafeBinaryWriter : IBinaryWriter
    {
        private byte[] _temporaryBuffer;

        public byte[] TemporaryBuffer
        {
            get { return _temporaryBuffer ?? (_temporaryBuffer = new byte[sizeof(decimal)]); }
        }

        public void WriteObject(object obj, Stream stream)
        {
            Contract.Requires<ArgumentNullException>(obj != null);
            Contract.Requires<ArgumentNullException>(stream != null);

            Trace.WriteLine("*** BEGIN WRITING ***");

            WriteObjectInternal(obj, stream);

            Trace.WriteLine("*** END WRITING ***");
        }

        private void WriteObjectInternal(object obj, Stream stream)
        {
            Contract.Requires<ArgumentNullException>(obj != null);

            var type = obj.GetType();
            if(type != typeof(object))
            {
                var objectType = TypeUtils.DetermineObjectType(type);
                if(objectType == ObjectType.Class || objectType == ObjectType.Struct)
                {
                    WriteCompositeObject(obj, type, stream);
                }
                else
                {
                    TraceUtils.WriteLineFormatted("Writing plain object of type: {0}", type.FullName);
                    WritePlainObject(obj, type, stream);
                }
            }
            else
            {
                TraceUtils.WriteLineFormatted("Unable to write object of type \"{0}\": Unsupported", type.FullName);
            }
        }

        private void WriteCompositeObject(object obj, Type type, Stream stream)
        {
            TraceUtils.WriteLineFormatted("Writing composite object of type: {0}", type.FullName);

            var props = obj.GetProperties();

            if(props.Any())
            {
                foreach(var prop in props)
                {
                    var value = prop.GetValue(obj);

                    TraceUtils.WriteLineFormatted("Writing sub-object of property {0} with type: {1}", prop.Name, prop.PropertyType.FullName);
                    WritePlainObject(value, prop.PropertyType, stream);
                }
            }
            else
            {
                Trace.WriteLine("Composite object hasn't any read\\write properties");
            }
        }

        private void WritePlainObject(object value, Type type, Stream stream)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            Contract.Requires<ArgumentNullException>(stream != null);


            if(value != null)
            {
                var objectType = TypeUtils.DetermineObjectType(type);

                switch(objectType)
                {
                    case ObjectType.Primitive:
                        WritePrimitive(value, stream);
                        break;
                    case ObjectType.Nullable:
                        WriteNullableValueType(value, stream);
                        break;
                    case ObjectType.String:
                        WriteString((string)value, stream);
                        break;
                    case ObjectType.DateTime:
                        WriteDateTime((DateTime)value, stream);
                        break;
                    case ObjectType.Class:
                        WriteClass(value, stream);
                        break;
                    case ObjectType.Struct:
                        WriteStruct(value, stream);
                        break;
                    case ObjectType.Enumerable:
                        WriteEnumerable(value, type, stream);
                        break;
                    case ObjectType.Enum:
                        WriteEnum(value, stream);
                        break;
                    case ObjectType.Unsupported:
                        TraceUtils.WriteLineFormatted("Unsupported value of type: \"{0}\"", type.FullName);
                        break;
                }
            }
            else
            {
                WriteNullFlag(true, stream);
            }
        }

        private void WritePrimitive(object value, Stream stream)
        {
            var affected = ConvertionUtils.Convert(value, TemporaryBuffer);
            WriteBytes(TemporaryBuffer, affected, stream);
        }

        private void WriteNullableValueType(object value, Stream stream)
        {
            var valueType = value.GetType();

            WriteNullFlag(false, stream);
            WritePlainObject(value, valueType, stream);
        }

        private void WriteString(string value, Stream stream)
        {
            var stringLength = value.Length;
            WriteNullFlag(false, stream);


            if(stringLength > 0)
            {
                var stringBytes = ConvertionUtils.GetStringBytes(value);
                WritePrimitive(stringBytes.Length, stream);
                WriteBytes(stringBytes, stringBytes.Length, stream);
            }
            else
            {
                WritePrimitive(0, stream);
            }
        }

        private void WriteDateTime(DateTime value, Stream stream)
        {
            WritePrimitive(value.Ticks, stream);
        }

        private void WriteEnumerable(object value, Type type, Stream stream)
        {
            var elementType = TypeUtils.GetEnumerableItemType(type);

            var supportedElementType = true;
            if(elementType != null)
            {
                supportedElementType = TypeUtils.IsSupportedElementType(elementType);
            }

            if(elementType != null && supportedElementType)
            {
                var enumerable = (IEnumerable)value;

                // ReSharper disable PossibleMultipleEnumeration
                var supportedEnumerable = enumerable.Cast<object>().All(item => item == null || item.GetType() == elementType);

                if(supportedEnumerable)
                {
                    Trace.WriteLine("Begin writing enumerable");
                    WriteNullFlag(false, stream);

                    using(var internalStream = new MemoryStream())
                    {
                        var count = 0;
                        foreach(var item in enumerable)
                        {
                            WritePlainObject(item, elementType, internalStream);
                            ++count;
                        }

                        WriteBytes(TemporaryBuffer, ConvertionUtils.Convert(count, TemporaryBuffer), stream);

                        if(count > 0)
                        {
                            internalStream.WriteTo(stream);
                        }
                    }
                    // ReSharper restore PossibleMultipleEnumeration

                    Trace.WriteLine("End writing enumerable");
                }
                else
                {
                    WriteNullFlag(true, stream);
                    TraceUtils.WriteLineFormatted("Unable to write Enumerable of type \"{0}\": All elements must be of one type", type);
                }
            }
            else if(elementType != null)
            {
                TraceUtils.WriteLineFormatted("Unable to write Enumerable of type \"{0}\": Unsupported element type \"{1}\"", type, elementType);
            }
            else
            {
                TraceUtils.WriteLineFormatted("Unable to write Enumerable of type \"{0}\": Unsupported", type);
            }
        }

        private void WriteEnum(object value, Stream stream)
        {
            var targetType = Enum.GetUnderlyingType(value.GetType());
            WritePrimitive(Convert.ChangeType(value, targetType), stream);
        }

        private void WriteClass(object value, Stream stream)
        {
            WriteNullFlag(false, stream);
            WriteObjectInternal(value, stream);
        }

        private void WriteStruct(object value, Stream stream)
        {
            WriteObjectInternal(value, stream);
        }

        private void WriteNullFlag(bool isNull, Stream stream)
        {
            stream.WriteByte((byte)(isNull ? 0 : 1));
        }

        private void WriteBytes(byte[] bytes, int length, Stream stream)
        {
            Contract.Requires<ArgumentOutOfRangeException>(length <= bytes.Length);
            stream.Write(bytes, 0, length);
        }
    }
}