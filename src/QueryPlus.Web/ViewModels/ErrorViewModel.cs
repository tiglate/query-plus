namespace QueryPlus.Web.ViewModels;

public sealed class ErrorViewModel
{
    public int StatusCodeValue { get; init; } = 500;
    public string? RequestId { get; init; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public bool IsNotFound => StatusCodeValue == 404;
    public bool IsServerError => StatusCodeValue is >= 500 and <= 599;
}
