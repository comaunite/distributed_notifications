namespace NotificationOrchestrator.Models;

internal class ResultModel
{
    private int totalCount;
    private int failureCount;

    public int TotalCount => totalCount;
    public int FailureCount => failureCount;

    public void IncrementTotal() => Interlocked.Increment(ref totalCount);
    public void IncrementFailure() => Interlocked.Increment(ref failureCount);
}