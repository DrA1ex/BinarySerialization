using System;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace BinarySerialization.Utils
{
    internal static class ConvertionUtils
    {
        private static readonly Encoding StringEncoding = Encoding.UTF8;

        internal static int Convert(object value, byte[] dst)
        {
            Contract.Requires<ArgumentNullException>(value != null);
            Contract.Requires<ArgumentException>(value.GetType().IsValueType);
            Contract.Requires<ArgumentNullException>(dst != null);
            Contract.Requires<ArgumentOutOfRangeException>(dst.Length >= Marshal.SizeOf(value));

            unsafe
            {
                fixed(byte* bytes = dst)
                {
                    Marshal.StructureToPtr(value, (IntPtr)bytes, false);
                }

                return Marshal.SizeOf(value);
            }
        }

        internal static byte[] GetStringBytes(string value)
        {
            Contract.Requires<ArgumentNullException>(value != null);

            return StringEncoding.GetBytes(value);
        }

        internal static object GetValue(Type type, byte[] bytes)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            Contract.Requires<ArgumentException>(type.IsValueType);
            Contract.Requires<ArgumentNullException>(bytes != null);
            Contract.Requires<ArgumentOutOfRangeException>(bytes.Length >= Marshal.SizeOf(type));


            unsafe
            {
                fixed(byte* data = bytes)
                {
                    return Marshal.PtrToStructure((IntPtr)data, type);
                }
            }
        }

        internal static string GetString(byte[] bytes)
        {
            Contract.Requires<ArgumentNullException>(bytes != null && bytes.Length > 0);

            return StringEncoding.GetString(bytes);
        }

        internal static Array ConvertListToArray(IList list)
        {
            var itemType = TypeUtils.GetEnumerableItemType(list.GetType());
            var array = TypeUtils.CreateArray(itemType, list.Count);
            list.CopyTo(array, 0);

            return array;
        }
    }
}