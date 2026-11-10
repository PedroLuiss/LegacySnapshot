using System.Text.Json;
using LegacySnapshot.Interfaces;
using LegacySnapshot.Models;

namespace LegacySnapshot.Services;

public sealed class LocalQueryHistoryRepository : IQueryHistoryRepository
{
    private const string HistoryKey = "query_history";
    private const int MaxHistoryItems = 20;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<QueryHistoryItem>> GetRecentAsync()
    {
        var json = Preferences.Default.Get(HistoryKey, "[]");
        var items = JsonSerializer.Deserialize<List<QueryHistoryItem>>(json, JsonOptions) ?? [];
        return Task.FromResult<IReadOnlyList<QueryHistoryItem>>(items);
    }

    public async Task AddAsync(QueryHistoryItem item)
    {
        var items = (await GetRecentAsync()).ToList();
        items.Insert(0, item);
        items = items.Take(MaxHistoryItems).ToList();
        Preferences.Default.Set(HistoryKey, JsonSerializer.Serialize(items, JsonOptions));
    }
}
