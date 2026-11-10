using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LegacySnapshot.Interfaces;
using LegacySnapshot.Models;
using LegacySnapshot.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LegacySnapshot.ViewModels;

public partial class ConnectionsViewModel(
    IConnectionRepository connectionRepository,
    IDatabaseRepository databaseRepository,
    IActiveConnectionService activeConnectionService,
    ILogger<ConnectionsViewModel> logger) : ObservableObject
{
    public ObservableCollection<ConnectionModel> Connections { get; } = [];
    public IReadOnlyList<string> Environments { get; } = ["Dev", "Staging", "Prod"];
    public IReadOnlyList<string> AuthenticationTypes { get; } =
        DeviceInfo.Platform == DevicePlatform.WinUI
            ? ["SQL Server", "Windows"]
            : ["SQL Server"];

    [ObservableProperty]
    public partial string SelectedAuthenticationType { get; set; } = "SQL Server";

    public bool UsesSqlAuthentication => SelectedAuthenticationType == "SQL Server";

    partial void OnSelectedAuthenticationTypeChanged(string value)
    {
        OnPropertyChanged(nameof(UsesSqlAuthentication));
    }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsStatusError { get; set; }

    [ObservableProperty]
    public partial bool IsFormVisible { get; set; }

    [ObservableProperty]
    public partial string FormTitle { get; set; } = "Nueva conexión";

    [ObservableProperty]
    public partial string ConnectionName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Server { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Database { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedEnvironment { get; set; } = "Dev";

    [ObservableProperty]
    public partial bool TrustServerCertificate { get; set; }

    public bool CanTrustServerCertificate => SelectedEnvironment == "Dev";

    partial void OnSelectedEnvironmentChanged(string value)
    {
        OnPropertyChanged(nameof(CanTrustServerCertificate));
        if (value != "Dev")
        {
            TrustServerCertificate = false;
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public Color StatusColor => IsStatusError ? Colors.IndianRed : Colors.ForestGreen;

    partial void OnIsStatusErrorChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusColor));
    }

    [RelayCommand]
    private async Task LoadConnectionsAsync()
    {
        IsBusy = true;
        try
        {
            var connections = await connectionRepository.GetAllAsync();
            Connections.Clear();
            foreach (var connection in connections.OrderByDescending(item => item.IsFavorite))
            {
                Connections.Add(connection);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load saved SQL Server connections.");
            ShowStatus("No se pudieron cargar las conexiones guardadas.", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ShowAddForm()
    {
        ResetForm();
        _editingConnection = null;
        FormTitle = "Nueva conexión";
        IsFormVisible = true;
    }

    [RelayCommand]
    private void CancelForm()
    {
        IsFormVisible = false;
        _editingConnection = null;
        ResetForm();
    }

    [RelayCommand]
    private async Task EditConnectionAsync(ConnectionModel connection)
    {
        try
        {
            var password = connection.AuthenticationType == "Windows"
                ? null
                : await connectionRepository.GetPasswordAsync(connection.Id);
            if (connection.AuthenticationType != "Windows" && password is null)
            {
                ShowStatus("No se encontró la contraseña guardada. No es posible editar esta conexión.", isError: true);
                return;
            }

            _editingConnection = connection;
            FormTitle = "Editar conexión";
            ConnectionName = connection.Name;
            Server = connection.Server;
            Database = connection.Database;
            UserName = connection.UserName;
            Password = password ?? string.Empty;
            SelectedAuthenticationType = connection.AuthenticationType;
            SelectedEnvironment = connection.Environment;
            TrustServerCertificate = connection.TrustServerCertificate;
            IsFormVisible = true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load credentials for connection {ConnectionId}.", connection.Id);
            ShowStatus("No se pudieron leer las credenciales guardadas de esta conexión.", isError: true);
        }
    }

    [RelayCommand]
    private async Task SaveConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionName)
            || string.IsNullOrWhiteSpace(Server)
            || string.IsNullOrWhiteSpace(Database)
            || (UsesSqlAuthentication && string.IsNullOrWhiteSpace(UserName)))
        {
            ShowStatus("Completa el nombre, servidor y base de datos; con autenticación SQL también indica el usuario.", isError: true);
            return;
        }

        IsBusy = true;
        try
        {
            var connection = _editingConnection ?? new ConnectionModel();
            connection.Name = ConnectionName.Trim();
            connection.Server = Server.Trim();
            connection.Database = Database.Trim();
            connection.UserName = UsesSqlAuthentication ? UserName.Trim() : string.Empty;
            connection.AuthenticationType = SelectedAuthenticationType;
            connection.Environment = SelectedEnvironment;
            connection.TrustServerCertificate = CanTrustServerCertificate && TrustServerCertificate;

            await connectionRepository.SaveAsync(connection, Password);
            if (_editingConnection is null)
            {
                Connections.Add(connection);
            }

            _editingConnection = null;
            IsFormVisible = false;
            ResetForm();
            ShowStatus("Conexión guardada.", isError: false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save SQL Server connection metadata.");
            ShowStatus("No se pudo guardar la conexión. Verifica que el almacenamiento seguro esté disponible.", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TestDraftConnectionAsync()
    {
        if (!ValidateDraft())
        {
            return;
        }

        await TestConnectionCoreAsync(
            Server.Trim(),
            Database.Trim(),
            UserName.Trim(),
            Password,
            SelectedAuthenticationType,
            TrustServerCertificate);
    }

    [RelayCommand]
    private async Task TestConnectionAsync(ConnectionModel connection)
    {
        IsBusy = true;
        try
        {
            var password = connection.AuthenticationType == "Windows"
                ? null
                : await connectionRepository.GetPasswordAsync(connection.Id);
            if (connection.AuthenticationType != "Windows" && password is null)
            {
                ShowStatus("No se encontró la contraseña guardada. Edita y guarda de nuevo esta conexión.", isError: true);
                return;
            }
            var connectionString = SqlConnectionStringFactory.Create(connection, password);

            await databaseRepository.TestConnectionAsync(connectionString);
            connection.LastSuccessfulConnection = DateTimeOffset.Now;
            await connectionRepository.SaveAsync(connection, password ?? string.Empty);
            ShowStatus($"Conexión correcta: {connection.Name}.", isError: false);
        }
        catch (SqlException exception)
        {
            logger.LogWarning(exception, "Connection test failed for saved connection {ConnectionId}.", connection.Id);
            ShowStatus(GetFriendlySqlError(exception), isError: true);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected error testing saved connection {ConnectionId}.", connection.Id);
            ShowStatus("Ocurrió un error inesperado al probar la conexión.", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConnectAsync(ConnectionModel connection)
    {
        IsBusy = true;
        try
        {
            var password = connection.AuthenticationType == "Windows"
                ? null
                : await connectionRepository.GetPasswordAsync(connection.Id);
            if (connection.AuthenticationType != "Windows" && password is null)
            {
                ShowStatus("No se encontró la contraseña guardada para esta conexión.", isError: true);
                return;
            }

            var connectionString = SqlConnectionStringFactory.Create(connection, password);
            await databaseRepository.TestConnectionAsync(connectionString);
            connection.LastSuccessfulConnection = DateTimeOffset.Now;
            await connectionRepository.SaveAsync(connection, password ?? string.Empty);
            await activeConnectionService.SetActiveAsync(connection);
            ShowStatus($"Conectado a {connection.Name}. Ya puedes usar las demás pestañas.", isError: false);
        }
        catch (SqlException exception)
        {
            logger.LogWarning(exception, "Could not activate saved connection {ConnectionId}.", connection.Id);
            ShowStatus(GetFriendlySqlError(exception), isError: true);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected error activating connection {ConnectionId}.", connection.Id);
            ShowStatus("No se pudo activar la conexión.", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(ConnectionModel connection)
    {
        try
        {
            var password = connection.AuthenticationType == "Windows"
                ? null
                : await connectionRepository.GetPasswordAsync(connection.Id);
            if (connection.AuthenticationType != "Windows" && password is null)
            {
                ShowStatus("No se encontró la contraseña guardada para esta conexión.", isError: true);
                return;
            }

            connection.IsFavorite = !connection.IsFavorite;
            await connectionRepository.SaveAsync(connection, password ?? string.Empty);

            var sorted = Connections.OrderByDescending(item => item.IsFavorite).ToList();
            Connections.Clear();
            foreach (var item in sorted)
            {
                Connections.Add(item);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not update favorite state for connection {ConnectionId}.", connection.Id);
            ShowStatus("No se pudo actualizar el favorito.", isError: true);
        }
    }

    [RelayCommand]
    private async Task DeleteConnectionAsync(ConnectionModel connection)
    {
        try
        {
            await connectionRepository.DeleteAsync(connection.Id);
            activeConnectionService.ClearActiveIf(connection.Id);
            Connections.Remove(connection);
            ShowStatus($"Conexión eliminada: {connection.Name}.", isError: false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not delete connection {ConnectionId}.", connection.Id);
            ShowStatus("No se pudo eliminar la conexión.", isError: true);
        }
    }

    private bool ValidateDraft()
    {
        if (string.IsNullOrWhiteSpace(Server)
            || string.IsNullOrWhiteSpace(Database)
            || (UsesSqlAuthentication && string.IsNullOrWhiteSpace(UserName)))
        {
            ShowStatus("Indica el servidor y la base de datos; con autenticación SQL también indica el usuario.", isError: true);
            return false;
        }

        return true;
    }

    private async Task TestConnectionCoreAsync(
        string server,
        string database,
        string userName,
        string password,
        string authenticationType,
        bool trustServerCertificate)
    {
        IsBusy = true;
        try
        {
            await databaseRepository.TestConnectionAsync(
                SqlConnectionStringFactory.Create(
                    new ConnectionModel
                    {
                        Server = server,
                        Database = database,
                        UserName = userName,
                        AuthenticationType = authenticationType,
                        Environment = SelectedEnvironment,
                        TrustServerCertificate = CanTrustServerCertificate && trustServerCertificate
                    },
                    password));
            ShowStatus("Conexión correcta. SQL Server respondió a SELECT @@VERSION.", isError: false);
        }
        catch (SqlException exception)
        {
            logger.LogWarning(exception, "Connection test failed for the draft SQL Server connection.");
            ShowStatus(GetFriendlySqlError(exception), isError: true);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected error testing the draft SQL Server connection.");
            ShowStatus("Ocurrió un error inesperado al probar la conexión.", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string GetFriendlySqlError(SqlException exception)
    {
        return exception.Number switch
        {
            18456 => "Inicio de sesión rechazado. Revisa el usuario y la contraseña.",
            2 or 53 or 11001 => "No se encontró el servidor. Revisa el nombre, la red y que SQL Server acepte conexiones remotas.",
            -2 => "La conexión agotó el tiempo de espera. Revisa la red y que el servidor esté disponible.",
            _ => $"No se pudo conectar a SQL Server. Detalle: {exception.Message}"
        };
    }

    private void ResetForm()
    {
        ConnectionName = string.Empty;
        Server = string.Empty;
        Database = string.Empty;
        UserName = string.Empty;
        Password = string.Empty;
        SelectedAuthenticationType = "SQL Server";
        SelectedEnvironment = Environments[0];
        TrustServerCertificate = false;
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }

    private ConnectionModel? _editingConnection;
}
