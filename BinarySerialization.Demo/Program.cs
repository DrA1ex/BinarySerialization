using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;

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

            Trace.Listeners.Add(new ConsoleTraceListener());

            using(var stream = new MemoryStream())
            {
                var binarySerializer = new BinarySerializer(BinarySerializationMethod.UnsafeSerialization);
                binarySerializer.Serialize(data, stream);

                stream.Seek(0, SeekOrigin.Begin);

                Console.WriteLine("Serialized size: {0} bytes", stream.Length);
                Console.WriteLine();
                Console.WriteLine("Press any key to deserialize");
                Console.ReadKey(true);

                // ReSharper disable once UnusedVariable
                var deserialized = binarySerializer.Deserialize<TestClass>(stream);

                Console.WriteLine("Press any key to exit");
                Console.ReadKey(true);
            }
        }
    }
}