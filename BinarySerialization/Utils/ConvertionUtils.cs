using System;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BinarySerialization.Utils
{
    public static class ConvertionUtils
    {
        private static readonly Encoding StringEncoding = Encoding.UTF8;
        private static readonly Type[] TypesWithCustomConverter = { typeof(byte), typeof(decimal) };

        private static MethodInfo FindGetBytesMethod(Type type)
        {
            var converterMethods = typeof(BitConverter).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(c => c.Name == "GetBytes");
            var targetMethod = converterMethods.SingleOrDefault(c => c.GetParameters().All(x => x.ParameterType == type));

            return targetMethod;
        }

        private static MethodInfo FindGetValueMethod(Type type)
        {
            return typeof(BitConverter).GetMethods(BindingFlags.Static | BindingFlags.Public).SingleOrDefault(c => c.Name == "To" + type.Name);
        }

        public static bool CanConvertPrimitiveType(Type type)
        {
            return TypesWithCustomConverter.Contains(type) || FindGetBytesMethod(type) != null && FindGetValueMethod(type) != null;
        }

        public static byte[] GetBytes(object value)
        {
            Contract.Requires<ArgumentNullException>(value != null);

            var valueType = value.GetType();

            if(TypesWithCustomConverter.Contains(valueType))
            {
                return GetBytesCustom(value);
            }

            var targetMethod = FindGetBytesMethod(valueType);

            if(targetMethod != null)
            {
                return targetMethod.Invoke(null, new[] { value }) as byte[];
            }

            return null;
        }

        private static byte[] GetBytesCustom(object value)
        {
            Contract.Requires<NotSupportedException>(TypesWithCustomConverter.Contains(value.GetType()));

            var type = value.GetType();
            if(type == typeof(decimal))
            {
                var bits = Decimal.GetBits((decimal)value);
                var result = new byte[bits.Length * sizeof(int)];
                Buffer.BlockCopy(bits, 0, result, 0, result.Length);

                return result;
            }
            if(type == typeof(byte))
            {
                return new[] { (byte)value };
            }

            throw new Exception("Unexpected exception");
        }

        public static byte[] GetStringBytes(string value)
        {
            Contract.Requires<ArgumentNullException>(value != null);

            return StringEncoding.GetBytes(value);
        }

        public static object GetValue(Type type, byte[] bytes)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            Contract.Requires<ArgumentNullException>(bytes != null && bytes.Length > 0);

            if(TypesWithCustomConverter.Contains(type))
            {
                return GetValueCustom(type, bytes);
            }

            var converterMethod = FindGetValueMethod(type);
            if(converterMethod != null)
            {
                return converterMethod.Invoke(null, new object[] { bytes, 0 });
            }

            return null;
        }

        private static object GetValueCustom(Type type, byte[] bytes)
        {
            Contract.Requires<NotSupportedException>(TypesWithCustomConverter.Contains(type));

            if(type == typeof(decimal))
            {
                var i1 = BitConverter.ToInt32(bytes, 0);
                var i2 = BitConverter.ToInt32(bytes, 4);
                var i3 = BitConverter.ToInt32(bytes, 8);
                var i4 = BitConverter.ToInt32(bytes, 12);

                return new decimal(new[] { i1, i2, i3, i4 });
            }
            if(type == typeof(byte))
            {
                return bytes[0];
            }

            throw new Exception("Unexpected exception");
        }

        public static string GetString(byte[] bytes)
        {
            Contract.Requires<ArgumentNullException>(bytes != null && bytes.Length > 0);

            return StringEncoding.GetString(bytes);
        }

        public static Array ConvertListToArray(IList list)
        {
            var itemType = TypeUtils.GetEnumerableItemType(list.GetType());
            var array = TypeUtils.CreateArray(itemType, list.Count);
            list.CopyTo(array, 0);

            return array;
        }
    }
}