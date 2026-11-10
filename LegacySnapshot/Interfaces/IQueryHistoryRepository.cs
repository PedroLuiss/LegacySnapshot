using LegacySnapshot.Models;

namespace LegacySnapshot.Interfaces;

public interface IQueryHistoryRepository
{
    Task<IReadOnlyList<QueryHistoryItem>> GetRecentAsync();
    Task AddAsync(QueryHistoryItem item);
}
