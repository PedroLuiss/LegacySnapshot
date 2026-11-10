using LegacySnapshot.Interfaces;
using LegacySnapshot.Models;

namespace LegacySnapshot.Services;

public sealed class ActiveConnectionService(IConnectionRepository connectionRepository) : IActiveConnectionService
{
    public ConnectionModel? ActiveConnection { get; private set; }

    public Task SetActiveAsync(ConnectionModel connection)
    {
        ActiveConnection = connection;
        return Task.CompletedTask;
    }

    public void ClearActiveIf(string connectionId)
    {
        if (ActiveConnection?.Id == connectionId)
        {
            ActiveConnection = null;
        }
    }

    public async Task<string> GetConnectionStringAsync()
    {
        if (ActiveConnection is null)
        {
            throw new InvalidOperationException("Selecciona primero una conexión a SQL Server.");
        }

        var password = ActiveConnection.AuthenticationType == "Windows"
            ? null
            : await connectionRepository.GetPasswordAsync(ActiveConnection.Id);
        if (ActiveConnection.AuthenticationType != "Windows" && password is null)
        {
            throw new InvalidOperationException("No se encontraron las credenciales guardadas de la conexión activa.");
        }

        return SqlConnectionStringFactory.Create(ActiveConnection, password);
    }
}
