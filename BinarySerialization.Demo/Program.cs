using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using BinarySerialization.Utils;

namespace BinarySerialization.Demo
{
    [Serializable]
    internal class TestClass
    {
        public int[] Array { get; set; }
        public IList<int> List { get; set; }
        public IList<dynamic> Enumerable { get; set; }
        public int Int { get; set; }
        public int? NullableInt { get; set; }
        public string String { get; set; }
        public double Double { get; set; }
        public TestChildObject ChildObj { get; set; }
        public TestStruct Struct { get; set; }
        public TestClass TestClassObj { get; set; }
        public Complex UnsupportedType { get; set; }
    }

    [Serializable]
    internal struct TestStruct
    {
        public long Long { get; set; }
        public float Float { get; set; }
    }

    [Serializable]
    internal class TestChildObject
    {
        public decimal Decimal { get; set; }
        public byte Byte { get; set; }
    }

    internal class Program
    {
        private static void Main()
        {
            Console.SetBufferSize(160, 300);
            Console.SetWindowSize(160, 40);

            var data = new TestClass
                       {
                           Array = new[] { 1, 2, 3, 4, 5 },
                           List = new List<int>(new[] { 6, 7, 8, 9, 10 }),
                           Enumerable = new dynamic[] { 1, "2", null },
                           Int = 11,
                           NullableInt = null,
                           String = "Бла-бла длинный текст",
                           Double = 12,
                           ChildObj = new TestChildObject
                                      {
                                          Decimal = new Decimal(13.14),
                                          Byte = 15
                                      },
                           Struct = new TestStruct
                                    {
                                        Long = 16,
                                        Float = 17
                                    },
                           TestClassObj = new TestClass
                                          {
                                              Array = new[] { 1, 2, 3, 4, 5 },
                                              List = new List<int>(new[] { 6, 7, 8, 9, 10 }),
                                              Enumerable = new dynamic[] { 1, "2", null },
                                              Int = 11,
                                              NullableInt = 1234,
                                              String = "Бла-бла длинный текст",
                                              Double = 12,
                                              ChildObj = new TestChildObject
                                                         {
                                                             Decimal = new Decimal(13.14),
                                                             Byte = 15
                                                         },
                                              Struct = new TestStruct
                                                       {
                                                           Long = 16,
                                                           Float = 17
                                                       }
                                          }
                       };

            Trace.Listeners.Add(new ConsoleTraceListener()); //Для отображения отладочного вывода сериализатора

            Console.WriteLine("Data will be writed as follow:");
            Console.WriteLine();

            PrintHieracy(data);

            using(var stream = new MemoryStream())
            {
                var binarySerializer = new BinarySerializer(BinarySerializationMethod.UnsafeSerialization);

                Console.WriteLine();
                Console.WriteLine("Press any key to serialize");
                Console.ReadKey(true);
                binarySerializer.Serialize(data, stream);

                stream.Seek(0, SeekOrigin.Begin);
                Console.WriteLine("Serialized size: {0} bytes", stream.Length);

                Console.WriteLine();
                Console.WriteLine("Press any key to deserialize");
                Console.ReadKey(true);

                // ReSharper disable once UnusedVariable
                var deserialized = binarySerializer.Deserialize<TestClass>(stream);

                Console.WriteLine();
                Console.WriteLine("Press any key to exit");
                Console.ReadKey(true);
            }
        }

        private static void PrintHieracy(object obj, int depth = 0)
        {
            var type = obj.GetType();
            if(type.IsClass || type.IsValueType && !type.IsPrimitive)
            {
                var props = obj.GetProperties();

                if(depth == 0)
                {
                    Console.WriteLine("- {0}", obj.GetType().Name);
                }

                depth += 1;

                if(props.Any())
                {
                    foreach(var prop in props)
                    {
                        var value = prop.GetValue(obj);
                        var propType = prop.PropertyType;

                        Console.Write("{0}+ ({1}) {2}: ", GetPadding(depth), prop.Name, propType.Name);
                        if(value != null)
                        {
                            PrintValue(value, propType, depth);
                        }
                        else
                        {
                            Console.WriteLine("0x00 (null)");
                        }
                    }
                }
                else
                {
                    if(depth > 0)
                    {
                        Console.WriteLine("{0}* No read\\write properties (skip)", GetPadding(depth));
                    }
                }
            }
            else
            {
                Console.Write("{0}+ ({1}): ", GetPadding(depth), type.Name);
                PrintValue(obj, obj.GetType(), depth);
            }
        }

        private static void PrintValue(object value, Type propType, int depth)
        {
            var valueType = TypeUtils.DetermineObjectType(propType);

            byte[] bytes;
            switch(valueType)
            {
                case ObjectType.Primitive:
                primitive:
                    bytes = ConvertionUtils.GetBytes(value);
                    if(bytes != null)
                    {
                        Console.WriteLine("{0} (data)", ByteArrayToString(bytes));
                    }
                    else
                    {
                        Console.WriteLine("unsupported");
                    }

                    break;

                case ObjectType.Nullable:
                    Console.Write("0x01 (not-null) ");
                    goto primitive;

                case ObjectType.String:
                    bytes = ConvertionUtils.GetStringBytes(value as String);
                    Console.Write("0x01 (not-null) {0} (length)", ByteArrayToString(BitConverter.GetBytes(bytes.Length)));
                    if(bytes.Length > 0)
                    {
                        Console.Write(" {0} (data)", ByteArrayToString(bytes));
                    }
                    Console.WriteLine();
                    break;

                case ObjectType.Class:
                    Console.WriteLine("0x01 (not-null)");
                    PrintHieracy(value, depth + 1);
                    break;
                case ObjectType.Struct:
                    Console.WriteLine();
                    PrintHieracy(value, depth + 1);
                    break;
                case ObjectType.Enumerable:
                    var elementType = TypeUtils.GetEnumerableItemType(propType);

                    var supportedElementType = true;
                    if(elementType != null)
                    {
                        supportedElementType = TypeUtils.IsSupportedElementType(elementType);
                    }

                    if(elementType != null && supportedElementType)
                    {
                        var enumerable = (IEnumerable)value;

                        // ReSharper disable PossibleMultipleEnumeration
                        var supportedEnumerable = enumerable.Cast<object>().All(item => item.GetType() == elementType);

                        if(supportedEnumerable)
                        {
                            var count = enumerable.Cast<object>().Count();
                            Console.WriteLine("0x01 (not-null) {0} (count)", ByteArrayToString(BitConverter.GetBytes(count)));

                            if(count > 0)
                            {
                                foreach(var item in enumerable)
                                {
                                    PrintHieracy(item, depth + 1);
                                }
                            }
                        }
                        else
                        {
                            Console.Write("0x00 (null) (elements has different type)");
                        }
                    }
                    else if(elementType != null)
                    {
                        Console.WriteLine("unsupported element type ({0})", elementType.Name);
                    }
                    else
                    {
                        Console.WriteLine("unsupported type");
                    }
                    break;
                case ObjectType.Unsupported:
                    Console.WriteLine("unsupported type");
                    break;
            }
        }

        private static string GetPadding(int depth)
        {
            if(depth > 0)
            {
                return String.Join("", Enumerable.Repeat("  ", depth));
            }

            return String.Empty;
        }

        private static string ByteArrayToString(byte[] bytes)
        {
            return "0x" + String.Join("", bytes.Select(c => Convert.ToString(c, 16).PadLeft(2, '0')));
        }
    }
}