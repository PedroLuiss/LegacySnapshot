namespace LegacySnapshot.Models
{
    public class TableInfo
    {
        public string SchemaName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public long RowCount { get; set; }
        public double TotalSizeMB { get; set; }
        public bool IsEmpty => RowCount == 0;
    }
}