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
    public class StringFromBase64Loh
    {
        private static readonly string lohString = new string('a', 500000).ToUtf8Base64String();

        [GlobalSetup]
        public static void Setup()
        {
            lohString.FromUtf8Base64String();
        }

        [Benchmark(Baseline = true)]
        public string ConvertFrom()
        {
            var r = Convert.FromBase64String(lohString);
            return Encoding.UTF8.GetString(r);
        }

        [Benchmark]
        public string StreamFrom()
        {
            return lohString.FromUtf8Base64String();
        }

        [Benchmark]
        public byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes(lohString);
        }
    }
}
