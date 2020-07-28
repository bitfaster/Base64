using Benchmark.Stream;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner
               .Run<StreamToBase64>(ManualConfig.Create(DefaultConfig.Instance)
               .With(Job.RyuJitX64));
        }
    }
}
