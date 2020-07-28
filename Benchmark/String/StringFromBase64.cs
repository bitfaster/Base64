using Base64;
using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class StringFromBase64
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

        // StreamTo memory alloc should be something like ConvertTo - GetBytes, since GetBytes should be done via the pooled objects.
        // Need to run under memory profiler and see where allocs come from.

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Length))]
        public string ConvertFrom(Data data)
        {
            var r = Convert.FromBase64String(data.String);
            return Encoding.UTF8.GetString(r);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Length))]
        public string StreamFrom(Data data)
        {
            return data.String.FromUtf8Base64String();
        }

        //[Benchmark]
        //[ArgumentsSource(nameof(Length))]
        //public string CryptoStreamFrom(Data data)
        //{
        //    return CryptoFromUtf8Base64String(data.String);
        //}

        //[Benchmark]
        //[ArgumentsSource(nameof(Length))]
        //public byte[] GetBytes(Data data)
        //{
        //    return Encoding.UTF8.GetBytes(data.String);
        //}

        // just to demonstrate how bad crypto version is
        public static string CryptoFromUtf8Base64String(string base64)
        {
            var encodingBuffer = PooledUtf8EncodingBuffer.GetInstance();

            try
            {
                using (var s = MemoryStreamFactory.Create(nameof(CryptoFromUtf8Base64String)))
                using (CryptoStream base64Stream = new CryptoStream(s, new System.Security.Cryptography.FromBase64Transform(), CryptoStreamMode.Write))
                {
                    using (var writer = new BufferedStreamWriter(base64Stream, encodingBuffer, true))
                    {
                        writer.Write(base64);
                        writer.Flush();
                    }

                    base64Stream.FlushFinalBlock();

                    s.TryGetBuffer(out var buffer);
                    return encodingBuffer.Encoding.GetString(buffer.Array, buffer.Offset, buffer.Count);
                }
            }
            finally
            {
                PooledUtf8EncodingBuffer.Free(encodingBuffer);
            }
        }

        public static IEnumerable<Data> Length()
        {
            for (int i = 0; i < count; i++)
                yield return data[i];
        }

        public class Data
        {
            public Data(int count)
            {
                this.String = new string('a', count).ToUtf8Base64String();
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
