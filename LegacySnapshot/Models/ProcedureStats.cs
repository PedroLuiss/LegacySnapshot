namespace LegacySnapshot.Models;

public sealed class ProcedureStats
{
    public int ObjectId { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public long ExecutionCount { get; set; }
    public double TotalElapsedMs { get; set; }
    public double AverageElapsedMs { get; set; }
    public double MinElapsedMs { get; set; }
    public double MaxElapsedMs { get; set; }
    public long TotalCpuMs { get; set; }
    public long TotalLogicalReads { get; set; }
    public bool HasExecutionStats { get; set; }
    public bool HasNoExecutionStats => !HasExecutionStats;
}
