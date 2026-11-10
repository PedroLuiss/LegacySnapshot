using LegacySnapshot.Models;

namespace LegacySnapshot.Interfaces;

public interface IConnectionRepository
{
    Task<IReadOnlyList<ConnectionModel>> GetAllAsync();
    Task SaveAsync(ConnectionModel connection, string password);
    Task<string?> GetPasswordAsync(string connectionId);
    Task DeleteAsync(string connectionId);
}
