namespace LegacySnapshot.Models;

public sealed class TableDetails
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public double TotalSizeMB { get; set; }
    public double DataSizeMB { get; set; }
    public double IndexSizeMB { get; set; }
    public List<ColumnInfo> Columns { get; set; } = [];
    public List<IndexInfo> Indexes { get; set; } = [];
}
