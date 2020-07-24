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
    public class ToBase64
    {
        [GlobalSetup]
        public static void Setup()
        {
            foreach (object[] d in Data())
            {
                (d[0] as string).ToUtf8Base64String();
            }
        }

        // StreamTo memory alloc should be something like ConvertTo - GetBytes, since GetBytes should be done via the pooled objects.
        // Need to run under memory profiler and see where allocs come from.

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Data))]
        public string ConvertTo(string input, int count)
        {
            var b = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(b);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public string StreamTo(string input, int count)
        {
            return input.ToUtf8Base64String();
        }

        //[Benchmark]
        //[ArgumentsSource(nameof(Data))]
        //public byte[] GetBytes(string input, int count)
        //{
        //    return Encoding.UTF8.GetBytes(input);
        //}

        public static IEnumerable<object[]> Data()
        {
            yield return new object[] { "", 0 };
            yield return new object[] { new string('a', 4), 4 };
            yield return new object[] { new string('a', 16), 16 };
            yield return new object[] { new string('a', 128), 128 };
            yield return new object[] { new string('a', 256), 256 };
            yield return new object[] { new string('a', 512), 512 };
            yield return new object[] { new string('a', 2048), 2048 };
            yield return new object[] { new string('a', 4096), 4096 };
            //yield return new object[] { new string('a', 6144), 6144 };
            yield return new object[] { new string('a', 8192), 8192 };
            yield return new object[] { new string('a', 16384), 16384 };
            //yield return new object[] { new string('a', 32768), 32768 };
        }
    }
}
