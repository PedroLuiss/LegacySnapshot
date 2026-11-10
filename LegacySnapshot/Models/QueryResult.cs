namespace LegacySnapshot.Models;

public sealed class QueryResult
{
    public List<string> Columns { get; set; } = [];
    public List<List<string?>> Rows { get; set; } = [];
    public int RowsAffected { get; set; }
    public long DurationMs { get; set; }
    public bool IsResultSet { get; set; }
    public bool IsTruncated { get; set; }
}
