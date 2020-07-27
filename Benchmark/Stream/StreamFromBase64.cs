using Base64;
using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark.Stream
{
    [MemoryDiagnoser]
    public class StreamFromBase64
    {
        private static Data[] data = Setup();

        private static Data[] Setup()
        {
            const int count = 10;
            var d = new Data[count];

            for (int i = 0; i < count; i++)
            {
                int pow = (int)Math.Pow(2, i+8);
                var s = new string('a', pow);
                var ms = new MemoryStream();

                s = s.ToUtf8Base64String();

                d[i] = new Data()
                {
                    Stream = ms,
                    String = s,
                    Count = pow
                };
            }

            return d;
        }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Input))]
        public void ConvertFrom(Data input)
        {
            input.Stream.Position = 0;

            byte[] decodedBytes = Convert.FromBase64String(input.String);
            Encoding.UTF8.GetString(decodedBytes);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Input))]
        public void StreamFrom(Data input)
        {
            input.Stream.Position = 0;
            input.Stream.WriteFromBase64(input.String);
        }


        public static IEnumerable<Data> Input()
        {
            yield return data[0];
            yield return data[1];
            yield return data[2];
            yield return data[3];
            yield return data[4];
            yield return data[5];
            yield return data[6];
            yield return data[7];
        }

        public class Data
        {
            public MemoryStream Stream { get; set; }

            public string String { get; set; }

            public int Count { get; set; }

            public override string ToString()
            {
                return Count.ToString();
            }
        }
    }
}
