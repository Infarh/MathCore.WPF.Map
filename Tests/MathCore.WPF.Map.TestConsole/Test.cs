using System;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace MathCore.WPF.Map.TestConsole
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
    [RankColumn]
    public class Test
    {
        private const double x = 2;
        private const int n = 79;

        [Benchmark] public double Pow20() => Math.Pow(x, n);

        [Benchmark] public double CustomPow20() => CustomPow(x, n);
        [Benchmark] public double CustomPowOpt20() => CustomPowOpt(x, n);
        [Benchmark] public double CustomPowInl20() => CustomPowInlining(x, n);

        [Benchmark]
        public double Iteration20()
        {
            var result = 1d;
            for (var i = 0; i < n; i++)
                result *= x;
            return result;
        }

        private static double CustomPow(double x, int n)
        {
            var result = 1d;
            for (var i = 0; i < n; i++)
                result *= x;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CustomPowInlining(double x, int n)
        {
            var result = 1d;
            for (var i = 0; i < n; i++)
                result *= x;
            return result;
        }

        private static double CustomPowOpt(double x, int n)
        {
            var k = x;
            var result = 1d;
            if (n > 20)
            {
                for (var i = 0; i < 20; i++) k *= x;

                var n1 = n / 20;
                for (var i = 0; i < n1; i++)
                    result *= k;

                n1 = n % 20;
                for (var i = 0; i < n1; i++)
                    result *= x;
            }
            else
                for (var i = 0; i < n; i++)
                    result *= k;
            return result;
        }
    }
}