using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using BinarySerialization.Utils;

namespace BinarySerialization.Demo
{
    internal enum TestEnum
    {
        A,
        B,
        C
    }

    [Serializable]
    internal class TestClass
    {
        public TestEnum Enum { get; set; }
        public DateTime DateTime { get; set; }
        public TimeSpan DateTimeOffset { get; set; }

        public int[] Array { get; set; }
        public IList<int> List { get; set; }
        public IList<dynamic> Enumerable { get; set; }
        public TestChildObject[] TestChildObjects { get; set; }
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
                           Enum = TestEnum.B,
                           DateTime = DateTime.Now,
                           DateTimeOffset = DateTime.Now - DateTime.Today,
                           Array = new[] { 1, 2, 3, 4, 5 },
                           List = new List<int>(new[] { 6, 7, 8, 9, 10 }),
                           Enumerable = new dynamic[] { 1, "2", null },
                           TestChildObjects = new[]
                                              {
                                                  new TestChildObject
                                                  {
                                                      Decimal = new Decimal(13.14),
                                                      Byte = 15
                                                  },
                                                  null,
                                                  new TestChildObject
                                                  {
                                                      Decimal = new Decimal(13.14),
                                                      Byte = 15
                                                  }
                                              },
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

                long formatterLength;
                using(var ms = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(ms, data);
                    formatterLength = ms.Length;
                }

                Console.WriteLine("Serialized size: {0} bytes (BinaryFormatter: {1} bytes)", stream.Length, formatterLength);

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
                    Console.WriteLine("- {0}", type.Name);
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

            byte[] bytes = new byte[sizeof(decimal)];
            switch(valueType)
            {
                case ObjectType.Primitive:
                primitive:
                    var readed = ConvertionUtils.Convert(value, bytes);
                    Console.WriteLine("{0} (data)", ByteArrayToString(bytes, readed));

                    break;

                case ObjectType.Nullable:
                    Console.Write("0x01 (not-null) ");
                    goto primitive;

                case ObjectType.String:
                    bytes = ConvertionUtils.GetStringBytes(value as String);
                    Console.Write("0x01 (not-null) {0} (length)", ByteArrayToString(BitConverter.GetBytes(bytes.Length), sizeof(int)));
                    if(bytes.Length > 0)
                    {
                        Console.Write(" {0} (data)", ByteArrayToString(bytes, bytes.Length));
                    }
                    Console.WriteLine();
                    break;

                case ObjectType.DateTime:
                    value = ((DateTime)value).Ticks;
                    goto primitive;

                case ObjectType.Class:
                    Console.WriteLine("0x01 (not-null)");
                    PrintHieracy(value, depth + 1);
                    break;
                case ObjectType.Struct:
                    Console.WriteLine();
                    PrintHieracy(value, depth + 1);
                    break;
                case ObjectType.Enum:
                    value = Convert.ChangeType(value, Enum.GetUnderlyingType(propType));
                    goto primitive;

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
                        var supportedEnumerable = enumerable.Cast<object>().All(item => item == null || item.GetType() == elementType);

                        if(supportedEnumerable)
                        {
                            int count = enumerable.Cast<object>().Count();
                            Console.WriteLine("0x01 (not-null) {0} (count)", ByteArrayToString(BitConverter.GetBytes(count), sizeof(int)));

                            if(count > 0)
                            {
                                foreach(var item in enumerable)
                                {
                                    var compositeObject = elementType.IsClass || elementType.IsValueType && !elementType.IsPrimitive;
                                    if(compositeObject)
                                    {
                                        Console.Write("{0}+ {1}: ", GetPadding(depth + 1), elementType.Name);
                                        Console.WriteLine(item != null ? "0x01 (not-null)" : "0x00 (null)");
                                    }

                                    if(item != null)
                                    {
                                        PrintHieracy(item, depth + 1);
                                    }
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

        private static string ByteArrayToString(byte[] bytes, int length)
        {
            return "0x" + String.Join("", bytes.Take(length).Select(c => Convert.ToString(c, 16).PadLeft(2, '0')));
        }
    }
}