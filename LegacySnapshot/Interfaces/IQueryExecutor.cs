using LegacySnapshot.Models;

namespace LegacySnapshot.Interfaces;

public interface IQueryExecutor
{
    Task<QueryResult> ExecuteAsync(
        string connectionString,
        string query,
        CancellationToken cancellationToken = default);
}
