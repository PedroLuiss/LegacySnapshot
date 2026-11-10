using LegacySnapshot.ViewModels;

namespace LegacySnapshot.Views;

public partial class QueryRunnerPage : ContentPage
{
    private readonly QueryRunnerViewModel _viewModel;

    public QueryRunnerPage(QueryRunnerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadHistoryCommand.Execute(null);
    }
}
