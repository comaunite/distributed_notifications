namespace NotificationApi.Models;

internal record Result
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}