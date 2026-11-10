namespace LegacySnapshot.Models;

public sealed class QueryHistoryItem
{
    public string Query { get; set; } = string.Empty;
    public DateTimeOffset ExecutedAt { get; set; }
    public long DurationMs { get; set; }
    public int RowsAffected { get; set; }
    public string Summary => $"{ExecutedAt:dd/MM HH:mm} · {DurationMs} ms · {RowsAffected:N0} filas";
}
