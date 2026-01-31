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
    public string DeterministicGuidToString()
    {
        var guid = GuidHelper.CreateDeterministic(notificationId, userId, deliveryChannel);

        return guid.ToString();
    }

    [Benchmark]
    public string DeterministicGuidToByteArrayThenToString()
    {
        var guid = GuidHelper.CreateDeterministic(notificationId, userId, deliveryChannel);

        var bytes = guid.ToByteArray();

        return new Guid(bytes).ToString();
    }

    [Benchmark]
    public string StringConcatenation()
    {
        return $"{notificationId}_{userId}_{deliveryChannel}";
    }
}