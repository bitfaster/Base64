using Base64;
using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class StringToBase64
    {
        private static Data[] data = Setup();
        const int count = 10;

        private static Data[] Setup()
        {
            var d = new Data[count];

            for (int i = 0; i < count; i++)
            {
                int pow = (int)Math.Pow(2, i + 8);

                d[i] = new Data(pow);
            }

            return d;
        }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Length))]
        public string ConvertTo(Data data)
        {
            var b = Encoding.UTF8.GetBytes(data.String);
            return Convert.ToBase64String(b);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Length))]
        public string StreamTo(Data data)
        {
            return data.String.ToUtf8Base64String();
        }

        //[Benchmark]
        //[ArgumentsSource(nameof(Length))]
        //public byte[] GetBytes(Data data)
        //{
        //    return Encoding.UTF8.GetBytes(data.String);
        //}

        public static IEnumerable<Data> Length()
        {
            for (int i = 0; i < count; i++)
                yield return data[i];
        }

        public class Data
        {
            public Data(int count)
            {
                this.String = new string('a', count);
                this.Count = count;
            }

            public string String { get; set; }

            public int Count { get; set; }

            public override string ToString()
            {
                return Count.ToString();
            }
        }
    }
}
