using System.Text.Json;
using LegacySnapshot.Interfaces;
using LegacySnapshot.Models;

namespace LegacySnapshot.Services;

public sealed class LocalConnectionRepository : IConnectionRepository
{
    private const string ConnectionsKey = "saved_sql_connections";
    private const string PasswordKeyPrefix = "sql_connection_password_";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<ConnectionModel>> GetAllAsync()
    {
        var json = Preferences.Default.Get(ConnectionsKey, "[]");
        var connections = JsonSerializer.Deserialize<List<ConnectionModel>>(json, JsonOptions)
            ?? [];

        return Task.FromResult<IReadOnlyList<ConnectionModel>>(connections);
    }

    public async Task SaveAsync(ConnectionModel connection, string password)
    {
        if (connection.AuthenticationType == "Windows")
        {
            SecureStorage.Default.Remove(GetPasswordKey(connection.Id));
        }
        else
        {
            await SecureStorage.Default.SetAsync(GetPasswordKey(connection.Id), password);
        }

        var connections = (await GetAllAsync()).ToList();
        var existingIndex = connections.FindIndex(item => item.Id == connection.Id);
        if (existingIndex >= 0)
        {
            connections[existingIndex] = connection;
        }
        else
        {
            connections.Add(connection);
        }

        Preferences.Default.Set(ConnectionsKey, JsonSerializer.Serialize(connections, JsonOptions));
    }

    public Task<string?> GetPasswordAsync(string connectionId)
    {
        return SecureStorage.Default.GetAsync(GetPasswordKey(connectionId));
    }

    public async Task DeleteAsync(string connectionId)
    {
        var connections = (await GetAllAsync())
            .Where(connection => connection.Id != connectionId)
            .ToList();

        Preferences.Default.Set(ConnectionsKey, JsonSerializer.Serialize(connections, JsonOptions));
        SecureStorage.Default.Remove(GetPasswordKey(connectionId));
    }

    private static string GetPasswordKey(string connectionId)
    {
        return $"{PasswordKeyPrefix}{connectionId}";
    }
}
