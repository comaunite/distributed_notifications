using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Common;

namespace Benchmarks.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public class DeduplicationIdBenchmark
{
    private Guid notificationId;
    private Guid userId;
    private int deliveryChannel;

    [GlobalSetup]
    public void Setup()
    {
        notificationId = Guid.NewGuid();
        userId = Guid.NewGuid();
        deliveryChannel = 1;
    }

    [Benchmark]
    public Guid DeterministicGuid()
    {
        return GuidHelper.CreateDeterministic(notificationId, userId, deliveryChannel);
    }

    [Benchmark]
    public string StringConcatenation()
    {
        return $"{notificationId}_{userId}_{deliveryChannel}";
    }
}