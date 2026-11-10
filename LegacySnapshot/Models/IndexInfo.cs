namespace LegacySnapshot.Models;

public sealed class IndexInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Columns { get; set; } = string.Empty;
    public bool IsUnique { get; set; }
    public string Summary => $"{Name} ({Type}){(IsUnique ? " UNIQUE" : string.Empty)}: {Columns}";
}
