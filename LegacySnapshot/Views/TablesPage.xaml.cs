using LegacySnapshot.ViewModels;

namespace LegacySnapshot.Views;

public partial class TablesPage : ContentPage
{
    private readonly TablesViewModel _viewModel;

    public TablesPage(TablesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadTablesCommand.Execute(null);
    }
}
