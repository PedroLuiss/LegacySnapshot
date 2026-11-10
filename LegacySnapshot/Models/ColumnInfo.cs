namespace LegacySnapshot.Models;

public sealed class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string Summary => $"{Name} : {DataType}{(IsNullable ? " NULL" : " NOT NULL")}{(IsPrimaryKey ? " · PK" : string.Empty)}";
}
