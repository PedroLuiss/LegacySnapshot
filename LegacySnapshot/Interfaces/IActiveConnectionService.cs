using LegacySnapshot.Models;

namespace LegacySnapshot.Interfaces;

public interface IActiveConnectionService
{
    ConnectionModel? ActiveConnection { get; }
    Task SetActiveAsync(ConnectionModel connection);
    void ClearActiveIf(string connectionId);
    Task<string> GetConnectionStringAsync();
}
