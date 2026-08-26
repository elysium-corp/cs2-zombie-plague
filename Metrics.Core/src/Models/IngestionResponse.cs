namespace Metrics.Core.Models;

internal sealed record IngestionResponse
{
    public int Accepted { get; init; }

    public int Duplicates { get; init; }

    public int Rejected { get; init; }

    public IReadOnlyCollection<IngestionError> Errors { get; init; } = [];
}

internal sealed record IngestionError
{
    public int Index { get; init; }

    public string? EventId { get; init; }

    public string? Code { get; init; }

    public string? Field { get; init; }

    public string? Message { get; init; }
}
