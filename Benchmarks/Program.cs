// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using Benchmarks.Benchmarks;

Console.WriteLine("100kg BENCH or GO HOME!");

BenchmarkRunner.Run<DeduplicationIdBenchmark>();