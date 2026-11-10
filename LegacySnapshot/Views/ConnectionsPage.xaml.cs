using LegacySnapshot.Models;
using LegacySnapshot.ViewModels;

namespace LegacySnapshot.Views;

public partial class ConnectionsPage : ContentPage
{
    private readonly ConnectionsViewModel _viewModel;

    public ConnectionsPage(ConnectionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadConnectionsCommand.Execute(null);
    }

    private async void OnDeleteConnectionClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: ConnectionModel connection })
        {
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Eliminar conexión",
            $"¿Quieres eliminar la conexión \"{connection.Name}\"?",
            "Eliminar",
            "Cancelar");

        if (confirmed)
        {
            await _viewModel.DeleteConnectionCommand.ExecuteAsync(connection);
        }
    }
}
